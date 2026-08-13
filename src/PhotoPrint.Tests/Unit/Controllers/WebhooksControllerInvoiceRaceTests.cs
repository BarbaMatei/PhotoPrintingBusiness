using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Controllers;
using PhotoPrint.API.Data;
using PhotoPrint.API.Hubs;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.API.Services.Sameday;
using Xunit;

namespace PhotoPrint.Tests.Unit.Controllers;

public class WebhooksControllerInvoiceRaceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public WebhooksControllerInvoiceRaceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>().UseSqlite(_connection).Options);

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

    private static WebhooksController MakeController(PhotoPrintDbContext db, IInvoiceCreationService invoiceCreator)
        => new(
            Mock.Of<IOrderService>(),
            Mock.Of<IStripeSignatureVerifier>(),
            Mock.Of<IEuPlatescService>(),
            db,
            Mock.Of<IOrderEmailService>(),
            Mock.Of<IOrderPhotoPromoter>(),
            Mock.Of<IAwbCreationNotifier>(),
            invoiceCreator,
            Mock.Of<IHubContext<AdminOrderHub>>(),
            Options.Create(new StripeSettings()),
            Options.Create(new EuPlatescSettings()),
            NullLogger<WebhooksController>.Instance);

    private static Task<bool> InvokeSaveAsync(WebhooksController controller, Order order)
    {
        var method = typeof(WebhooksController).GetMethod("SaveOrderPaidWithInvoiceAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task<bool>)method.Invoke(controller, [order, CancellationToken.None])!;
    }

    private static InvoiceCreationService MakeInvoiceCreator(PhotoPrintDbContext db, IInvoiceNumberingService numbering) =>
        new(db, numbering, Options.Create(new VatSettings()), TimeProvider.System, NullLogger<InvoiceCreationService>.Instance);

    private static bool InvokeIsInvoiceOrderIdViolation(DbUpdateException ex)
    {
        var method = typeof(WebhooksController).GetMethod("IsInvoiceOrderIdViolation",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [ex])!;
    }

    // One shared SQLite connection can't run true concurrent transactions, so the race is forced by ordering the calls directly.
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

        var created = await InvokeSaveAsync(controller, order);

        created.Should().BeFalse();
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

        var created = await InvokeSaveAsync(controller, order);

        created.Should().BeTrue();
        numbering.CallCount.Should().Be(2);
        using var verify = CreateDb();
        var mine = await verify.Invoices.FirstAsync(i => i.OrderId == orderId);
        mine.Number.Should().Be(2);
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
