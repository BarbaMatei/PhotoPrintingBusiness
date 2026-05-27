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
    private readonly ILogger<PaymentsController> _logger;

    private const string IdempotencyHeader = "Idempotency-Key";

    public PaymentsController(
        IOrderService orderService,
        IStripePaymentGateway stripeGateway,
        IEuPlatescService euPlatescService,
        PhotoPrintDbContext db,
        ILogger<PaymentsController> logger)
    {
        _orderService = orderService;
        _stripeGateway = stripeGateway;
        _euPlatescService = euPlatescService;
        _db = db;
        _logger = logger;
    }

    // ── POST /api/payments/stripe/intent ──────────────────────────────────────

    [HttpPost("stripe/intent")]
    [ProducesResponseType(typeof(StripeIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStripeIntentAsync(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        WarnIfMissingIdempotencyKey(idempotencyKey, "stripe.intent");

        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var result = await _orderService.CreateFromCartAsync(
            userId, guestSessionId, request, idempotencyKey, cancellationToken);
        var order = result.Order;

        // Idempotent replay: return the exact same secret without touching Stripe.
        if (result.WasIdempotentReplay && order.StripeClientSecret is not null)
        {
            _logger.LogInformation(
                "payments.idempotency.replay processor=Stripe order_id={OrderId}", order.Id);
            return Ok(new StripeIntentResponse(order.StripeClientSecret, order.Id));
        }

        var amountBani = (long)(order.TotalRon * 100);
        var (clientSecret, paymentIntentId) = await _stripeGateway.CreatePaymentIntentAsync(
            amountBani, "ron", order.Id.ToString(), idempotencyKey, cancellationToken);

        order.PaymentIntentId = paymentIntentId;
        order.StripeClientSecret = clientSecret;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new StripeIntentResponse(clientSecret, order.Id));
    }

    // ── POST /api/payments/euplatesc/initiate ─────────────────────────────────

    [HttpPost("euplatesc/initiate")]
    [ProducesResponseType(typeof(EuPlatescInitiateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InitiateEuPlatescAsync(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        WarnIfMissingIdempotencyKey(idempotencyKey, "euplatesc.initiate");

        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var result = await _orderService.CreateFromCartAsync(
            userId, guestSessionId, request, idempotencyKey, cancellationToken);
        var order = result.Order;

        // Idempotent replay: return the stored redirect URL verbatim (the URL is
        // not reproducible — it embeds a timestamp + nonce).
        if (result.WasIdempotentReplay && order.EuPlatescRedirectUrl is not null)
        {
            _logger.LogInformation(
                "payments.idempotency.replay processor=EuPlatesc order_id={OrderId}", order.Id);
            return Ok(new EuPlatescInitiateResponse(order.EuPlatescRedirectUrl, order.Id));
        }

        var redirectUrl = _euPlatescService.BuildInitiateUrl(order);
        order.EuPlatescRedirectUrl = redirectUrl;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new EuPlatescInitiateResponse(redirectUrl, order.Id));
    }

    private void WarnIfMissingIdempotencyKey(string? key, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
            _logger.LogWarning(
                "payments.idempotency.missing-key endpoint={Endpoint} correlation_id={CorrelationId}",
                endpoint, correlationId);
        }
    }
}
