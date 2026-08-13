using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Sameday;
using Stripe;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IStripeSignatureVerifier _stripeVerifier;
    private readonly IEuPlatescService _euPlatescService;
    private readonly PhotoPrintDbContext _db;
    private readonly IOrderEmailService _orderEmailService;
    private readonly IOrderPhotoPromoter _photoPromoter;
    private readonly IAwbCreationNotifier _awbNotifier;
    private readonly IHubContext<AdminOrderHub> _hub;
    private readonly StripeSettings _stripeSettings;
    private readonly EuPlatescSettings _euPlatescSettings;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IOrderService orderService,
        IStripeSignatureVerifier stripeVerifier,
        IEuPlatescService euPlatescService,
        PhotoPrintDbContext db,
        IOrderEmailService orderEmailService,
        IOrderPhotoPromoter photoPromoter,
        IAwbCreationNotifier awbNotifier,
        IHubContext<AdminOrderHub> hub,
        IOptions<StripeSettings> stripeSettings,
        IOptions<EuPlatescSettings> euPlatescSettings,
        ILogger<WebhooksController> logger)
    {
        _orderService = orderService;
        _stripeVerifier = stripeVerifier;
        _euPlatescService = euPlatescService;
        _db = db;
        _orderEmailService = orderEmailService;
        _photoPromoter = photoPromoter;
        _awbNotifier = awbNotifier;
        _hub = hub;
        _stripeSettings = stripeSettings.Value;
        _euPlatescSettings = euPlatescSettings.Value;
        _logger = logger;
    }

    // ── POST /api/webhooks/stripe ─────────────────────────────────────────────

    [HttpPost("stripe")]
    [AllowAnonymous]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> StripeWebhookAsync(CancellationToken cancellationToken)
    {
        string json;
        using (var reader = new System.IO.StreamReader(Request.Body))
        {
            json = await reader.ReadToEndAsync(cancellationToken);
        }

        var signature = Request.Headers["Stripe-Signature"].ToString();

        // Verify signature — only StripeException indicates tampered payload
        string eventType;
        try
        {
            var stripeEvent = _stripeVerifier.ConstructEvent(json, signature, _stripeSettings.WebhookSecret);
            eventType = stripeEvent.Type ?? "";
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Stripe webhook signature invalid: {Message}", ex.Message);
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.SignatureInvalid);
            return BadRequest("Invalid Stripe signature.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook event parse error");
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.Failed);
            return BadRequest("Could not parse Stripe event.");
        }

        // Extract PaymentIntent ID directly from the raw JSON body.
        // This avoids dependency on stripeEvent.Data.Object deserialization
        // which can vary across Stripe.net minor versions.
        string? paymentIntentId = null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var dataEl) &&
                dataEl.TryGetProperty("object", out var objEl) &&
                objEl.TryGetProperty("id", out var idEl))
            {
                paymentIntentId = idEl.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract PaymentIntent ID from Stripe event JSON");
        }

        switch (eventType)
        {
            case "payment_intent.succeeded":
                await HandleStripePaymentSucceededAsync(paymentIntentId, cancellationToken);
                break;

            case "payment_intent.payment_failed":
                await HandleStripePaymentFailedAsync(paymentIntentId, cancellationToken);
                break;

            default:
                _logger.LogDebug("Unhandled Stripe event type: {Type}", eventType);
                break;
        }

        return Ok();
    }

    // ── POST /api/webhooks/euplatesc ──────────────────────────────────────────

    [HttpPost("euplatesc")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> EuPlatescIpnAsync(
        [FromForm] IFormCollection form,
        CancellationToken cancellationToken)
    {
        var fields = form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        var secretKey = _euPlatescSettings.SecretKey;

        // Validate HMAC signature
        if (!EuPlatescService.ValidateIpnSignature(fields, secretKey))
        {
            _logger.LogWarning("EuPlatesc IPN signature validation failed");
            RecordPaymentWebhook(MetricNames.ProcessorValues.EuPlatesc,
                MetricNames.WebhookResultValues.SignatureInvalid);
            return Content("<epayment>error</epayment>", "text/plain");
        }

        // Resolve order
        if (!fields.TryGetValue("invoice_id", out var invoiceIdStr) ||
            !Guid.TryParse(invoiceIdStr, out var orderId))
        {
            _logger.LogWarning("EuPlatesc IPN: missing or invalid invoice_id");
            RecordPaymentWebhook(MetricNames.ProcessorValues.EuPlatesc,
                MetricNames.WebhookResultValues.Failed);
            return Content("<epayment>error</epayment>", "text/plain");
        }

        var order = await _orderService.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("EuPlatesc IPN: order {OrderId} not found", orderId);
            RecordPaymentWebhook(MetricNames.ProcessorValues.EuPlatesc,
                MetricNames.WebhookResultValues.OrderNotFound);
            return Content(EuPlatescService.BuildIpnResponse(secretKey), "text/plain");
        }

        // Amount validation — reject silently (per EuPlatesc spec, still return 200)
        if (fields.TryGetValue("amount", out var amountStr))
        {
            var expected = order.TotalRon.ToString("F2", CultureInfo.InvariantCulture);
            if (amountStr != expected)
            {
                _logger.LogWarning(
                    "EuPlatesc IPN amount mismatch: expected {Expected}, received {Received} for order {OrderId}",
                    expected, amountStr, orderId);
                RecordPaymentWebhook(MetricNames.ProcessorValues.EuPlatesc,
                    MetricNames.WebhookResultValues.AmountMismatch);
                return Content(EuPlatescService.BuildIpnResponse(secretKey), "text/plain");
            }
        }

        // Process action
        var action = fields.GetValueOrDefault("action", "");

        if (action == "0" && order.Status == OrderStatus.AwaitingPayment)
        {
            var transactionId = fields.GetValueOrDefault("ep_id", "");
            OrderStatusMachine.Transition(order, OrderStatus.Paid);
            order.PaidAt = DateTimeOffset.UtcNow;
            order.EuPlatescTransactionId = transactionId;
            await _db.SaveChangesAsync(cancellationToken);
            RecordPaymentWebhook(MetricNames.ProcessorValues.EuPlatesc,
                MetricNames.WebhookResultValues.Ok);
            await BroadcastNewOrderAsync(order, cancellationToken);
            await FireOrderConfirmedEmailAsync(order, cancellationToken);
            // Enqueue cloud promotion off the hot path. Returns immediately;
            // the worker picks up and uploads asynchronously.
            await _photoPromoter.EnqueueAsync(order.Id, cancellationToken);
            // Enqueue Sameday AWB creation off the hot path.
            // No-op when Sameday:Jobs:Enabled = false (NullAwbCreationNotifier).
            await _awbNotifier.NotifyPaidAsync(order.Id, cancellationToken);
        }
        else if (action == "0" && OrderStatusMachine.HasBeenPaid(order.Status))
        {
            // Duplicate IPN for an already-paid order — Stripe-equivalent duplicate path.
            RecordPaymentWebhook(MetricNames.ProcessorValues.EuPlatesc,
                MetricNames.WebhookResultValues.Duplicate);
        }
        else if (action != "0" && order.Status == OrderStatus.AwaitingPayment)
        {
            OrderStatusMachine.Transition(order, OrderStatus.PaymentFailed);
            await _db.SaveChangesAsync(cancellationToken);
            RecordPaymentWebhook(MetricNames.ProcessorValues.EuPlatesc,
                MetricNames.WebhookResultValues.Failed);
        }
        else
        {
            if (action == "0")
                _logger.LogError(
                    "EuPlatesc IPN: paid notification for order {OrderId} in status {Status} — customer charged, order not Paid, manual reconciliation required",
                    orderId, order.Status);
            else
                _logger.LogWarning(
                    "EuPlatesc IPN: action {Action} for order {OrderId} in status {Status} — no transition applied",
                    action, orderId, order.Status);

            RecordPaymentWebhook(MetricNames.ProcessorValues.EuPlatesc,
                MetricNames.WebhookResultValues.Failed);
        }

        return Content(EuPlatescService.BuildIpnResponse(secretKey), "text/plain");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task HandleStripePaymentSucceededAsync(string? paymentIntentId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(paymentIntentId))
        {
            _logger.LogWarning("Stripe webhook: payment_intent.succeeded but no PaymentIntent ID found");
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.Failed);
            return;
        }

        var order = await _orderService.GetByPaymentIntentIdAsync(paymentIntentId, ct);
        if (order == null)
        {
            _logger.LogWarning(
                "Stripe webhook: PaymentIntent {Id} not linked to any order", paymentIntentId);
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.OrderNotFound);
            return;
        }

        // Idempotency: silently ignore if already paid
        if (OrderStatusMachine.HasBeenPaid(order.Status))
        {
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.Duplicate);
            return;
        }

        if (order.Status == OrderStatus.AwaitingPayment)
        {
            OrderStatusMachine.Transition(order, OrderStatus.Paid);
            order.PaidAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.Ok);
            await BroadcastNewOrderAsync(order, ct);
            await FireOrderConfirmedEmailAsync(order, ct);
            // Enqueue cloud promotion off the hot path. Returns immediately;
            // the worker picks up and uploads asynchronously.
            await _photoPromoter.EnqueueAsync(order.Id, ct);
            // Enqueue Sameday AWB creation off the hot path.
            // No-op when Sameday:Jobs:Enabled = false (NullAwbCreationNotifier).
            await _awbNotifier.NotifyPaidAsync(order.Id, ct);
        }
        else
        {
            _logger.LogError(
                "Stripe webhook: payment_intent.succeeded for order {OrderId} in status {Status} — customer charged, order not Paid, manual reconciliation required",
                order.Id, order.Status);
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.Failed);
        }
    }

    private async Task HandleStripePaymentFailedAsync(string? paymentIntentId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(paymentIntentId))
        {
            _logger.LogWarning("Stripe webhook: payment_intent.payment_failed but no PaymentIntent ID found");
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.Failed);
            return;
        }

        var order = await _orderService.GetByPaymentIntentIdAsync(paymentIntentId, ct);
        if (order == null)
        {
            _logger.LogWarning(
                "Stripe webhook: PaymentIntent {Id} not linked to any order", paymentIntentId);
            RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
                MetricNames.WebhookResultValues.OrderNotFound);
            return;
        }

        if (order.Status == OrderStatus.AwaitingPayment)
        {
            OrderStatusMachine.Transition(order, OrderStatus.PaymentFailed);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            _logger.LogWarning(
                "Stripe webhook: payment_intent.payment_failed for order {OrderId} in status {Status} — no transition applied",
                order.Id, order.Status);
        }

        RecordPaymentWebhook(MetricNames.ProcessorValues.Stripe,
            MetricNames.WebhookResultValues.Failed);
    }

    private static void RecordPaymentWebhook(string processor, string result)
    {
        FotoMetrics.PaymentWebhook.Add(1,
            new TagList
            {
                { MetricNames.Labels.Processor, processor },
                { MetricNames.Labels.Result,    result },
            });
    }

    private async Task BroadcastNewOrderAsync(Order order, CancellationToken ct)
    {
        await _hub.Clients.All.SendAsync("NewOrderReceived", new
        {
            order.Id,
            order.OrderNumber,
            CustomerEmail = order.User?.Email ?? order.GuestEmail ?? "",
            CustomerName = order.User?.FirstName ?? "",
            order.TotalRon,
            order.CreatedAt,
            Status = order.Status.ToString(),
        }, ct);
    }

    private async Task LoadOrderDetailsForEmailAsync(Order order, CancellationToken ct)
    {
        await _db.Entry(order).Collection(o => o.Items).LoadAsync(ct);
        await _db.Entry(order).Reference(o => o.EasyboxLocker).LoadAsync(ct);
    }

    private async Task FireOrderConfirmedEmailAsync(Order order, CancellationToken ct)
    {
        await LoadOrderDetailsForEmailAsync(order, ct);
        _orderEmailService.FireOrderConfirmedEmail(order);
    }
}
