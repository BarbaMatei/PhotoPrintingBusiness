using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Orders;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class OrderService : IOrderService
{
    private readonly PhotoPrintDbContext _db;
    private readonly IOrderNumberService _orderNumberService;
    private readonly IShippingService _shipping;

    public OrderService(
        PhotoPrintDbContext db,
        IOrderNumberService orderNumberService,
        IShippingService shipping)
    {
        _db = db;
        _orderNumberService = orderNumberService;
        _shipping = shipping;
    }

    // ── Idempotency (bolt 035) ────────────────────────────────────────────────

    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);

    // ── CreateFromCartAsync ───────────────────────────────────────────────────

    public async Task<OrderCreationResult> CreateFromCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        CreateOrderRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        // 1. Load cart items with everything needed for price calculation
        var cartItems = await _db.CartItems
            .Where(ci => userId.HasValue
                ? ci.UserId == userId
                : ci.GuestSessionId == guestSessionId)
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Sizes)
                    .ThenInclude(s => s.PricingTiers)
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Finishes)
            .Include(ci => ci.Upload)
            .Where(ci => ci.Upload.DeletedAt == null)
            .OrderBy(ci => ci.AddedAt)
            .ToListAsync(ct);

        if (cartItems.Count == 0)
            throw new BadRequestException("Coșul este gol.");

        // 2. Build order items with price snapshots
        var orderItems = cartItems.Select(ci =>
        {
            var unitPrice = ResolveUnitPrice(ci.Product, ci.Quantity);
            var size = ci.Product.Sizes.FirstOrDefault(s => s.IsActive);
            return new OrderItem
            {
                UploadId = ci.UploadId,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPriceRon = unitPrice,
                LineTotalRon = unitPrice * ci.Quantity,
                ProductSnapshot = new ProductSnapshot
                {
                    ProductName = ci.Product.Name,
                    Size = size?.Label ?? "",
                    Finish = ci.Product.Finishes.FirstOrDefault()?.Name ?? "",
                },
            };
        }).ToList();

        var subtotal = orderItems.Sum(i => i.LineTotalRon);

        // Server-side shipping resolution — never trust client-supplied cost.
        // See bolt 034 (intent 014 payment-hardening).
        var shipping = await _shipping.GetShippingCostAsync(request.DeliveryType.ToString(), ct);
        var shippingCostRon = shipping.CostRon;
        var total = subtotal + shippingCostRon;

        // 2b. Idempotency resolution (bolt 035). Only when a key is supplied.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await GetByIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null)
            {
                var divergent = DivergentFields(existing, request, total);
                if (divergent.Count > 0)
                    throw new IdempotencyConflictException(divergent);

                // Same logical request within the window → replay the original order.
                return new OrderCreationResult(existing, WasIdempotentReplay: true);
            }

            // A row may still hold this key but be older than the window (stale).
            // Free the key first, in its own save, so the new INSERT below does not
            // transiently violate the unique index (Postgres checks per-statement).
            var stale = await _db.Orders
                .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, ct);
            if (stale is not null)
            {
                stale.IdempotencyKey = null;
                await _db.SaveChangesAsync(ct);
            }
        }

        // 3. Capture guest email before building the order
        string? guestEmail = null;
        if (guestSessionId.HasValue)
        {
            var gs = await _db.GuestSessions.FindAsync(new object[] { guestSessionId.Value }, ct);
            guestEmail = gs?.Email;
        }

        // 4. Generate order number
        var orderNumber = await _orderNumberService.GenerateAsync(ct);

        // 5. Build and persist the order
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            GuestSessionId = guestSessionId,
            GuestEmail = guestEmail,
            Status = OrderStatus.AwaitingPayment,
            PaymentProcessor = request.PaymentProcessor,
            ShippingAddress = request.ShippingAddress ?? new ShippingAddressSnapshot(),
            DeliveryType = request.DeliveryType,
            EasyboxLockerId = request.EasyboxLockerId,
            ShippingCostRon = shippingCostRon,
            SubtotalRon = subtotal,
            TotalRon = total,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey,
            Items = orderItems,
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return new OrderCreationResult(order, WasIdempotentReplay: false);
    }

    // ── Idempotency helpers (bolt 035) ─────────────────────────────────────────

    public async Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - IdempotencyWindow;
        return await _db.Orders
            .FirstOrDefaultAsync(o => o.IdempotencyKey == key && o.CreatedAt > cutoff, ct);
    }

    /// <summary>
    /// Returns the names of the fields that diverge between an existing order and a
    /// candidate request. Empty list = same logical request (replay-eligible).
    /// `ShippingAddress` is intentionally excluded (see ADR-005).
    /// </summary>
    private static IReadOnlyList<string> DivergentFields(
        Order existing, CreateOrderRequest request, decimal candidateTotal)
    {
        var fields = new List<string>();
        if (existing.PaymentProcessor != request.PaymentProcessor) fields.Add("paymentProcessor");
        if (existing.DeliveryType != request.DeliveryType) fields.Add("deliveryType");
        if (existing.EasyboxLockerId != request.EasyboxLockerId) fields.Add("easyboxLockerId");
        if (existing.TotalRon != candidateTotal) fields.Add("totalRon");
        return fields;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<Order?> GetByPaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken ct = default)
        => await _db.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId, ct);

    public async Task<Order?> GetByIdAsync(
        Guid orderId,
        CancellationToken ct = default)
        => await _db.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    // ── Customer order queries ────────────────────────────────────────────────

    public async Task<(IReadOnlyList<OrderSummaryDto> Items, int Total)> GetOrdersAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.Status.ToString(),
                o.TotalRon,
                o.CreatedAt,
                o.DeliveryType.ToString(),
                o.Items.Sum(i => i.Quantity)))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<OrderDetailDto> GetOrderDetailAsync(
        Guid orderId, Guid userId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.EasyboxLocker)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            throw new NotFoundException($"Order {orderId} not found.");

        if (order.UserId != userId)
            throw new ForbiddenException("Access denied.");

        var items = order.Items.Select(i => new OrderItemDto(
            i.UploadId,
            $"/api/uploads/{i.UploadId}/preview",
            i.ProductSnapshot.ProductName,
            i.ProductSnapshot.Size,
            i.ProductSnapshot.Finish,
            i.Quantity,
            i.UnitPriceRon,
            i.LineTotalRon)).ToList();

        ShippingAddressDto? shippingAddress = null;
        if (order.DeliveryType == DeliveryType.Courier && order.ShippingAddress is not null)
        {
            shippingAddress = new ShippingAddressDto(
                order.ShippingAddress.RecipientName,
                order.ShippingAddress.Street,
                order.ShippingAddress.Number,
                order.ShippingAddress.Block,
                order.ShippingAddress.City,
                order.ShippingAddress.County,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Phone);
        }

        return new OrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.SubtotalRon,
            order.ShippingCostRon,
            order.TotalRon,
            order.CreatedAt,
            order.PaidAt,
            order.DeliveryType.ToString(),
            order.PaymentProcessor.ToString(),
            order.EasyboxLockerId,
            order.EasyboxLocker?.Name,
            order.EasyboxLocker?.Address,
            shippingAddress,
            items);
    }

    // ── Price helper (mirrors CartService.ResolveUnitPrice) ───────────────────

    private static decimal ResolveUnitPrice(Product product, int quantity)
    {
        var tiers = product.Sizes
            .Where(s => s.IsActive)
            .SelectMany(s => s.PricingTiers)
            .OrderByDescending(t => t.MinQuantity)
            .ToList();

        if (tiers.Count == 0)
            return 0m;

        var matched = tiers.FirstOrDefault(t =>
            t.MinQuantity <= quantity &&
            (t.MaxQuantity == null || quantity <= t.MaxQuantity));

        return (matched ?? tiers[0]).UnitPrice;
    }
}
