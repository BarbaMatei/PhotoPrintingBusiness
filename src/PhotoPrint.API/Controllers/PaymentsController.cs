using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Filters;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]
[RequestSizeLimit(MaxRequestBodyBytes)]
[ServiceFilter(typeof(DetectLegacyShippingCostFilter))]
[ServiceFilter(typeof(IdempotencyKeyFilter))]
public class PaymentsController : ControllerBase
{
    // CreateOrderRequest is an enum plus one length-bounded address (~2 KB at its longest), and a guest token is free, so the body DetectLegacyShippingCostFilter buffers needs a ceiling.
    public const int MaxRequestBodyBytes = 64 * 1024;

    private readonly IOrderService _orderService;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IOrderService orderService,
        IStripePaymentGateway stripeGateway,
        PhotoPrintDbContext db,
        ILogger<PaymentsController> logger)
    {
        _orderService = orderService;
        _stripeGateway = stripeGateway;
        _db = db;
        _logger = logger;
    }

    // ── POST /api/payments/stripe/intent ──────────────────────────────────────

    [HttpPost("stripe/intent")]
    [ProducesResponseType(typeof(StripeIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // The 409 body carries the divergentFields extension — type it so
    // generated clients see the field that tells the FE which inputs to fix.
    [ProducesResponseType(typeof(IdempotencyConflictProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStripeIntentAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();
        var idempotencyKey = HttpContext.GetIdempotencyKey();

        var result = await _orderService.CreateFromCartAsync(
            userId, guestSessionId, request, idempotencyKey, cancellationToken);
        var order = result.Order;

        if (result.WasIdempotentReplay && order.StripeClientSecret is not null)
        {
            // Replay with the secret already persisted → return it, no gateway call.
            _logger.LogInformation(
                "payments.idempotency.replay order_id={OrderId}", order.Id);
            return Ok(new StripeIntentResponse(order.StripeClientSecret, order.Id));
        }

        if (result.WasIdempotentReplay)
        {
            // Recovery replay — an earlier attempt created this order but died before
            // persisting the client secret. Re-invoking Stripe below is safe (the intent
            // is keyed by the stable order id → same PaymentIntent, no double charge)
            // but it is a distinct completion path, so log it as such.
            _logger.LogInformation(
                "payments.idempotency.replay-recovery order_id={OrderId}", order.Id);
        }

        var amountBani = (long)(order.TotalRon * 100);
        // Key Stripe by the order id (stable per order), not the client
        // Idempotency-Key, so a recycled client key can't collide at Stripe.
        var (clientSecret, paymentIntentId) = await _stripeGateway.CreatePaymentIntentAsync(
            amountBani, "ron", order.Id.ToString(), order.Id.ToString(), cancellationToken);
        order.PaymentIntentId = paymentIntentId;
        order.StripeClientSecret = clientSecret;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new StripeIntentResponse(clientSecret, order.Id));
    }
}
