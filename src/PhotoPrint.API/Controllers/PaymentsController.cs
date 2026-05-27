using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Filters;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]
[ServiceFilter(typeof(DetectLegacyShippingCostFilter))]
public class PaymentsController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly IEuPlatescService _euPlatescService;
    private readonly PhotoPrintDbContext _db;

    public PaymentsController(
        IOrderService orderService,
        IStripePaymentGateway stripeGateway,
        IEuPlatescService euPlatescService,
        PhotoPrintDbContext db)
    {
        _orderService = orderService;
        _stripeGateway = stripeGateway;
        _euPlatescService = euPlatescService;
        _db = db;
    }

    // ── POST /api/payments/stripe/intent ──────────────────────────────────────

    [HttpPost("stripe/intent")]
    [ProducesResponseType(typeof(StripeIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStripeIntentAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var order = await _orderService.CreateFromCartAsync(
            userId, guestSessionId, request, cancellationToken);

        var amountBani = (long)(order.TotalRon * 100);
        var (clientSecret, paymentIntentId) = await _stripeGateway.CreatePaymentIntentAsync(
            amountBani, "ron", order.Id.ToString(), cancellationToken);

        order.PaymentIntentId = paymentIntentId;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new StripeIntentResponse(clientSecret, order.Id));
    }

    // ── POST /api/payments/euplatesc/initiate ─────────────────────────────────

    [HttpPost("euplatesc/initiate")]
    [ProducesResponseType(typeof(EuPlatescInitiateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateEuPlatescAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var order = await _orderService.CreateFromCartAsync(
            userId, guestSessionId, request, cancellationToken);

        var redirectUrl = _euPlatescService.BuildInitiateUrl(order);

        return Ok(new EuPlatescInitiateResponse(redirectUrl, order.Id));
    }
}
