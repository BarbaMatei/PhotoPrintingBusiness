using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.API.Services.Sameday;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

// Real Postgres and a real creation service: EF InMemory raises no unique violation, and the classifier only matches the PostgreSQL error.
public class AdminOrderServicePaidRaceTests : IDisposable
{
    private static readonly DateTimeOffset WebhookPaidAt = new(2026, 8, 1, 9, 30, 0, TimeSpan.Zero);

    private readonly PostgresTestDatabase _database = new();
    private readonly Mock<IOrderEmailService> _email = new();
    private readonly Mock<IAwbCreationNotifier> _awb = new();
    private readonly Mock<IHubContext<AdminOrderHub>> _hub = new();
    private readonly Mock<IHubClients> _hubClients = new();
    private readonly Mock<IClientProxy> _clientProxy = new();

    public AdminOrderServicePaidRaceTests()
    {
        _hub.Setup(h => h.Clients).Returns(_hubClients.Object);
        _hubClients.Setup(c => c.All).Returns(_clientProxy.Object);
        _clientProxy
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _awb.Setup(n => n.NotifyPaidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public void Dispose() => _database.Dispose();

    private AdminOrderService BuildService(
        PhotoPrintDbContext db, IInvoiceCreationService creator,
        ILogger<AdminOrderService>? logger = null, Sentry.IHub? sentry = null)
        => new(
            db,
            _email.Object,
            Mock.Of<IEuPlatescService>(),
            Mock.Of<Stripe.IStripeClient>(),
            Mock.Of<IStorageRouter>(),
            Mock.Of<IOriginalPurger>(),
            Options.Create(new ArchiveSettings()),
            _hub.Object,
            _awb.Object,
            creator,
            logger ?? NullLogger<AdminOrderService>.Instance,
            sentry);

    private static InvoiceCreationService RealCreator(
        PhotoPrintDbContext db, IInvoiceNumberingService numbering)
        => new(
            db, numbering,
            Options.Create(new VatSettings { InvoiceSeries = "FT", Rate = 0.19m }),
            TimeProvider.System,
            NullLogger<InvoiceCreationService>.Instance);

    private async Task<Guid> SeedAwaitingPaymentAsync(string? euTransactionId = null)
    {
        var id = Guid.NewGuid();
        using var seed = _database.NewContext();
        var order = TestOrders.Make(id);
        order.Status = OrderStatus.AwaitingPayment;
        order.EuPlatescTransactionId = euTransactionId;
        seed.Orders.Add(order);
        await seed.SaveChangesAsync();
        return id;
    }

    private async Task SeedInvoicedOrderAsync(int number)
    {
        var id = Guid.NewGuid();
        using var seed = _database.NewContext();
        seed.Orders.Add(TestOrders.Make(id));
        await seed.SaveChangesAsync();
        seed.Invoices.Add(TestOrders.MakeInvoice(id, number: number));
        await seed.SaveChangesAsync();
    }

    private void CommitWebhookWinner(Guid orderId, int invoiceNumber)
    {
        using var winner = _database.NewContext();
        var order = winner.Orders.First(o => o.Id == orderId);
        order.Status = OrderStatus.Paid;
        order.PaidAt = WebhookPaidAt;
        order.UpdatedAt = WebhookPaidAt;
        winner.Invoices.Add(new Invoice
        {
            OrderId = orderId,
            Series = "FT",
            Number = invoiceNumber,
            InvoiceNumber = $"FT-2026-{invoiceNumber:D5}",
            IssuedAt = WebhookPaidAt,
            NetTotalRon = 84.03m,
            VatRon = 15.97m,
            TotalRon = 100m,
            AnafStatus = InvoiceAnafStatus.Pending,
        });
        winner.SaveChanges();
    }

    private void AssertNoPaidSideEffects()
    {
        _email.Verify(e => e.FireOrderConfirmedEmail(It.IsAny<Order>()), Times.Never);
        _awb.Verify(n => n.NotifyPaidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ManualPaid_LosingTheRaceBeforeTheInsert_KeepsTheWebhookPaidAtAndFiresNoSideEffects()
    {
        var orderId = await SeedAwaitingPaymentAsync();

        using var db = _database.NewContext();
        var creator = new RaceInjectingCreator(
            RealCreator(db, new FixedNumbering(901)),
            () => CommitWebhookWinner(orderId, 900),
            beforeTheExistenceQuery: true);
        var sut = BuildService(db, creator);

        var dto = await sut.UpdateStatusAsync(orderId, "Paid", null, null);

        using var verify = _database.NewContext();
        var order = await verify.Orders.FirstAsync(o => o.Id == orderId);
        order.PaidAt.Should().Be(WebhookPaidAt, "the winner's paid timestamp is what the invoice was issued against");
        var invoices = await verify.Invoices.Where(i => i.OrderId == orderId).ToListAsync();
        invoices.Should().ContainSingle();
        invoices[0].IssuedAt.Should().Be(order.PaidAt!.Value);
        AssertNoPaidSideEffects();
        dto.Status.Should().Be(nameof(OrderStatus.Paid), "the admin still gets the order's real state back");
        dto.PaidAt.Should().Be(WebhookPaidAt);
        _clientProxy.Verify(
            c => c.SendCoreAsync("OrderStatusChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once, "the board still has to learn the order moved");
    }

    [Fact]
    public async Task ManualPaid_LosingTheRaceOnTheUniqueIndex_KeepsTheWebhookPaidAtAndFiresNoSideEffects()
    {
        var orderId = await SeedAwaitingPaymentAsync();

        using var db = _database.NewContext();
        var creator = new RaceInjectingCreator(
            RealCreator(db, new FixedNumbering(901)),
            () => CommitWebhookWinner(orderId, 900),
            beforeTheExistenceQuery: false);
        var sut = BuildService(db, creator);

        await sut.UpdateStatusAsync(orderId, "Paid", null, null);

        using var verify = _database.NewContext();
        var order = await verify.Orders.FirstAsync(o => o.Id == orderId);
        order.PaidAt.Should().Be(WebhookPaidAt);
        var invoices = await verify.Invoices.Where(i => i.OrderId == orderId).ToListAsync();
        invoices.Should().ContainSingle();
        invoices[0].Number.Should().Be(900, "the loser's invoice must not replace the committed one");
        invoices[0].IssuedAt.Should().Be(order.PaidAt!.Value);
        AssertNoPaidSideEffects();
    }

    [Fact]
    public async Task ManualPaid_WhenInvoiceNumberRetriesExhaust_LeavesTheOrderUnpaidAndAnswersConflict()
    {
        await SeedInvoicedOrderAsync(number: 700);
        var orderId = await SeedAwaitingPaymentAsync(euTransactionId: "EP-ADMIN-RECONCILE");

        using var db = _database.NewContext();
        var numbering = new FixedNumbering(700);
        var logs = new LogCapture();
        var sut = BuildService(db, RealCreator(db, numbering), logs.LoggerFor<AdminOrderService>());

        var act = () => sut.UpdateStatusAsync(orderId, "Paid", null, null);

        await act.Should().ThrowAsync<ConflictException>(
            "a 500 tells the admin nothing and leaves the order looking Paid in the response");
        numbering.CallCount.Should().Be(4, "three retries after the first attempt");

        using var verify = _database.NewContext();
        var order = await verify.Orders.FirstAsync(o => o.Id == orderId);
        order.Status.Should().Be(OrderStatus.AwaitingPayment);
        order.PaidAt.Should().BeNull();
        (await verify.Invoices.CountAsync(i => i.OrderId == orderId)).Should().Be(0);

        logs.Records.Should().ContainSingle(
            r => r.Level == LogLevel.Error &&
                 r.Message.StartsWith("admin.order.invoice-number-collision-exhausted", StringComparison.Ordinal) &&
                 r.Message.Contains(order.OrderNumber) &&
                 r.Message.Contains("100") &&
                 r.Message.Contains("EP-ADMIN-RECONCILE"),
            "the order number, the total and the payment handles are what a manual reconciliation needs");
        AssertNoPaidSideEffects();
        _clientProxy.Verify(
            c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never, "nothing moved, so nothing is broadcast");
    }

    [Fact]
    public async Task ManualPaid_WhenInvoiceNumberRetriesExhaust_CapturesTheExceptionForTriage()
    {
        await SeedInvoicedOrderAsync(number: 702);
        var orderId = await SeedAwaitingPaymentAsync();

        using var db = _database.NewContext();
        var hub = new Mock<Sentry.IHub>();
        hub.SetupGet(h => h.IsEnabled).Returns(true);
        var sut = BuildService(db, RealCreator(db, new FixedNumbering(702)), sentry: hub.Object);

        var act = () => sut.UpdateStatusAsync(orderId, "Paid", null, null);

        await act.Should().ThrowAsync<ConflictException>();
        hub.Verify(h => h.CaptureEvent(
                It.Is<Sentry.SentryEvent>(e => e.Exception is DbUpdateException),
                It.IsAny<Sentry.Scope>(), It.IsAny<Sentry.SentryHint>()),
            Times.Once);
    }

    // The rollback is a mechanism of its own: a throw from its reload must not turn the conflict into an unexplained 500.
    [Fact]
    public async Task ManualPaid_WhenTheRollbackReloadFails_StillAnswersConflict()
    {
        await SeedInvoicedOrderAsync(number: 701);
        var orderId = await SeedAwaitingPaymentAsync();

        using var db = _database.NewContext();
        using var cts = new CancellationTokenSource();
        var logs = new LogCapture();
        var logger = new CancellingLogger(
            logs, "admin.order.invoice-number-collision-exhausted", cts);
        var sut = BuildService(db, RealCreator(db, new FixedNumbering(701)), logger);

        var act = () => sut.UpdateStatusAsync(orderId, "Paid", null, null, cts.Token);

        await act.Should().ThrowAsync<ConflictException>();
        logs.Records.Should().ContainSingle(
            r => r.Message.StartsWith("admin.order.rollback-reload-failed", StringComparison.Ordinal));

        using var verify = _database.NewContext();
        (await verify.Orders.FirstAsync(o => o.Id == orderId)).PaidAt.Should().BeNull();
    }

    [Fact]
    public async Task ManualPaid_RetriesATakenInvoiceNumberInsteadOfThrowing()
    {
        await SeedInvoicedOrderAsync(number: 500);
        var orderId = await SeedAwaitingPaymentAsync();

        using var db = _database.NewContext();
        var sut = BuildService(db, RealCreator(db, new CollidingThenFreeNumbering(collideWith: 500, thenUse: 501)));

        await sut.UpdateStatusAsync(orderId, "Paid", null, null);

        using var verify = _database.NewContext();
        var invoice = await verify.Invoices.FirstAsync(i => i.OrderId == orderId);
        invoice.Number.Should().Be(501, "the taken number must be retried, not thrown on");
        (await verify.Orders.FirstAsync(o => o.Id == orderId)).PaidAt.Should().NotBeNull();
        _email.Verify(e => e.FireOrderConfirmedEmail(It.IsAny<Order>()), Times.Once);
    }

    // Sentry registers its hub only when Sentry:Enabled, so the container has to build this service without one.
    [Fact]
    public void TheServiceStillResolvesWithNoSentryHubRegistered()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PhotoPrintDbContext>(o => o.UseNpgsql(_database.ConnectionString));
        services.AddSingleton(_email.Object);
        services.AddSingleton(Mock.Of<IEuPlatescService>());
        services.AddSingleton(Mock.Of<Stripe.IStripeClient>());
        services.AddSingleton(Mock.Of<IStorageRouter>());
        services.AddSingleton(Mock.Of<IOriginalPurger>());
        services.AddSingleton(Options.Create(new ArchiveSettings()));
        services.AddSingleton(_hub.Object);
        services.AddSingleton(_awb.Object);
        services.AddSingleton(Mock.Of<IInvoiceCreationService>());
        services.AddSingleton<ILogger<AdminOrderService>>(NullLogger<AdminOrderService>.Instance);
        services.AddScoped<IAdminOrderService, AdminOrderService>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAdminOrderService>().Should().NotBeNull();
    }

    // The flag picks which side of the creation service's existence query the winner commits on, so both race windows are reachable.
    private sealed class RaceInjectingCreator : IInvoiceCreationService
    {
        private readonly IInvoiceCreationService _inner;
        private readonly Action _commitWinner;
        private readonly bool _beforeTheExistenceQuery;
        private bool _fired;

        public RaceInjectingCreator(
            IInvoiceCreationService inner, Action commitWinner, bool beforeTheExistenceQuery)
        {
            _inner = inner;
            _commitWinner = commitWinner;
            _beforeTheExistenceQuery = beforeTheExistenceQuery;
        }

        public Task<Invoice?> CreateForOrderAsync(Guid orderId, CancellationToken ct = default)
            => RunAsync(() => _inner.CreateForOrderAsync(orderId, ct));

        public Task<Invoice?> CreateForOrderAsync(Order order, CancellationToken ct = default)
            => RunAsync(() => _inner.CreateForOrderAsync(order, ct));

        private async Task<Invoice?> RunAsync(Func<Task<Invoice?>> inner)
        {
            if (!_fired && _beforeTheExistenceQuery)
            {
                _commitWinner();
                _fired = true;
            }

            var invoice = await inner();

            if (!_fired)
            {
                _commitWinner();
                _fired = true;
            }

            return invoice;
        }
    }

    private sealed class FixedNumbering : IInvoiceNumberingService
    {
        private readonly int _number;

        public FixedNumbering(int number) => _number = number;

        public int CallCount { get; private set; }

        public Task<InvoiceNumber> NextNumberAsync(string series, int year, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(new InvoiceNumber(series, year, _number));
        }
    }

    private sealed class CollidingThenFreeNumbering : IInvoiceNumberingService
    {
        private readonly int _collideWith;
        private readonly int _thenUse;
        private int _calls;

        public CollidingThenFreeNumbering(int collideWith, int thenUse)
        {
            _collideWith = collideWith;
            _thenUse = thenUse;
        }

        public Task<InvoiceNumber> NextNumberAsync(string series, int year, CancellationToken ct = default)
            => Task.FromResult(new InvoiceNumber(series, year, _calls++ == 0 ? _collideWith : _thenUse));
    }

    // Cancels the token the moment a named line is logged, landing the cancellation inside the call that follows it.
    private sealed class CancellingLogger : ILogger<AdminOrderService>
    {
        private readonly LogCapture _capture;
        private readonly string _prefix;
        private readonly CancellationTokenSource _cts;

        public CancellingLogger(LogCapture capture, string prefix, CancellationTokenSource cts)
        {
            _capture = capture;
            _prefix = prefix;
            _cts = cts;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            _capture.LoggerFor<AdminOrderService>().Log(logLevel, eventId, message, exception, (m, _) => m);
            if (message.StartsWith(_prefix, StringComparison.Ordinal)) _cts.Cancel();
        }
    }
}
