using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public sealed class OrderEmailService : IOrderEmailService
{
    private readonly IEmailService _email;
    private readonly string _baseUrl;
    private readonly ILogger<OrderEmailService> _logger;

    public OrderEmailService(
        IEmailService email,
        IOptions<AppSettings> app,
        ILogger<OrderEmailService> logger)
    {
        _email = email;
        _baseUrl = app.Value.BaseUrl;
        _logger = logger;
    }

    // ── Order Confirmed ───────────────────────────────────────────────────────

    public void FireOrderConfirmedEmail(Order order)
    {
        var to = order.User?.Email ?? order.GuestEmail;
        if (string.IsNullOrEmpty(to)) return;

        // Extract all primitives before Task.Run to avoid captured scoped-service risks
        var firstName = ResolveFirstName(order);
        var orderNumber = order.OrderNumber;
        var orderId = order.Id;
        var isEasybox = order.DeliveryType == DeliveryType.Easybox;
        var lockerName = order.EasyboxLocker?.Name;
        var lockerCity = order.EasyboxLocker?.City;
        var lockerAddress = order.EasyboxLocker?.Address;
        var shipAddr = isEasybox ? null : order.ShippingAddress;
        var subtotal = order.SubtotalRon;
        var shipping = order.ShippingCostRon;
        var discount = order.DiscountRon;
        var couponCode = order.CouponCode;
        var total = order.TotalRon;
        var orderUrl = $"{_baseUrl}/comenzile-mele/{orderId}";

        var items = order.Items.Select(i => new OrderItemEmailRow(
            i.ProductSnapshot.ProductName,
            i.ProductSnapshot.Size,
            i.ProductSnapshot.Finish,
            i.Quantity,
            i.UnitPriceRon,
            i.LineTotalRon)).ToList();

        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendTemplatedAsync(
                    to,
                    "Comanda ta a fost confirmată — FotoTipar",
                    "OrderConfirmed",
                    new OrderConfirmedEmailModel(
                        firstName, orderNumber, items, isEasybox,
                        lockerName, lockerCity, lockerAddress,
                        shipAddr, subtotal, shipping, discount, couponCode, total, orderUrl),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send order-confirmed email for {OrderNumber}", orderNumber);
            }
        });
    }

    // ── Order Shipped ─────────────────────────────────────────────────────────

    public void FireOrderShippedEmail(Order order)
    {
        var to = order.User?.Email ?? order.GuestEmail;
        if (string.IsNullOrEmpty(to)) return;

        var firstName = ResolveFirstName(order);
        var orderNumber = order.OrderNumber;
        var awb = order.AwbNumber;
        var tracking = order.TrackingUrl;
        var isEasybox = order.DeliveryType == DeliveryType.Easybox;
        var lockerName = order.EasyboxLocker?.Name;
        var lockerAddress = order.EasyboxLocker?.Address;

        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendTemplatedAsync(
                    to,
                    "Comanda ta a fost expediată — FotoTipar",
                    "OrderShipped",
                    new OrderShippedEmailModel(
                        firstName, orderNumber, awb, tracking,
                        isEasybox, lockerName, lockerAddress),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send order-shipped email for {OrderNumber}", orderNumber);
            }
        });
    }

    // ── Order Delivered ───────────────────────────────────────────────────────

    public void FireOrderDeliveredEmail(Order order)
    {
        var to = order.User?.Email ?? order.GuestEmail;
        if (string.IsNullOrEmpty(to)) return;

        var firstName = ResolveFirstName(order);
        var orderNumber = order.OrderNumber;
        var ordersUrl = $"{_baseUrl}/comenzile-mele";

        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendTemplatedAsync(
                    to,
                    "Comanda ta a fost livrată — FotoTipar",
                    "OrderDelivered",
                    new OrderDeliveredEmailModel(firstName, orderNumber, ordersUrl),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send order-delivered email for {OrderNumber}", orderNumber);
            }
        });
    }

    // ── Order Cancelled ───────────────────────────────────────────────────────

    public void FireOrderCancelledEmail(Order order, string? reason)
    {
        var to = order.User?.Email ?? order.GuestEmail;
        if (string.IsNullOrEmpty(to)) return;

        var firstName = ResolveFirstName(order);
        var orderNumber = order.OrderNumber;
        var orderId = order.Id;
        var total = order.TotalRon;
        var orderUrl = $"{_baseUrl}/comenzile-mele/{orderId}";

        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendTemplatedAsync(
                    to,
                    "Comanda ta a fost anulată — FotoTipar",
                    "OrderCancelled",
                    new OrderCancelledEmailModel(
                        firstName, orderNumber, total, reason, orderUrl),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send order-cancelled email for {OrderNumber}", orderNumber);
            }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ResolveFirstName(Order order)
    {
        if (order.User?.FirstName is { Length: > 0 } fn) return fn;
        var recipient = order.ShippingAddress?.RecipientName ?? "";
        return recipient.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Client";
    }
}

// ── View model records (used only by email templates) ─────────────────────────

public sealed record OrderItemEmailRow(
    string ProductName,
    string Size,
    string Finish,
    int Quantity,
    decimal UnitPriceRon,
    decimal LineTotalRon);

public sealed record OrderConfirmedEmailModel(
    string FirstName,
    string OrderNumber,
    IReadOnlyList<OrderItemEmailRow> Items,
    bool IsEasybox,
    string? LockerName,
    string? LockerCity,
    string? LockerAddress,
    ShippingAddressSnapshot? ShippingAddress,
    decimal SubtotalRon,
    decimal ShippingCostRon,
    decimal DiscountRon,
    string? CouponCode,
    decimal TotalRon,
    string OrderUrl);

public sealed record OrderShippedEmailModel(
    string FirstName,
    string OrderNumber,
    string? AwbNumber,
    string? TrackingUrl,
    bool IsEasybox,
    string? LockerName,
    string? LockerAddress);

public sealed record OrderDeliveredEmailModel(
    string FirstName,
    string OrderNumber,
    string OrdersUrl);

public sealed record OrderCancelledEmailModel(
    string FirstName,
    string OrderNumber,
    decimal TotalRon,
    string? CancellationReason,
    string OrderUrl);
