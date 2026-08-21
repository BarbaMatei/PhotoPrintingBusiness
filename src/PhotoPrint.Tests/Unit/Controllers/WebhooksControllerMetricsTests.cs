using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.Data;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.API.Services.Sameday;
using PhotoPrint.Tests.Helpers;
using Stripe;

namespace PhotoPrint.Tests.Unit.Controllers;

// A branch that records nothing keeps a charged-but-unpaid order out of the SLO
// denominator, so the payment SLO reads 100% while customers are losing money.
public class WebhooksControllerMetricsTests
{
    private const string SecretKeyHex = "00112233445566778899aabbccddeeff";

    private readonly Mock<IOrderService> _orderService = new();
    private readonly Mock<IStripeSignatureVerifier> _stripeVerifier = new();
    private readonly Mock<IEuPlatescService> _euPlatesc = new();
    private readonly Mock<IOrderEmailService> _emailSvc = new();
    private readonly Mock<IOrderPhotoPromoter> _promoter = new();
    private readonly Mock<IAwbCreationNotifier> _awbNotifier = new();
    private readonly Mock<IInvoiceCreationService> _invoiceCreator = new();
    private readonly Mock<IHubContext<AdminOrderHub>> _hub = new();
    private readonly Mock<IHubClients> _hubClients = new();
    private readonly Mock<IClientProxy> _clientProxy = new();
    private readonly LogCapture _logs = new();
    private readonly string _dbName = $"Webhooks_{Guid.NewGuid():N}";
    private readonly PhotoPrintDbContext _db;
    private readonly WebhooksController _sut;

    public WebhooksControllerMetricsTests()
    {
        _db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options);

        _hub.Setup(h => h.Clients).Returns(_hubClients.Object);
        _hubClients.Setup(c => c.All).Returns(_clientProxy.Object);
        _clientProxy
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _promoter.Setup(p => p.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .Returns(ValueTask.CompletedTask);
        _awbNotifier.Setup(n => n.NotifyPaidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _invoiceCreator.Setup(i => i.CreateForOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PhotoPrint.API.Models.Invoice?)null);

        _sut = new WebhooksController(
            _orderService.Object,
            _stripeVerifier.Object,
            _euPlatesc.Object,
            _db,
            _emailSvc.Object,
            _promoter.Object,
            _awbNotifier.Object,
            _invoiceCreator.Object,
            _hub.Object,
            Options.Create(new StripeSettings { WebhookSecret = "whsec_test" }),
            Options.Create(new EuPlatescSettings { SecretKey = SecretKeyHex, MerchantId = "M1" }),
            _logs.LoggerFor<WebhooksController>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Order SeedOrder(OrderStatus status, decimal total = 45m)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
            Status = status,
            PaymentProcessor = PaymentProcessor.EuPlatesc,
            DeliveryType = DeliveryType.Courier,
            TotalRon = total,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Alice Pop", Phone = "0700000000",
                Street = "Str. Test", Number = "1",
                City = "București", County = "Ilfov", PostalCode = "010000",
            },
        };
        _db.Orders.Add(order);
        _db.SaveChanges();
        _orderService.Setup(s => s.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(order);
        return order;
    }

    private static FormCollection SignedIpn(Guid orderId, string action, decimal amount)
    {
        var fields = new Dictionary<string, string>
        {
            ["amount"] = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["curr"] = "RON",
            ["invoice_id"] = orderId.ToString(),
            ["ep_id"] = "EP-1",
            ["merch_id"] = "M1",
            ["action"] = action,
            ["message"] = "",
            ["approval"] = "",
            ["timestamp"] = "20260803120000",
            ["nonce"] = "abc",
        };

        var ordered = new[]
        {
            "amount", "curr", "invoice_id", "ep_id",
            "merch_id", "action", "message", "approval",
            "timestamp", "nonce",
        };
        fields["fp"] = EuPlatescService.ComputeHmac(
            SecretKeyHex, ordered.Select(k => fields[k]).ToArray());

        return new FormCollection(
            fields.ToDictionary(kvp => kvp.Key, kvp => new Microsoft.Extensions.Primitives.StringValues(kvp.Value)));
    }

    private void StripeEventIs(string type) =>
        _stripeVerifier
            .Setup(v => v.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Event { Type = type });

    private void GivenStripeBody(string paymentIntentId)
    {
        var json = "{\"data\":{\"object\":{\"id\":\"" + paymentIntentId + "\"}}}";
        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)) },
            },
        };
    }

    private static MetricCapture Capture() =>
        new(MetricNames.Instruments.PaymentWebhookTotal);

    private PhotoPrintDbContext FreshDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>().UseInMemoryDatabase(_dbName).Options);

    // ── EuPlatesc fall-through: the charged-but-unpaid case ───────────────────

    [Fact]
    public async Task EuPlatesc_paid_notification_for_a_cancelled_order_records_failed_and_logs_error()
    {
        var order = SeedOrder(OrderStatus.Cancelled);
        using var metrics = Capture();

        await _sut.EuPlatescIpnAsync(SignedIpn(order.Id, action: "0", order.TotalRon), default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.EuPlatesc),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Failed))
            .Should().HaveCount(1, "a customer charged for an order that cannot become Paid must enter the SLO denominator");
        metrics.ContractViolations().Should().BeEmpty();

        _logs.Records.Should().Contain(r =>
            r.Level == Microsoft.Extensions.Logging.LogLevel.Error &&
            r.Message.Contains("customer charged"));
    }

    [Fact]
    public async Task EuPlatesc_failure_notification_for_a_paid_order_records_exactly_one_increment()
    {
        var order = SeedOrder(OrderStatus.Paid);
        using var metrics = Capture();

        await _sut.EuPlatescIpnAsync(SignedIpn(order.Id, action: "1", order.TotalRon), default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal).Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();
    }

    [Fact]
    public async Task EuPlatesc_paid_notification_for_an_awaiting_order_still_records_ok()
    {
        var order = SeedOrder(OrderStatus.AwaitingPayment);
        using var metrics = Capture();

        await _sut.EuPlatescIpnAsync(SignedIpn(order.Id, action: "0", order.TotalRon), default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Ok))
            .Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();
    }

    // ── Stripe succeeded: fall-through past the AwaitingPayment guard ─────────

    [Fact]
    public async Task Stripe_succeeded_for_a_cancelled_order_records_failed_and_logs_error()
    {
        var order = SeedOrder(OrderStatus.Cancelled);
        _orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(order);
        StripeEventIs("payment_intent.succeeded");
        GivenStripeBody("pi_1");
        using var metrics = Capture();

        await _sut.StripeWebhookAsync(default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Failed))
            .Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();

        _logs.Records.Should().Contain(r =>
            r.Level == Microsoft.Extensions.Logging.LogLevel.Error &&
            r.Message.Contains("customer charged"));
    }

    // ── Stripe payment_failed: the two silent returns and the missing else ────

    [Fact]
    public async Task Stripe_payment_failed_without_a_payment_intent_id_records_failed()
    {
        StripeEventIs("payment_intent.payment_failed");
        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}")) },
            },
        };
        using var metrics = Capture();

        await _sut.StripeWebhookAsync(default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Failed))
            .Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();
    }

    [Fact]
    public async Task Stripe_payment_failed_for_an_unknown_order_records_order_not_found()
    {
        _orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_missing", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Order?)null);
        StripeEventIs("payment_intent.payment_failed");
        GivenStripeBody("pi_missing");
        using var metrics = Capture();

        await _sut.StripeWebhookAsync(default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.OrderNotFound))
            .Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();
    }

    [Fact]
    public async Task Stripe_payment_failed_for_an_already_paid_order_records_exactly_one_increment()
    {
        var order = SeedOrder(OrderStatus.Paid);
        _orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_2", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(order);
        StripeEventIs("payment_intent.payment_failed");
        GivenStripeBody("pi_2");
        using var metrics = Capture();

        await _sut.StripeWebhookAsync(default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal).Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();
    }

    // ── A paid order that has moved on is a duplicate, not a lost payment ─────

    [Theory]
    [InlineData(OrderStatus.Printing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task Stripe_succeeded_for_an_order_past_paid_records_duplicate(OrderStatus status)
    {
        var order = SeedOrder(status);
        _orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_dup", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(order);
        StripeEventIs("payment_intent.succeeded");
        GivenStripeBody("pi_dup");
        using var metrics = Capture();

        await _sut.StripeWebhookAsync(default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Duplicate))
            .Should().HaveCount(1, "the order was paid and has simply moved on, so a redelivery is a duplicate");
        metrics.For(MetricNames.Instruments.PaymentWebhookTotal).Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();

        _logs.Records.Should().NotContain(r =>
            r.Level == Microsoft.Extensions.Logging.LogLevel.Error,
            "a healthy fulfilled order must not raise a reconciliation alert");
    }

    [Theory]
    [InlineData(OrderStatus.Printing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task EuPlatesc_paid_notification_for_an_order_past_paid_records_duplicate(OrderStatus status)
    {
        var order = SeedOrder(status);
        using var metrics = Capture();

        await _sut.EuPlatescIpnAsync(SignedIpn(order.Id, action: "0", order.TotalRon), default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.EuPlatesc),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Duplicate))
            .Should().HaveCount(1, "the order was paid and has simply moved on, so a redelivery is a duplicate");
        metrics.For(MetricNames.Instruments.PaymentWebhookTotal).Should().HaveCount(1);
        metrics.ContractViolations().Should().BeEmpty();

        _logs.Records.Should().NotContain(r =>
            r.Level == Microsoft.Extensions.Logging.LogLevel.Error,
            "a healthy fulfilled order must not raise a reconciliation alert");
    }

    // ── Invoice creation on the Paid transition ───────────────────────────────

    [Fact]
    public async Task Stripe_succeeded_for_an_awaiting_order_invokes_invoice_creation()
    {
        var order = SeedOrder(OrderStatus.AwaitingPayment);
        _orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_inv", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(order);
        StripeEventIs("payment_intent.succeeded");
        GivenStripeBody("pi_inv");

        await _sut.StripeWebhookAsync(default);

        _invoiceCreator.Verify(
            i => i.CreateForOrderAsync(It.Is<Order>(o => o.Id == order.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Stripe_succeeded_when_invoice_creation_throws_leaves_order_awaiting_payment_in_the_database()
    {
        var order = SeedOrder(OrderStatus.AwaitingPayment);
        _orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_boom", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(order);
        _invoiceCreator.Setup(i => i.CreateForOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("numbering service unavailable"));
        StripeEventIs("payment_intent.succeeded");
        GivenStripeBody("pi_boom");

        var act = () => _sut.StripeWebhookAsync(default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        using var freshDb = FreshDb();
        var persisted = await freshDb.Orders.FindAsync(order.Id);
        persisted!.Status.Should().Be(OrderStatus.AwaitingPayment);
        persisted.PaidAt.Should().BeNull();
    }

    // An unclassified failure used to escape before the metric, dropping a charged customer out of the SLO.
    [Fact]
    public async Task Stripe_succeeded_when_invoice_creation_fails_unclassified_still_records_the_webhook()
    {
        var order = SeedOrder(OrderStatus.AwaitingPayment);
        _orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_boom2", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(order);
        _invoiceCreator.Setup(i => i.CreateForOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("numbering service unavailable"));
        StripeEventIs("payment_intent.succeeded");
        GivenStripeBody("pi_boom2");
        using var metrics = Capture();

        var act = () => _sut.StripeWebhookAsync(default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Failed))
            .Should().HaveCount(1, "a charge whose invoice never committed must enter the SLO denominator");
        metrics.ContractViolations().Should().BeEmpty();
    }

    [Fact]
    public async Task Stripe_succeeded_when_the_request_is_cancelled_records_nothing()
    {
        var order = SeedOrder(OrderStatus.AwaitingPayment);
        _orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_cancel", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(order);
        _invoiceCreator.Setup(i => i.CreateForOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new OperationCanceledException());
        StripeEventIs("payment_intent.succeeded");
        GivenStripeBody("pi_cancel");
        using var metrics = Capture();

        var act = () => _sut.StripeWebhookAsync(default);

        await act.Should().ThrowAsync<OperationCanceledException>();
        metrics.For(MetricNames.Instruments.PaymentWebhookTotal)
            .Should().BeEmpty("a deploy or client abort is not a payment failure");
    }

    // ── The deliberate exception: routine Stripe event types stay out ─────────

    [Fact]
    public async Task Stripe_unhandled_event_type_records_nothing()
    {
        StripeEventIs("charge.updated");
        GivenStripeBody("pi_3");
        using var metrics = Capture();

        await _sut.StripeWebhookAsync(default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal)
            .Should().BeEmpty("routine non-payment events would swamp the SLO denominator");
    }
}
