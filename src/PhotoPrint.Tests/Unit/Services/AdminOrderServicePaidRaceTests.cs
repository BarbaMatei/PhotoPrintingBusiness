using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
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

    private AdminOrderService BuildService(PhotoPrintDbContext db, IInvoiceCreationService creator)
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
            NullLogger<AdminOrderService>.Instance);

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
}
