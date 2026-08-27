using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Orders;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;

namespace PhotoPrint.API.Services;

public class OrderService : IOrderService
{
    private readonly PhotoPrintDbContext _db;
    private readonly IOrderNumberService _orderNumberService;
    private readonly IShippingService _shipping;
    private readonly IStorageRouter _storageRouter;
    private readonly StorageSettings _storageSettings;
    private readonly VatSettings _vatSettings;

    public OrderService(
        PhotoPrintDbContext db,
        IOrderNumberService orderNumberService,
        IShippingService shipping,
        IStorageRouter storageRouter,
        IOptions<StorageSettings> storageSettings,
        IOptions<VatSettings> vatSettings)
    {
        _db = db;
        _orderNumberService = orderNumberService;
        _shipping = shipping;
        _storageRouter = storageRouter;
        _storageSettings = storageSettings.Value;
        _vatSettings = vatSettings.Value;
    }

    // ── Idempotency ────────────────────────────────────────────────

    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);

    // The non-Postgres OrderNumber generator is a racy COUNT, so a
    // concurrent insert can pick a duplicate number. That is transient — regenerate and retry
    // a bounded number of times before letting a genuine, persistent clash surface.
    private const int MaxOrderNumberRetries = 3;

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

        // 2b. Idempotency resolution. Only when a key is supplied.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // Single round-trip for THIS caller's row holding the key — fresh or stale
            // (replaces the old two-query fresh-then-stale pattern). Scoped to
            // the caller so another tenant's key can never be resolved here.
            var holder = await FindKeyHolderAsync(idempotencyKey, userId, guestSessionId, ct);
            if (holder is not null)
            {
                // A settled order must not hand back its client secret; a failed one frees the key.
                if (IsFresh(holder) && holder.Status == OrderStatus.AwaitingPayment)
                    return ReplayOrConflict(holder, request, total, orderItems);

                if (IsFresh(holder) && holder.Status != OrderStatus.PaymentFailed)
                    throw new IdempotencyKeyConsumedException(holder.Id);

                // Stale (>24h) row this caller owns still holds the key. Null it on the
                // in-memory entity WITHOUT an intermediate save, so
                // the free (UPDATE) and the new-order INSERT below flush in ONE
                // SaveChanges → one transaction. EF Core's unique-index-aware command
                // ordering emits the UPDATE before the INSERT, so they do not collide on
                // ix_orders_idempotency_key within the batch; and because they share a
                // transaction, a failing INSERT rolls the free back with it — the stale
                // row can never lose its key linkage with no replacement order created
                // (the orphaning the two-save version risked on a crash/mid-failure).
                holder.IdempotencyKey = null;
            }
        }

        // 3. Capture guest email before building the order
        string? guestEmail = null;
        if (guestSessionId.HasValue)
        {
            var gs = await _db.GuestSessions.FindAsync(new object[] { guestSessionId.Value }, ct);
            guestEmail = gs?.Email;
        }

        // 4. VAT breakdown (bolt 038). Romanian convention: prices are
        // VAT-inclusive; VAT is extracted from the gross total, not added
        // on top. Shipping is folded into the gross at the same rate as
        // goods (the simpler/common B2C convention — see ADR-019 rounding,
        // and a code-level comment in VatCalculator on the shipping
        // assumption). Rate is snapshotted onto the order; later config
        // changes do NOT mutate existing rows.
        var vat = VatCalculator.ExtractBreakdown(total, _vatSettings.Rate);

        // 5. Build and persist the order
        var order = new Order
        {
            OrderNumber = await _orderNumberService.GenerateAsync(ct),
            UserId = userId,
            GuestSessionId = guestSessionId,
            GuestEmail = guestEmail,
            Status = OrderStatus.AwaitingPayment,
            ShippingAddress = request.ShippingAddress ?? new ShippingAddressSnapshot(),
            DeliveryType = request.DeliveryType,
            EasyboxLockerId = request.EasyboxLockerId,
            ShippingCostRon = shippingCostRon,
            SubtotalRon = subtotal,
            TotalRon = total,
            NetTotalRon = vat.NetTotalRon,
            VatRon = vat.VatRon,
            VatRate = vat.VatRate,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey,
            Items = orderItems,
        };

        _db.Orders.Add(order);

        var hasKey = !string.IsNullOrWhiteSpace(idempotencyKey);

        // 5. Persist. Two unique indexes can reject the INSERT, each with its own recovery;
        // any OTHER DbUpdateException (FK, NOT NULL) matches neither `when` filter and
        // propagates honestly — we never infer the cause from a follow-up AnyAsync probe.
        //
        //  • ix_orders_idempotency_key: a concurrent request with the SAME key won
        //    the race between our resolution above and this save → resolve the winner
        //    (replay / 409) instead of a 500. Terminal — no retry.
        //  • ix_orders_order_number: on InMemory the OrderNumber comes from a racy COUNT,
        //    so two concurrent inserts can pick the SAME number. That is transient and
        //    unrelated to idempotency — regenerate the number and retry the (still-tracked)
        //    order rather than 500. Bounded, so a genuine persistent clash still surfaces.
        //    Postgres uses a per-year sequence and cannot hit this.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await _db.SaveChangesAsync(ct);

                // Observability: orders_created_total{processor,status}.
                // Status is "created" — the order is in AwaitingPayment; the Paid
                // transition is observed via payment_webhook_total at the webhook handlers.
                FotoMetrics.OrdersCreated.Add(1,
                    new TagList
                    {
                        { MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe },
                        { MetricNames.Labels.Status,    MetricNames.OrderStatusValues.Created },
                    });

                return new OrderCreationResult(order, WasIdempotentReplay: false);
            }
            catch (DbUpdateException ex) when (hasKey && IsIdempotencyKeyViolation(ex))
            {
                // Snapshot the candidate items BEFORE detaching: order.Items and orderItems
                // are the same reference, and EF fix-up empties the collection as each item
                // is detached — which would otherwise make the divergence check see no items.
                var candidateItems = order.Items.ToList();
                DetachFailedInsert(order);

                var winner = await FindKeyHolderAsync(idempotencyKey!, userId, guestSessionId, ct);
                if (winner is not null && IsFresh(winner))
                {
                    if (winner.Status == OrderStatus.AwaitingPayment)
                        return ReplayOrConflict(winner, request, total, candidateItems);

                    if (winner.Status != OrderStatus.PaymentFailed)
                        throw new IdempotencyKeyConsumedException(winner.Id);
                }

                // The constraint error already proves the key is taken, but this caller owns
                // no (fresh) row for it → it is held by a *different* caller (global unique
                // index). Replaying would disclose another tenant's order → clean 409.
                // Thrown as a DISTINCT type (subtype of ConflictException) so the middleware
                // can emit the reserved cross-tenant abuse signal for triage.
                throw new IdempotencyKeyTakenException();
            }
            catch (DbUpdateException ex) when (attempt < MaxOrderNumberRetries && IsOrderNumberViolation(ex))
            {
                // Transient number-generation race: regenerate and retry the SAME
                // order — it is still tracked as Added after the failed save, so assigning a
                // fresh number and looping re-attempts the INSERT (no detach/rebuild needed).
                order.OrderNumber = await _orderNumberService.GenerateAsync(ct);
            }
        }
    }

    /// <summary>
    /// True iff <paramref name="ex"/> is the database rejecting an INSERT/UPDATE because
    /// of the idempotency unique index (<see cref="PhotoPrintDbContext.IdempotencyKeyIndexName"/>)
    /// — as opposed to any other constraint. Inspecting the provider error lets the catch
    /// handle ONLY a real key collision and rethrow everything else, instead of inferring the
    /// cause from a follow-up query.
    ///
    /// Matching <see cref="PhotoPrintDbContext.IdempotencyKeyIndexName"/> — the same constant the
    /// index is named with — makes a rename a compile break rather than a silent fall-through.
    /// </summary>
    private static bool IsIdempotencyKeyViolation(DbUpdateException ex)
        => ex.InnerException switch
        {
            Npgsql.PostgresException pg =>
                pg.SqlState == "23505" /* unique_violation */ &&
                pg.ConstraintName == PhotoPrintDbContext.IdempotencyKeyIndexName,
            _ => false,
        };

    /// <summary>
    /// True iff <paramref name="ex"/> is the database rejecting the INSERT because of the
    /// OrderNumber unique index (<see cref="PhotoPrintDbContext.OrderNumberIndexName"/>).
    /// Only the InMemory COUNT-based generator can produce a duplicate number under
    /// concurrency, and that is transient — the caller regenerates
    /// and retries. Same provider-error inspection (and same compile-break coupling) as
    /// <see cref="IsIdempotencyKeyViolation"/>.
    /// </summary>
    private static bool IsOrderNumberViolation(DbUpdateException ex)
        => ex.InnerException switch
        {
            Npgsql.PostgresException pg =>
                pg.SqlState == "23505" /* unique_violation */ &&
                pg.ConstraintName == PhotoPrintDbContext.OrderNumberIndexName,
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

    // ── Idempotency helpers ─────────────────────────────────────────

    /// <summary>True while <paramref name="holder"/> is inside the 24h idempotency window.</summary>
    private static bool IsFresh(Order holder)
        => holder.CreatedAt > DateTimeOffset.UtcNow - IdempotencyWindow;

    /// <summary>
    /// Shared resolution for a fresh holder of the caller's key:
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
    /// caller can only ever see their own order. Items are included for the
    /// divergence comparison.
    /// </summary>
    private Task<Order?> FindKeyHolderAsync(
        string key, Guid? userId, Guid? guestSessionId, CancellationToken ct)
    {
        // Defense-in-depth. With both identities null the scope
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
    /// `ShippingAddress` is intentionally excluded; the cart items ARE
    /// compared — without that, two carts with identical totals but different
    /// photos would silently replay the wrong order's images.
    /// </summary>
    private static IReadOnlyList<string> DivergentFields(
        Order existing, CreateOrderRequest request, decimal candidateTotal,
        IReadOnlyList<OrderItem> candidateItems)
    {
        var fields = new List<string>();
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
    /// wrong order.
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
            // Unique tiebreaker so Skip/Take paging stays stable when CreatedAt ties. This query projects Items.Sum (one correlated query, not a split
            // Include), so it can't drop items like the admin list — but paging must still be total.
            .OrderByDescending(o => o.CreatedAt)
            .ThenBy(o => o.Id);

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
            order.NetTotalRon,
            order.VatRon,
            order.VatRate,
            order.ShippingCostRon,
            order.TotalRon,
            order.CreatedAt,
            order.PaidAt,
            order.DeliveryType.ToString(),
            order.EasyboxLockerId,
            order.EasyboxLocker?.Name,
            order.EasyboxLocker?.Address,
            shippingAddress,
            items);
    }

    // ── GetOrderPhotosAsync ────────────────────────────────────────

    public async Task<OrderPhotosDto> GetOrderPhotosAsync(
        Guid orderId, Guid userId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Upload)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            throw new NotFoundException($"Order {orderId} not found.");

        if (order.UserId != userId)
            throw new ForbiddenException("Access denied.");

        // Cloud tier off (Storage:Provider=Local) — dev / misconfigured deployment.
        // Return empty rather than 500; the UI's empty-state copy covers it.
        if (!_storageRouter.CloudEnabled)
            return new OrderPhotosDto([]);

        var ttl = TimeSpan.FromMinutes(_storageSettings.PresignTtlMinutes);

        // Filter: only live (not soft-deleted), Cloud-promoted uploads with BOTH blob keys
        // still present. UploadCleanupJob soft-deletes a row but leaves its path fields set,
        // so without the DeletedAt check this presigned URLs for already-deleted blobs —
        // broken thumbnails the refresh can't fix. A row mid-retention
        // with one key nulled is excluded — the lightbox would otherwise fail on click.
        var viewable = order.Items
            .Select(i => i.Upload)
            .Where(u => u.DeletedAt == null)
            .Where(u => u.StorageLocation == StorageLocation.Cloud)
            .Where(u => u.LargePreviewPath is not null && u.ThumbnailPath is not null)
            .DistinctBy(u => u.Id)
            .ToList();

        var photos = new List<OrderPhotoDto>(viewable.Count);
        foreach (var u in viewable)
        {
            var thumbUrl = await _storageRouter.Cloud
                .GetPresignedUrlAsync(u.ThumbnailPath!, ttl, ct);
            var largeUrl = await _storageRouter.Cloud
                .GetPresignedUrlAsync(u.LargePreviewPath!, ttl, ct);
            photos.Add(new OrderPhotoDto(u.Id, u.OriginalFileName, thumbUrl, largeUrl));
        }

        return new OrderPhotosDto(photos);
    }

    // ── Price helper (mirrors CartService.ResolveUnitPrice) ───────────────────

    // The tier bracket-matching is shared with CartService via
    // PricingTierResolver. This call site keeps its own semantics — the tiers of the item's
    // ACTIVE sizes, matched against that item's own quantity — which is deliberately NOT the
    // same basis CartService uses (a size's tiers vs. the per-group total copies). The old
    // "mirrors CartService" comment claimed they were identical; they are not, so only the
    // bracket rule is shared, not the input selection.
    private static decimal ResolveUnitPrice(Product product, int quantity)
        => PricingTierResolver.Resolve(
            product.Sizes.Where(s => s.IsActive).SelectMany(s => s.PricingTiers),
            quantity);
}
