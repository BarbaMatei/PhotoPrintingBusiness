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
    private readonly ILogger<AdminOrderService> _logger;

    public AdminOrderService(
        PhotoPrintDbContext db,
        IOrderEmailService orderEmailService,
        IEuPlatescService euPlatescService,
        IStripeClient stripeClient,
        IStorageRouter storageRouter,
        IOriginalPurger originalPurger,
        IOptions<ArchiveSettings> archiveSettings,
        IHubContext<AdminOrderHub> hub,
        ILogger<AdminOrderService> logger)
    {
        _db = db;
        _orderEmailService = orderEmailService;
        _euPlatescService = euPlatescService;
        _stripeClient = stripeClient;
        _storageRouter = storageRouter;
        _originalPurger = originalPurger;
        _archiveSettings = archiveSettings.Value;
        _hub = hub;
        _logger = logger;
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
        // order total (F2, review 042-v8).
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

        if (newStatus == OrderStatus.Shipped)
        {
            order.AwbNumber = awbNumber;
            order.TrackingUrl = trackingUrl;
        }

        await _db.SaveChangesAsync(ct);

        if (newStatus == OrderStatus.Shipped)
            _orderEmailService.FireOrderShippedEmail(order);
        else if (newStatus == OrderStatus.Delivered)
            _orderEmailService.FireOrderDeliveredEmail(order);

        await _hub.Clients.All.SendAsync(
            "OrderStatusChanged", orderId, order.Status.ToString(), ct);

        // Bolt 052: when the order enters the configured production-complete status
        // (default Shipped), purge each upload's cloud original. Synchronous — adds
        // ~50–100 ms per upload to this admin PATCH but keeps the lifecycle ordering
        // simple. Self-refuses if the cloud tier is off or archive is disabled.
        if (_archiveSettings.IsProductionCompleteStatus(newStatus))
            await _originalPurger.PurgeOrderOriginalsAsync(order.Id, ct);

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
            // object store, not on local disk, once promotion has run (F1, review 043-v1).
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
            order.InternalNotes,
            items);
    }
}
