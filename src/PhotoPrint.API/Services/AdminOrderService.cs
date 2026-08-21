using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.DTOs.Orders;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.API.Services.Sameday;
using Stripe;

namespace PhotoPrint.API.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly PhotoPrintDbContext _db;
    private readonly IOrderEmailService _orderEmailService;
    private readonly IEuPlatescService _euPlatescService;
    private readonly IStripeClient _stripeClient;
    private readonly IStorageRouter _storageRouter;
    private readonly IOriginalPurger _originalPurger;
    private readonly ArchiveSettings _archiveSettings;
    private readonly IHubContext<AdminOrderHub> _hub;
    private readonly IAwbCreationNotifier _awbNotifier;
    private readonly IInvoiceCreationService _invoiceCreator;
    private readonly ILogger<AdminOrderService> _logger;
    private readonly Sentry.IHub? _sentry;

    public AdminOrderService(
        PhotoPrintDbContext db,
        IOrderEmailService orderEmailService,
        IEuPlatescService euPlatescService,
        IStripeClient stripeClient,
        IStorageRouter storageRouter,
        IOriginalPurger originalPurger,
        IOptions<ArchiveSettings> archiveSettings,
        IHubContext<AdminOrderHub> hub,
        IAwbCreationNotifier awbNotifier,
        IInvoiceCreationService invoiceCreator,
        ILogger<AdminOrderService> logger,
        // No hub is registered unless Sentry:Enabled, so this cannot be a required dependency.
        Sentry.IHub? sentry = null)
    {
        _db = db;
        _orderEmailService = orderEmailService;
        _euPlatescService = euPlatescService;
        _stripeClient = stripeClient;
        _storageRouter = storageRouter;
        _originalPurger = originalPurger;
        _archiveSettings = archiveSettings.Value;
        _hub = hub;
        _awbNotifier = awbNotifier;
        _invoiceCreator = invoiceCreator;
        _logger = logger;
        _sentry = sentry;
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<AdminOrderSummaryDto> Items, int Total)> GetOrdersAsync(
        int page, int pageSize, string? status, string? search, CancellationToken ct = default)
    {
        var query = _db.Orders
            .Include(o => o.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToUpperInvariant();
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                (o.User != null && o.User.NormalizedEmail!.Contains(q)) ||
                (o.GuestEmail != null && o.GuestEmail.ToUpper().Contains(q)));
        }

        // Unique tiebreaker on this Skip/Take + Include(Items) query. Under the global SplitQuery
        // default (Program.cs) the parent page and the Items child run as separate round-trips; a
        // non-unique ORDER BY (CreatedAt ties) can resolve the tie differently between them on
        // Postgres under concurrency, so a paged order comes back with missing Items. Id makes the
        // order total.
        query = query.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .ToListAsync(ct);

        return (items.Select(BuildSummaryDto).ToList(), total);
    }

    // ── Detail ────────────────────────────────────────────────────────────────

    public async Task<AdminOrderDetailDto> GetOrderDetailAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await LoadFullOrderAsync(orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found.");
        return BuildDetailDto(order);
    }

    // ── Update status ─────────────────────────────────────────────────────────

    public async Task<AdminOrderDetailDto> UpdateStatusAsync(
        Guid orderId, string statusStr, string? awbNumber, string? trackingUrl,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrderStatus>(statusStr, true, out var newStatus))
            throw new BadRequestException($"Unknown order status '{statusStr}'.");

        var order = await LoadFullOrderAsync(orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        OrderStatusMachine.Transition(order, newStatus);

        double? processingSeconds = null;
        var savedWithInvoice = false;
        PaidSaveOutcome? paidOutcome = null;

        if (newStatus == OrderStatus.Shipped)
        {
            // Preserve a machine-created AWB/tracking URL when the admin form omits it.
            if (!string.IsNullOrWhiteSpace(awbNumber)) order.AwbNumber = awbNumber;
            if (!string.IsNullOrWhiteSpace(trackingUrl)) order.TrackingUrl = trackingUrl;
            order.ShippedAt = DateTimeOffset.UtcNow;

            // PaidAt should always be set for an order reaching Shipped, but guard
            // anyway — the admin's manual force-Shipped path is a rare edge case.
            if (order.PaidAt is not null)
                processingSeconds = (order.ShippedAt.Value - order.PaidAt.Value).TotalSeconds;
        }
        else if (newStatus == OrderStatus.Delivered && order.DeliveredAt is null)
        {
            // Admin-initiated Delivered (legacy or manual override path).
            order.DeliveredAt = DateTimeOffset.UtcNow;
        }
        else if (newStatus == OrderStatus.Paid && order.PaidAt is null)
        {
            // Offline / manual reconciliation — PaidAt is what the AWB retry sweep
            // keys off, so it must be stamped like the webhook Paid path does.
            order.PaidAt = DateTimeOffset.UtcNow;
            paidOutcome = await SaveWithInvoiceAsync(order, ct);
            savedWithInvoice = true;
        }

        if (!savedWithInvoice)
            await _db.SaveChangesAsync(ct);

        // The order is still awaiting payment, so a 200 carrying a Paid-looking DTO would tell the admin the opposite of what happened.
        if (paidOutcome == PaidSaveOutcome.NumberExhausted)
            throw new ConflictException(
                $"Comanda {order.OrderNumber} nu a fost marcată ca plătită: nu s-a putut aloca un număr de factură. Comanda a rămas neschimbată — reîncearcă după verificarea seriei de facturare.");

        // Cumulative histogram: a shipment that never committed can never be un-observed.
        if (processingSeconds is { } seconds)
            FotoMetrics.OrderProcessingDuration.Record(seconds);

        if (newStatus == OrderStatus.Shipped)
            _orderEmailService.FireOrderShippedEmail(order);
        else if (newStatus == OrderStatus.Delivered)
            _orderEmailService.FireOrderDeliveredEmail(order);
        else if (newStatus == OrderStatus.Paid && paidOutcome is null or PaidSaveOutcome.Created)
        {
            // Gated positively: a delivery that invoiced this order already sent both, and a second confirmation email cannot be unsent.
            _orderEmailService.FireOrderConfirmedEmail(order);
            await _awbNotifier.NotifyPaidAsync(order.Id, ct);
        }

        await _hub.Clients.All.SendAsync(
            "OrderStatusChanged", orderId, order.Status.ToString(), ct);

        // When the order enters the configured production-complete status
        // (default Shipped), purge each upload's cloud original. Synchronous — adds
        // ~50–100 ms per upload to this admin PATCH but keeps the lifecycle ordering
        // simple. Gated on archive-on + cloud-on like the cancel path: with the supported
        // Provider=local config the purger's self-refusal logged an Error on EVERY ship
        // (chronic false alarm). The archive-on-but-cloud-off mismatch
        // stays visible via the purge recovery scanner's boot-time cloud-tier-off log and
        // UploadCleanupJob's hourly unroutable-count warning when Cloud rows accumulate.
        if (_archiveSettings.IsProductionCompleteStatus(newStatus)
            && _archiveSettings.Enabled && _storageRouter.CloudEnabled)
        {
            // Best-effort, mirroring the cancel path: the transition is already
            // committed + emailed + broadcast, so a purge hiccup (transient DB load, client-disconnect
            // cancellation) must not 500 the PATCH. The periodic recovery sweep backstops a miss.
            try
            {
                await _originalPurger.PurgeOrderOriginalsAsync(order.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "production-complete purge failed for order {OrderNumber} — recovery sweep will retry",
                    order.OrderNumber);
            }
        }

        return BuildDetailDto(order);
    }

    // ── ZIP download ──────────────────────────────────────────────────────────

    public async Task StreamZipAsync(Guid orderId, HttpResponse response, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Upload)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        // Fail before writing any response bytes if the ZIP cannot be produced completely. A
        // Cloud-located original with the cloud tier disabled is unroutable — For(Cloud) would throw
        // mid-stream, after the headers + earlier entries are already committed to Response.Body,
        // handing the admin a truncated ZIP with no clean error.
        if (!_storageRouter.CloudEnabled &&
            order.Items.Any(i => i.Upload?.FilePath is not null &&
                                 i.Upload.StorageLocation == StorageLocation.Cloud))
        {
            throw new InvalidOperationException(
                $"Order {order.OrderNumber} has cloud-stored originals but the cloud tier is disabled " +
                "(Storage:Provider=local) — cannot build the fulfilment ZIP.");
        }

        response.ContentType = "application/zip";
        response.Headers.ContentDisposition =
            $"attachment; filename=\"order-{order.OrderNumber}.zip\"";
        response.Headers.CacheControl = "no-store";

        using var archive = new ZipArchive(response.Body, ZipArchiveMode.Create, leaveOpen: true);

        var idx = 1;
        foreach (var item in order.Items)
        {
            if (item.Upload?.FilePath is null) continue;

            var ext = Path.GetExtension(item.Upload.OriginalFileName);
            var entryName = $"{idx:D3}_{item.ProductSnapshot.ProductName}{ext}";
            var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);

            await using var entryStream = entry.Open();
            // Route by the upload's tier — a promoted (Cloud) order's original lives in the
            // object store, not on local disk, once promotion has run.
            await using var fileStream = await _storageRouter
                .For(item.Upload.StorageLocation)
                .GetStreamAsync(item.Upload.FilePath, ct);
            await fileStream.CopyToAsync(entryStream, ct);

            idx++;
        }
    }

    // ── Cancel + refund ───────────────────────────────────────────────────────

    public async Task<AdminOrderDetailDto> CancelOrderAsync(Guid orderId, string? reason, CancellationToken ct = default)
    {
        var order = await LoadFullOrderAsync(orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        OrderStatusMachine.Transition(order, OrderStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var existingNotes = string.IsNullOrWhiteSpace(order.InternalNotes) ? "" : order.InternalNotes + "\n";
            order.InternalNotes = $"{existingNotes}[Anulat de admin] Motiv: {reason}";
        }

        await _db.SaveChangesAsync(ct);

        try
        {
            if (order.PaymentProcessor == Models.PaymentProcessor.Stripe &&
                !string.IsNullOrEmpty(order.PaymentIntentId))
            {
                var refundSvc = new RefundService(_stripeClient);
                await refundSvc.CreateAsync(
                    new RefundCreateOptions { PaymentIntent = order.PaymentIntentId },
                    cancellationToken: ct);
            }
            else if (order.PaymentProcessor == Models.PaymentProcessor.EuPlatesc &&
                     !string.IsNullOrEmpty(order.EuPlatescTransactionId))
            {
                await _euPlatescService.RefundAsync(
                    order.EuPlatescTransactionId, order.TotalRon, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Refund failed for cancelled order {OrderNumber} — manual refund required",
                order.OrderNumber);
        }

        await _hub.Clients.All.SendAsync(
            "OrderStatusChanged", orderId, OrderStatus.Cancelled.ToString(), ct);

        _orderEmailService.FireOrderCancelledEmail(order, reason);

        // A cancelled/refunded order's cloud original must be
        // purged too (owner decision — minimise storage/GDPR exposure). Runs after the refund so
        // it never delays the money path. Best-effort cleanup: gated on the cloud tier + archive
        // being on (cancel with cloud off has nothing to purge, and the purger's refusal logs at
        // Error, which would false-alarm on every cancel in a local-only deployment — unlike the
        // production-complete hook, where cloud-off IS a misconfiguration worth surfacing), and a
        // purge hiccup must never fail the already-committed cancel + refund. The periodic
        // recovery sweep (now including Cancelled) backstops the promotion-in-flight-at-cancel race.
        if (_archiveSettings.Enabled && _storageRouter.CloudEnabled)
        {
            try
            {
                await _originalPurger.PurgeOrderOriginalsAsync(order.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "purge-on-cancel failed for order {OrderNumber} — recovery sweep will retry",
                    order.OrderNumber);
            }
        }

        return BuildDetailDto(order);
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    public async Task<AdminOrderDetailDto> UpdateNotesAsync(
        Guid orderId, string? notes, CancellationToken ct = default)
    {
        var order = await LoadFullOrderAsync(orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        order.InternalNotes = notes;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return BuildDetailDto(order);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Models.Order?> LoadFullOrderAsync(Guid orderId, CancellationToken ct)
        => await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.EasyboxLocker)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    private static string ResolveEmail(Models.Order order)
        => order.User?.Email ?? order.GuestEmail ?? "";

    private static string ResolveName(Models.Order order)
        => order.User?.FirstName ?? order.ShippingAddress?.RecipientName ?? "";

    private static AdminOrderSummaryDto BuildSummaryDto(Models.Order order) => new(
        order.Id,
        order.OrderNumber,
        order.Status.ToString(),
        ResolveEmail(order),
        ResolveName(order),
        order.TotalRon,
        order.CreatedAt,
        order.Items.Sum(i => i.Quantity),
        order.DeliveryType.ToString());

    private static AdminOrderDetailDto BuildDetailDto(Models.Order order)
    {
        var isEasybox = order.DeliveryType == DeliveryType.Easybox;

        ShippingAddressDto? shippingAddress = null;
        if (!isEasybox && order.ShippingAddress is { } addr)
        {
            shippingAddress = new ShippingAddressDto(
                addr.RecipientName, addr.Street, addr.Number, addr.Block,
                addr.City, addr.County, addr.PostalCode, addr.Phone);
        }

        var items = order.Items.Select(i => new AdminOrderItemDto(
            i.UploadId,
            i.ProductSnapshot.ProductName,
            i.ProductSnapshot.Size,
            i.ProductSnapshot.Finish,
            i.Quantity,
            i.UnitPriceRon,
            i.LineTotalRon)).ToList();

        return new AdminOrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            ResolveEmail(order),
            ResolveName(order),
            order.SubtotalRon,
            order.ShippingCostRon,
            order.TotalRon,
            order.CreatedAt,
            order.PaidAt,
            order.DeliveryType.ToString(),
            order.EasyboxLocker?.Name,
            order.EasyboxLocker?.Address,
            shippingAddress,
            order.PaymentProcessor.ToString(),
            order.PaymentIntentId,
            order.EuPlatescTransactionId,
            order.AwbNumber,
            order.TrackingUrl,
            order.AwbLabelUrl,
            order.ShippedAt,
            order.DeliveredAt,
            order.InternalNotes,
            items);
    }

    // Only Created may run the caller's Paid side effects; the webhook's own enum is private and pinned by a test that reflects on it.
    private enum PaidSaveOutcome { Created, AlreadyInvoiced, NumberExhausted }

    // Mirrors the webhook Paid path: a concurrent delivery or taken number must not 500 an admin status change.
    private async Task<PaidSaveOutcome> SaveWithInvoiceAsync(Order order, CancellationToken ct)
    {
        const int maxNumberRetries = 3;
        for (var attempt = 0; ; attempt++)
        {
            var invoice = await _invoiceCreator.CreateForOrderAsync(order, ct);

            // The creation service's existence query returns a winner's committed row as an unchanged entity, so this window throws nothing to catch.
            if (invoice is not null && _db.Entry(invoice).State == EntityState.Unchanged)
                return await AbandonToWinnerAsync(order, "pre-insert", ct);

            try
            {
                await _db.SaveChangesAsync(ct);
                return PaidSaveOutcome.Created;
            }
            catch (DbUpdateException ex) when (InvoiceUniqueViolation.IsOrderIdViolation(ex))
            {
                if (invoice is not null) _db.Entry(invoice).State = EntityState.Detached;
                return await AbandonToWinnerAsync(order, "unique-index", ct);
            }
            catch (DbUpdateException ex) when (attempt < maxNumberRetries && InvoiceUniqueViolation.IsNumberViolation(ex))
            {
                if (invoice is not null) _db.Entry(invoice).State = EntityState.Detached;
                _logger.LogWarning(
                    "admin.order.invoice-number-collision-retry order_id={OrderId} attempt={Attempt}",
                    order.Id, attempt);
            }
            catch (DbUpdateException ex) when (InvoiceUniqueViolation.IsNumberViolation(ex))
            {
                if (invoice is not null) _db.Entry(invoice).State = EntityState.Detached;
                // Logged before the rollback discards them: on an offline reconciliation these are the only handles on the payment being recorded.
                _logger.LogError(ex,
                    "admin.order.invoice-number-collision-exhausted order_id={OrderId} order_number={OrderNumber} total_ron={TotalRon} payment_intent_id={PaymentIntentId} euplatesc_transaction_id={EuPlatescTransactionId} — order not marked Paid, manual reconciliation required",
                    order.Id, order.OrderNumber, order.TotalRon, order.PaymentIntentId, order.EuPlatescTransactionId);
                // A conflict is a 4xx and the request pipeline captures only 5xx, so nothing downstream would page anyone.
                _sentry?.CaptureException(ex);

                await RollBackTransitionAsync(order, ct);
                return PaidSaveOutcome.NumberExhausted;
            }
        }
    }

    // A reload, not a second save: saving here would commit the admin's own PaidAt over the one the winner's invoice was issued against.
    private async Task<PaidSaveOutcome> AbandonToWinnerAsync(Order order, string window, CancellationToken ct)
    {
        _logger.LogInformation(
            "admin.order.invoice-already-created order_id={OrderId} window={Window}", order.Id, window);

        // Swallowed like the rollback's: a lost race is a benign outcome, and a failing reload here would turn it into a 500 plus a Sentry capture for an order that is Paid.
        try
        {
            await _db.Entry(order).ReloadAsync(ct);
        }
        catch (Exception reloadEx)
        {
            _logger.LogWarning(reloadEx,
                "admin.order.abandon-reload-failed order_id={OrderId}", order.Id);
        }

        return PaidSaveOutcome.AlreadyInvoiced;
    }

    private async Task RollBackTransitionAsync(Order order, CancellationToken ct)
    {
        // Reload rather than unwind field by field, and swallow a failure: the caller answers a conflict either way, and a throw here would turn it into an unexplained 500.
        try
        {
            await _db.Entry(order).ReloadAsync(ct);
        }
        catch (Exception reloadEx)
        {
            _logger.LogWarning(reloadEx,
                "admin.order.rollback-reload-failed order_id={OrderId}", order.Id);
        }
    }
}
