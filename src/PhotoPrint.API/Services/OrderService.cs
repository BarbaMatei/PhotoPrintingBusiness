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
            // Single round-trip for THIS caller's row holding the key — fresh or stale
            // (QUAL-1: replaces the old two-query fresh-then-stale pattern). Scoped to
            // the caller so another tenant's key can never be resolved here (SEC-1).
            var holder = await FindKeyHolderAsync(idempotencyKey, userId, guestSessionId, ct);
            if (holder is not null)
            {
                if (holder.CreatedAt > DateTimeOffset.UtcNow - IdempotencyWindow)
                {
                    // Fresh match for this caller → replay or (on divergence) 409.
                    var divergent = DivergentFields(holder, request, total, orderItems);
                    if (divergent.Count > 0)
                        throw new IdempotencyConflictException(divergent);

                    return new OrderCreationResult(holder, WasIdempotentReplay: true);
                }

                // Stale (>24h) row this caller owns still holds the key. Free it first,
                // in its own save, so the INSERT below does not violate the unique index
                // (both SQLite and Postgres enforce it per-statement — DOC-1).
                holder.IdempotencyKey = null;
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

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _db.SaveChangesAsync(ct);
            return new OrderCreationResult(order, WasIdempotentReplay: false);
        }

        // BUG-1: a concurrent request carrying the same key may have won the INSERT
        // race between our resolution above and this save. The unique index then
        // rejects ours with a DbUpdateException — previously unhandled → 500 on the
        // canonical double-submit. Catch it, detach our failed insert, and resolve
        // the winner instead of surfacing a 500.
        //
        // The `when` filter (BUG-1, review 035-v5) confirms the failure IS the
        // idempotency unique index before treating it as a key collision. An unrelated
        // DbUpdateException (FK, NOT NULL, an OrderNumber unique collision) no longer
        // matches the catch, so it propagates honestly instead of being masked behind a
        // misleading 409 — and we no longer probe `AnyAsync(key)` to *infer* the cause
        // (which also raced a holder freed between the throw and the probe).
        try
        {
            await _db.SaveChangesAsync(ct);
            return new OrderCreationResult(order, WasIdempotentReplay: false);
        }
        catch (DbUpdateException ex) when (IsIdempotencyKeyViolation(ex))
        {
            // Snapshot the candidate items BEFORE detaching: order.Items and orderItems
            // are the same reference, and EF fix-up empties the collection as each item
            // is detached — which would otherwise make the divergence check see no items.
            var candidateItems = order.Items.ToList();
            DetachFailedInsert(order);

            var winner = await FindKeyHolderAsync(idempotencyKey, userId, guestSessionId, ct);
            if (winner is not null && winner.CreatedAt > DateTimeOffset.UtcNow - IdempotencyWindow)
            {
                // Same caller won the race → replay it (or 409 if the request diverged).
                var divergent = DivergentFields(winner, request, total, candidateItems);
                if (divergent.Count > 0)
                    throw new IdempotencyConflictException(divergent);

                return new OrderCreationResult(winner, WasIdempotentReplay: true);
            }

            // The constraint error already proves the key is taken, but this caller owns
            // no (fresh) row for it → it is held by a *different* caller (global unique
            // index). Replaying would disclose another tenant's order (SEC-1) → clean 409.
            throw new ConflictException(
                "The Idempotency-Key is already associated with another request.");
        }
    }

    /// <summary>
    /// True iff <paramref name="ex"/> is the database rejecting an INSERT/UPDATE because
    /// of the <c>ix_orders_idempotency_key</c> unique index — as opposed to any other
    /// constraint. Inspecting the provider error (Postgres <c>23505</c> + the constraint
    /// name, SQLite constraint code + the column name) lets the catch handle ONLY a real
    /// key collision and rethrow everything else, instead of inferring the cause from a
    /// follow-up query (BUG-1, review 035-v5).
    /// </summary>
    private static bool IsIdempotencyKeyViolation(DbUpdateException ex)
        => ex.InnerException switch
        {
            Microsoft.Data.Sqlite.SqliteException sqlite =>
                sqlite.SqliteErrorCode == 19 /* SQLITE_CONSTRAINT */ &&
                sqlite.Message.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase),
            Npgsql.PostgresException pg =>
                pg.SqlState == "23505" /* unique_violation */ &&
                pg.ConstraintName == "ix_orders_idempotency_key",
            _ => false,
        };

    /// <summary>
    /// Detaches an order whose INSERT was rejected by the unique index, together with
    /// its items, so the shared request-scoped <see cref="PhotoPrintDbContext"/> does
    /// not later persist the orphaned graph on the next SaveChanges (e.g. the
    /// controller saving the gateway secret after an idempotent replay).
    /// </summary>
    private void DetachFailedInsert(Order order)
    {
        // Snapshot the collection: detaching an item triggers EF fix-up that removes it
        // from order.Items, which would otherwise mutate the collection mid-enumeration.
        foreach (var item in order.Items.ToList())
            _db.Entry(item).State = EntityState.Detached;
        _db.Entry(order).State = EntityState.Detached;
    }

    // ── Idempotency helpers (bolt 035) ─────────────────────────────────────────

    public async Task<Order?> GetByIdempotencyKeyAsync(
        string key, Guid? userId, Guid? guestSessionId, CancellationToken ct = default)
    {
        var holder = await FindKeyHolderAsync(key, userId, guestSessionId, ct);
        if (holder is null)
            return null;

        // Stale (>24h) matches are treated as absent (the window has expired).
        return holder.CreatedAt > DateTimeOffset.UtcNow - IdempotencyWindow ? holder : null;
    }

    /// <summary>
    /// The single order (if any) currently holding <paramref name="key"/> for the
    /// supplied caller — fresh OR stale; callers branch on the 24h window in memory.
    /// Scoped to <paramref name="userId"/> / <paramref name="guestSessionId"/> so a
    /// caller can only ever see their own order (SEC-1). Items are included for the
    /// divergence comparison (BUG-3).
    /// </summary>
    private Task<Order?> FindKeyHolderAsync(
        string key, Guid? userId, Guid? guestSessionId, CancellationToken ct)
        => _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o =>
                o.IdempotencyKey == key &&
                (userId.HasValue ? o.UserId == userId : o.GuestSessionId == guestSessionId), ct);

    /// <summary>
    /// Returns the names of the fields that diverge between an existing order and a
    /// candidate request. Empty list = same logical request (replay-eligible).
    /// `ShippingAddress` is intentionally excluded (see ADR-005); the cart items ARE
    /// compared (BUG-3) — without that, two carts with identical totals but different
    /// photos would silently replay the wrong order's images.
    /// </summary>
    private static IReadOnlyList<string> DivergentFields(
        Order existing, CreateOrderRequest request, decimal candidateTotal,
        IReadOnlyList<OrderItem> candidateItems)
    {
        var fields = new List<string>();
        if (existing.PaymentProcessor != request.PaymentProcessor) fields.Add("paymentProcessor");
        if (existing.DeliveryType != request.DeliveryType) fields.Add("deliveryType");
        if (existing.EasyboxLockerId != request.EasyboxLockerId) fields.Add("easyboxLockerId");
        if (existing.TotalRon != candidateTotal) fields.Add("totalRon");
        if (ItemsSignature(existing.Items) != ItemsSignature(candidateItems)) fields.Add("items");
        return fields;
    }

    /// <summary>
    /// Order-independent signature of a set of order items (product + upload + quantity).
    /// Two requests with the same total but different photos/quantities produce
    /// different signatures, so a reused key correctly 409s instead of replaying the
    /// wrong order (BUG-3).
    /// </summary>
    private static string ItemsSignature(IEnumerable<OrderItem> items)
        => string.Join("|", items
            .OrderBy(i => i.ProductId)
            .ThenBy(i => i.UploadId)
            .Select(i => $"{i.ProductId:N}:{i.UploadId:N}:{i.Quantity}"));

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
