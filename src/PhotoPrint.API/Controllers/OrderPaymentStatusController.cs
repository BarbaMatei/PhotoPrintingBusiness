using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Orders;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Controllers;

// The orders controller is signed-in only; the caller who most needs this read is a guest.
[ApiController]
[Route("api/orders/{orderId:guid}/payment-status")]
[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]
public sealed class OrderPaymentStatusController : ControllerBase
{
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<OrderPaymentStatusController> _logger;

    public OrderPaymentStatusController(
        PhotoPrintDbContext db, ILogger<OrderPaymentStatusController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OrderPaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid orderId, CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new
            {
                o.Id, o.UserId, o.GuestSessionId, o.OrderNumber,
                o.Status, o.TotalRon, o.VatRon, o.VatRate, o.CouponCode, o.DiscountRon,
                o.DeliveryType, o.CreatedAt, o.PaidAt,
            })
            .FirstOrDefaultAsync(ct);

        if (order is null) return NotFound();

        var owns = (userId is not null && order.UserId == userId.Value) ||
                   (guestSessionId is not null && order.GuestSessionId == guestSessionId.Value);
        if (!owns) return Forbid();

        if (order.Status == OrderStatus.AwaitingPayment)
            _logger.LogInformation(
                "payments.status.unsettled-read order_id={OrderId} correlation_id={CorrelationId}",
                order.Id, HttpContext.GetCorrelationId());

        Response.Headers.CacheControl = "private, no-store";

        return Ok(new OrderPaymentStatusDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.TotalRon,
            order.VatRon,
            order.VatRate,
            order.CouponCode,
            order.DiscountRon,
            order.DeliveryType.ToString(),
            order.CreatedAt,
            order.PaidAt));
    }
}
