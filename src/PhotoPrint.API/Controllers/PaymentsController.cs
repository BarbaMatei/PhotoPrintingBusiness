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
[ServiceFilter(typeof(IdempotencyKeyFilter))]
public class PaymentsController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly IEuPlatescService _euPlatescService;
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<PaymentsController> _logger;

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
    public Task<IActionResult> CreateStripeIntentAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
        => CreateIntentAsync(
            request,
            processor: "Stripe",
            cachedValue: o => o.StripeClientSecret,
            computeAndApplyAsync: async o =>
            {
                var amountBani = (long)(o.TotalRon * 100);
                // BUG-4: key Stripe by the order id (stable per order), not the client
                // Idempotency-Key, so a recycled client key can't collide at Stripe.
                var (clientSecret, paymentIntentId) = await _stripeGateway.CreatePaymentIntentAsync(
                    amountBani, "ron", o.Id.ToString(), o.Id.ToString(), cancellationToken);
                o.PaymentIntentId = paymentIntentId;
                o.StripeClientSecret = clientSecret;
                return clientSecret;
            },
            buildResponse: (o, secret) => new StripeIntentResponse(secret, o.Id),
            cancellationToken);

    // ── POST /api/payments/euplatesc/initiate ─────────────────────────────────

    [HttpPost("euplatesc/initiate")]
    [ProducesResponseType(typeof(EuPlatescInitiateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> InitiateEuPlatescAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
        => CreateIntentAsync(
            request,
            processor: "EuPlatesc",
            cachedValue: o => o.EuPlatescRedirectUrl,
            computeAndApplyAsync: o =>
            {
                // The redirect URL embeds a timestamp + nonce, so it is persisted and
                // replayed verbatim rather than rebuilt on a later call.
                var redirectUrl = _euPlatescService.BuildInitiateUrl(o);
                o.EuPlatescRedirectUrl = redirectUrl;
                return Task.FromResult(redirectUrl);
            },
            buildResponse: (o, url) => new EuPlatescInitiateResponse(url, o.Id),
            cancellationToken);

    /// <summary>
    /// QUAL-4: the replay/compute/persist shape shared by both processors. Resolve the
    /// (idempotent) order; if this is a replay and the processor's value is already
    /// cached, return it without touching the gateway; otherwise compute it
    /// (<paramref name="computeAndApplyAsync"/> calls the gateway and writes the order's
    /// fields), persist, and return. The Idempotency-Key is read from
    /// <see cref="HttpContext"/> where <see cref="IdempotencyKeyFilter"/> stashed it (QUAL-3).
    /// </summary>
    private async Task<IActionResult> CreateIntentAsync<TResponse>(
        CreateOrderRequest request,
        string processor,
        Func<Order, string?> cachedValue,
        Func<Order, Task<string>> computeAndApplyAsync,
        Func<Order, string, TResponse> buildResponse,
        CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();
        var idempotencyKey = HttpContext.GetIdempotencyKey();

        var result = await _orderService.CreateFromCartAsync(
            userId, guestSessionId, request, idempotencyKey, ct);
        var order = result.Order;

        // Idempotent replay with the value already cached → return it, no gateway call.
        var cached = cachedValue(order);
        if (result.WasIdempotentReplay && cached is not null)
        {
            _logger.LogInformation(
                "payments.idempotency.replay processor={Processor} order_id={OrderId}", processor, order.Id);
            return Ok(buildResponse(order, cached));
        }

        var value = await computeAndApplyAsync(order);
        await _db.SaveChangesAsync(ct);
        return Ok(buildResponse(order, value));
    }
}
