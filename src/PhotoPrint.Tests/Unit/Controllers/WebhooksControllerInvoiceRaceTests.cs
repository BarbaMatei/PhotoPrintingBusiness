using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
using Xunit;

namespace PhotoPrint.Tests.Unit.Controllers;

public class WebhooksControllerInvoiceRaceTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public WebhooksControllerInvoiceRaceTests(PostgresTestDatabase database)
    {
        _database = database;
        database.ResetForTest();
    }


    private PhotoPrintDbContext CreateDb() => _database.NewContext();

    private static Order MakeOrder(Guid id) => new()
    {
        Id = id,
        OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
        Status = OrderStatus.AwaitingPayment,
        NetTotalRon = 84.03m,
        VatRon = 15.97m,
        TotalRon = 100m,
        VatRate = 0.19m,
        ShippingAddress = new ShippingAddressSnapshot
        {
            RecipientName = "x", Phone = "x",
            Street = "x", Number = "1",
            City = "x", County = "x", PostalCode = "x",
        },
    };

    private static WebhooksController MakeController(
        PhotoPrintDbContext db, IInvoiceCreationService invoiceCreator,
        LogCapture? logCapture = null, Sentry.IHub? hub = null,
        IOrderService? orderService = null, IStripeSignatureVerifier? stripeVerifier = null)
    {
        var controller = MakeBareController(db, invoiceCreator, logCapture, orderService, stripeVerifier);
        var services = new ServiceCollection();
        if (hub is not null) services.AddSingleton(hub);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider(),
            },
        };
        return controller;
    }

    private static WebhooksController MakeBareController(
        PhotoPrintDbContext db, IInvoiceCreationService invoiceCreator, LogCapture? logCapture = null,
        IOrderService? orderService = null, IStripeSignatureVerifier? stripeVerifier = null)
        => new(
            orderService ?? Mock.Of<IOrderService>(),
            stripeVerifier ?? Mock.Of<IStripeSignatureVerifier>(),
            Mock.Of<IEuPlatescService>(),
            db,
            Mock.Of<IOrderEmailService>(),
            Mock.Of<IOrderPhotoPromoter>(),
            Mock.Of<IAwbCreationNotifier>(),
            invoiceCreator,
            Mock.Of<IHubContext<AdminOrderHub>>(),
            Options.Create(new StripeSettings()),
            Options.Create(new EuPlatescSettings()),
            logCapture is null
                ? NullLogger<WebhooksController>.Instance
                : logCapture.LoggerFor<WebhooksController>());

    private static Task<string> InvokeSaveAsync(WebhooksController controller, Order order, OrderStatus statusBeforeTransition)
        => InvokeSaveAsync(controller, order, statusBeforeTransition, CancellationToken.None);

    private static async Task<string> InvokeSaveAsync(
        WebhooksController controller, Order order, OrderStatus statusBeforeTransition, CancellationToken ct)
    {
        var method = typeof(WebhooksController).GetMethod("SaveOrderPaidWithInvoiceAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task)method.Invoke(controller, [order, statusBeforeTransition, ct])!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task)!.ToString()!;
    }

    private static InvoiceCreationService MakeInvoiceCreator(PhotoPrintDbContext db, IInvoiceNumberingService numbering) =>
        new(db, numbering, Options.Create(new VatSettings()), TimeProvider.System, NullLogger<InvoiceCreationService>.Instance);

    private static bool InvokeIsInvoiceOrderIdViolation(DbUpdateException ex)
    {
        var method = typeof(WebhooksController).GetMethod("IsInvoiceOrderIdViolation",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [ex])!;
    }

    private static bool InvokeIsInvoiceNumberViolation(DbUpdateException ex)
    {
        var method = typeof(WebhooksController).GetMethod("IsInvoiceNumberViolation",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [ex])!;
    }

    // Raw SQL with no model declaration, so it was the one unique index neither classifier matched.
    [Fact]
    public async Task CompositeSeriesYearNumberViolation_IsClassifiedAsANumberCollision()
    {
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        using var db = CreateDb();
        db.Orders.Add(MakeOrder(orderA));
        db.Orders.Add(MakeOrder(orderB));
        await db.SaveChangesAsync();

        db.Invoices.Add(TestOrders.MakeInvoice(orderA, number: 700));
        await db.SaveChangesAsync();
        db.Invoices.Add(TestOrders.MakeInvoice(orderB, number: 700, invoiceNumber: "FT-2026-00700-dup"));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        InvokeIsInvoiceNumberViolation(ex).Should().BeTrue(
            "a repeat of series+year+number is a number collision, so a fresh number is the fix");
        InvokeIsInvoiceOrderIdViolation(ex).Should().BeFalse();
    }

    // The race is forced by ordering the calls directly rather than by real concurrency.
    [Fact]
    public async Task ConcurrentDeliveriesForSameOrder_LoserGetsClassifiableViolation_ExactlyOneInvoicePersists()
    {
        var orderId = Guid.NewGuid();
        using (var seed = CreateDb())
        {
            seed.Orders.Add(MakeOrder(orderId));
            await seed.SaveChangesAsync();
        }

        using var dbA = CreateDb();
        using var dbB = CreateDb();
        var numbering = new FixedInvoiceNumbering();
        var creatorA = MakeInvoiceCreator(dbA, numbering);
        var creatorB = MakeInvoiceCreator(dbB, numbering);

        var invoiceA = await creatorA.CreateForOrderAsync(orderId);
        var invoiceB = await creatorB.CreateForOrderAsync(orderId);
        invoiceA.Should().NotBeNull();
        invoiceB.Should().NotBeNull();

        await dbA.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => dbB.SaveChangesAsync());

        InvokeIsInvoiceOrderIdViolation(ex).Should().BeTrue();

        using var verify = CreateDb();
        (await verify.Invoices.Where(i => i.OrderId == orderId).CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveOrderPaidWithInvoiceAsync_InvoiceAlreadyExistsForOrder_ReturnsFalseWithoutThrowing()
    {
        var orderId = Guid.NewGuid();
        using (var seed = CreateDb())
        {
            seed.Orders.Add(MakeOrder(orderId));
            await seed.SaveChangesAsync();
        }
        using (var winner = CreateDb())
        {
            var winnerCreator = MakeInvoiceCreator(winner, new FixedInvoiceNumbering());
            await winnerCreator.CreateForOrderAsync(orderId);
            await winner.SaveChangesAsync();
        }

        using var dbLoser = CreateDb();
        var order = await dbLoser.Orders.FirstAsync(o => o.Id == orderId);
        var controller = MakeController(dbLoser, MakeInvoiceCreator(dbLoser, new FixedInvoiceNumbering()));

        var outcome = await InvokeSaveAsync(controller, order, OrderStatus.AwaitingPayment);

        outcome.Should().Be("AlreadyInvoiced");
        using var verify = CreateDb();
        (await verify.Invoices.Where(i => i.OrderId == orderId).CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveOrderPaidWithInvoiceAsync_InvoiceNumberCollision_RetriesWithFreshNumber()
    {
        var winnerOrderId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        using (var seed = CreateDb())
        {
            seed.Orders.Add(MakeOrder(winnerOrderId));
            seed.Orders.Add(MakeOrder(orderId));
            await seed.SaveChangesAsync();
        }
        using (var winner = CreateDb())
        {
            var winnerCreator = MakeInvoiceCreator(winner, new SequenceInvoiceNumbering(1));
            await winnerCreator.CreateForOrderAsync(winnerOrderId);
            await winner.SaveChangesAsync();
        }

        using var db = CreateDb();
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        var numbering = new SequenceInvoiceNumbering(1, 2);
        var controller = MakeController(db, MakeInvoiceCreator(db, numbering));

        var outcome = await InvokeSaveAsync(controller, order, OrderStatus.AwaitingPayment);

        outcome.Should().Be("Created");
        numbering.CallCount.Should().Be(2);
        using var verify = CreateDb();
        var mine = await verify.Invoices.FirstAsync(i => i.OrderId == orderId);
        mine.Number.Should().Be(2);
    }

    [Fact]
    public async Task SaveOrderPaidWithInvoiceAsync_InvoiceNumberCollisionExhaustsRetries_LogsManualReconciliationAndReturnsFalse()
    {
        var winnerOrderId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        using (var seed = CreateDb())
        {
            seed.Orders.Add(MakeOrder(winnerOrderId));
            seed.Orders.Add(MakeOrder(orderId));
            await seed.SaveChangesAsync();
        }
        using (var winner = CreateDb())
        {
            var winnerCreator = MakeInvoiceCreator(winner, new AlwaysSameInvoiceNumbering());
            await winnerCreator.CreateForOrderAsync(winnerOrderId);
            await winner.SaveChangesAsync();
        }

        using var db = CreateDb();
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        order.EuPlatescTransactionId = "EP-RECONCILE-1";
        var logs = new LogCapture();
        var hub = new Mock<Sentry.IHub>();
        hub.SetupGet(h => h.IsEnabled).Returns(true);
        var controller = MakeController(db, MakeInvoiceCreator(db, new AlwaysSameInvoiceNumbering()), logs, hub.Object);

        var outcome = await InvokeSaveAsync(controller, order, OrderStatus.AwaitingPayment);

        // The rollback and the metric label are asserted by the endpoint-driven test below; from here the transition was never applied, so they would pass vacuously.
        outcome.Should().Be("NumberExhausted");
        hub.Verify(h => h.CaptureEvent(
            It.Is<Sentry.SentryEvent>(e => e.Exception is DbUpdateException),
            It.IsAny<Sentry.Scope>(), It.IsAny<Sentry.SentryHint>()), Times.Once);
        // The charge's own identifiers must survive: the reload below wipes them from the entity.
        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Error &&
                 r.Message.StartsWith("invoice.creation.number-collision-exhausted", StringComparison.Ordinal) &&
                 r.Message.Contains(order.Id.ToString()) &&
                 r.Message.Contains(order.OrderNumber) &&
                 r.Message.Contains("EP-RECONCILE-1"));
        using var verify = CreateDb();
        (await verify.Invoices.Where(i => i.OrderId == orderId).CountAsync()).Should().Be(0);
    }

    // Drives the real endpoint, not the helper: the label the handler records and the rollback of the applied transition are only observable from out here.
    [Fact]
    public async Task StripeWebhook_WhenInvoiceNumberRetriesExhaust_RecordsFailedNotDuplicateAndLeavesOrderUnpaid()
    {
        var winnerOrderId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        using (var seed = CreateDb())
        {
            seed.Orders.Add(MakeOrder(winnerOrderId));
            seed.Orders.Add(MakeOrder(orderId));
            await seed.SaveChangesAsync();
        }
        using (var winner = CreateDb())
        {
            await MakeInvoiceCreator(winner, new AlwaysSameInvoiceNumbering()).CreateForOrderAsync(winnerOrderId);
            await winner.SaveChangesAsync();
        }

        using var db = CreateDb();
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        var updatedAtBefore = order.UpdatedAt;

        var orderService = new Mock<IOrderService>();
        orderService.Setup(s => s.GetByPaymentIntentIdAsync("pi_exhaust", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(order);
        var verifier = new Mock<IStripeSignatureVerifier>();
        verifier.Setup(v => v.ConstructEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Stripe.Event { Type = "payment_intent.succeeded" });

        var controller = MakeController(
            db, MakeInvoiceCreator(db, new AlwaysSameInvoiceNumbering()),
            orderService: orderService.Object, stripeVerifier: verifier.Object);
        controller.ControllerContext.HttpContext.Request.Body =
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"data\":{\"object\":{\"id\":\"pi_exhaust\"}}}"));

        using var metrics = new MetricCapture(MetricNames.Instruments.PaymentWebhookTotal);

        await controller.StripeWebhookAsync(default);

        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe),
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Failed))
            .Should().HaveCount(1, "a charge whose invoice number never allocated must burn SLO budget, not read as a duplicate");
        metrics.For(MetricNames.Instruments.PaymentWebhookTotal,
                (MetricNames.Labels.Result, MetricNames.WebhookResultValues.Duplicate))
            .Should().BeEmpty();
        metrics.ContractViolations().Should().BeEmpty();

        order.Status.Should().Be(OrderStatus.AwaitingPayment, "the applied Paid transition must be rolled back");
        order.PaidAt.Should().BeNull();
        order.UpdatedAt.Should().Be(updatedAtBefore, "the rollback must undo every field Transition touched");
        using var verify = CreateDb();
        (await verify.Invoices.Where(i => i.OrderId == orderId).CountAsync()).Should().Be(0);
    }

    // A throw escaping the rollback would skip RecordPaymentWebhook, dropping a real charge out of the SLO — the hole a rethrow was refused to avoid.
    [Fact]
    public async Task SaveOrderPaidWithInvoiceAsync_WhenRollbackReloadIsCancelled_StillReturnsWithoutThrowing()
    {
        var winnerOrderId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        using (var seed = CreateDb())
        {
            seed.Orders.Add(MakeOrder(winnerOrderId));
            seed.Orders.Add(MakeOrder(orderId));
            await seed.SaveChangesAsync();
        }
        using (var winner = CreateDb())
        {
            await MakeInvoiceCreator(winner, new AlwaysSameInvoiceNumbering()).CreateForOrderAsync(winnerOrderId);
            await winner.SaveChangesAsync();
        }

        using var db = CreateDb();
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        var logs = new LogCapture();
        using var cts = new CancellationTokenSource();

        // CaptureEvent fires immediately before the reload, so cancelling from it lands the token inside ReloadAsync.
        var hub = new Mock<Sentry.IHub>();
        hub.SetupGet(h => h.IsEnabled).Returns(true);
        hub.Setup(h => h.CaptureEvent(It.IsAny<Sentry.SentryEvent>(), It.IsAny<Sentry.Scope>(), It.IsAny<Sentry.SentryHint>()))
           .Callback(() => cts.Cancel())
           .Returns(Sentry.SentryId.Empty);

        var controller = MakeController(
            db, MakeInvoiceCreator(db, new AlwaysSameInvoiceNumbering()), logs, hub.Object);

        var outcome = await InvokeSaveAsync(controller, order, OrderStatus.AwaitingPayment, cts.Token);

        outcome.Should().Be("NumberExhausted", "the caller must still get an outcome so it can record the metric");
        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("invoice.creation.rollback-reload-failed", StringComparison.Ordinal));
    }

    // "duplicate" sits in SLO 3's success numerator, so mislabelling the exhausted path inflates the SLO instead of burning budget.
    [Theory]
    [InlineData("Created", "ok")]
    [InlineData("AlreadyInvoiced", "duplicate")]
    [InlineData("NumberExhausted", "failed")]
    public void ResultLabelFor_maps_each_outcome_to_its_slo_label(string outcomeName, string expectedLabel)
    {
        var enumType = typeof(WebhooksController).GetNestedType("PaidSaveOutcome", BindingFlags.NonPublic)!;
        var method = typeof(WebhooksController).GetMethod("ResultLabelFor",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        method.Invoke(null, [Enum.Parse(enumType, outcomeName)]).Should().Be(expectedLabel);
    }

    private sealed class AlwaysSameInvoiceNumbering : IInvoiceNumberingService
    {
        public Task<InvoiceNumber> NextNumberAsync(string series, int year, CancellationToken ct = default)
            => Task.FromResult(new InvoiceNumber(series, year, 1));
    }

    private sealed class FixedInvoiceNumbering : IInvoiceNumberingService
    {
        private int _next = 1;
        public Task<InvoiceNumber> NextNumberAsync(string series, int year, CancellationToken ct = default)
            => Task.FromResult(new InvoiceNumber(series, year, Interlocked.Increment(ref _next)));
    }

    private sealed class SequenceInvoiceNumbering : IInvoiceNumberingService
    {
        private readonly int[] _numbers;
        private int _call;
        public SequenceInvoiceNumbering(params int[] numbers) => _numbers = numbers;
        public int CallCount => _call;

        public Task<InvoiceNumber> NextNumberAsync(string series, int year, CancellationToken ct = default)
        {
            var n = _numbers[Math.Min(_call, _numbers.Length - 1)];
            _call++;
            return Task.FromResult(new InvoiceNumber(series, year, n));
        }
    }
}
