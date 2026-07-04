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
                // Fresh match for this caller → replay or (on divergence) 409 (QUAL-1).
                if (IsFresh(holder))
                    return ReplayOrConflict(holder, request, total, orderItems);

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

            // Same caller won the race → replay it (or 409 if the request diverged) (QUAL-1).
            var winner = await FindKeyHolderAsync(idempotencyKey, userId, guestSessionId, ct);
            if (winner is not null && IsFresh(winner))
                return ReplayOrConflict(winner, request, total, candidateItems);

            // The constraint error already proves the key is taken, but this caller owns
            // no (fresh) row for it → it is held by a *different* caller (global unique
            // index). Replaying would disclose another tenant's order (SEC-1) → clean 409.
            // Thrown as a DISTINCT type (subtype of ConflictException) so the middleware
            // can emit the reserved cross-tenant abuse signal for triage (OBS-1, v8).
            throw new IdempotencyKeyTakenException();
        }
    }

    /// <summary>
    /// True iff <paramref name="ex"/> is the database rejecting an INSERT/UPDATE because
    /// of the idempotency unique index (<see cref="PhotoPrintDbContext.IdempotencyKeyIndexName"/>)
    /// — as opposed to any other constraint. Inspecting the provider error lets the catch
    /// handle ONLY a real key collision and rethrow everything else, instead of inferring the
    /// cause from a follow-up query (BUG-1, review 035-v5).
    ///
    /// BUG-1 (review 035-v8) hardened both arms against silent regressions:
    /// <list type="bullet">
    /// <item>Postgres matches <see cref="PhotoPrintDbContext.IdempotencyKeyIndexName"/> (the
    /// same constant the index is named with) so a rename is a compile break, not a fall-through.</item>
    /// <item>SQLite has no structured constraint name, so it keys off the <b>extended</b> result
    /// code <c>SQLITE_CONSTRAINT_UNIQUE</c> (2067 — narrower than the generic <c>SQLITE_CONSTRAINT</c>
    /// 19) plus the column name via <c>nameof</c>, so a column rename also breaks at compile time.</item>
    /// </list>
    /// </summary>
    private static bool IsIdempotencyKeyViolation(DbUpdateException ex)
        => ex.InnerException switch
        {
            Microsoft.Data.Sqlite.SqliteException sqlite =>
                sqlite.SqliteExtendedErrorCode == 2067 /* SQLITE_CONSTRAINT_UNIQUE */ &&
                sqlite.Message.Contains(nameof(Order.IdempotencyKey), StringComparison.OrdinalIgnoreCase),
            Npgsql.PostgresException pg =>
                pg.SqlState == "23505" /* unique_violation */ &&
                pg.ConstraintName == PhotoPrintDbContext.IdempotencyKeyIndexName,
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
        return IsFresh(holder) ? holder : null;
    }

    /// <summary>True while <paramref name="holder"/> is inside the 24h idempotency window.</summary>
    private static bool IsFresh(Order holder)
        => holder.CreatedAt > DateTimeOffset.UtcNow - IdempotencyWindow;

    /// <summary>
    /// Shared resolution for a fresh holder of the caller's key (QUAL-1, review 035-v5):
    /// replay it, or throw <see cref="IdempotencyConflictException"/> if the candidate
    /// request diverges. Used by both the pre-INSERT lookup and the post-collision
    /// recovery, which were near-duplicate blocks. (Named for what it does; the review
    /// sketched it as <c>ResolveFreshHolder</c>.)
    /// </summary>
    private OrderCreationResult ReplayOrConflict(
        Order freshHolder, CreateOrderRequest request, decimal total,
        IReadOnlyList<OrderItem> candidateItems)
    {
        var divergent = DivergentFields(freshHolder, request, total, candidateItems);
        if (divergent.Count > 0)
            throw new IdempotencyConflictException(divergent);

        return new OrderCreationResult(freshHolder, WasIdempotentReplay: true);
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
    {
        // SEC-1 (review 035-v5): defense-in-depth. With both identities null the scope
        // predicate below collapses to `o.GuestSessionId == null`, which matches every
        // authenticated user's order — a borrowed key could then resolve an arbitrary
        // user's order/secret. The payment endpoints' dual-auth guarantees exactly one
        // identity is non-null today, so this is unreachable; reject it loudly rather than
        // let a future token-shape change silently turn the predicate into an IDOR.
        if (userId is null && guestSessionId is null)
            throw new InvalidOperationException(
                "Idempotency lookup requires an authenticated user or guest session identity.");

        return _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o =>
                o.IdempotencyKey == key &&
                (userId.HasValue ? o.UserId == userId : o.GuestSessionId == guestSessionId), ct);
    }

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
