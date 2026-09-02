using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

/// <summary>
/// Verifies the invoicing hand-off: <see cref="InvoiceCreationService"/> consumes the
/// numbering service + VAT snapshot, producing an Invoice row at the Paid transition.
/// </summary>
public class InvoiceCreationServiceTests : IClassFixture<PostgresTestDatabase>, IDisposable
{
    private readonly PostgresTestDatabase _database;
    private readonly PhotoPrintDbContext _db;
    private readonly PostgresInvoiceNumberingService _numbering;
    private readonly InvoiceCreationService _sut;
    private readonly DateTimeOffset _now = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    public InvoiceCreationServiceTests(PostgresTestDatabase database)
    {
        _database = database;
        database.ResetForTest();

        _db = _database.NewContext();
        _numbering = NewNumbering(_db);
        _sut = new InvoiceCreationService(
            _db,
            _numbering,
            Options.Create(new VatSettings { InvoiceSeries = "FT", Rate = 0.19m }),
            new FakeClock(_now),
            new LoggerFactory().CreateLogger<InvoiceCreationService>());
    }

    private static PostgresInvoiceNumberingService NewNumbering(PhotoPrintDbContext db) =>
        new(db, NullLogger<PostgresInvoiceNumberingService>.Instance);

    private PhotoPrintDbContext CreateSqlLoggingDb(List<string> sqlLog) =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .LogTo(sqlLog.Add, LogLevel.Information)
            .Options);

    private InvoiceCreationService MakeSut(PhotoPrintDbContext db) =>
        new(db,
            NewNumbering(db),
            Options.Create(new VatSettings { InvoiceSeries = "FT", Rate = 0.19m }),
            new FakeClock(_now),
            new LoggerFactory().CreateLogger<InvoiceCreationService>());

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<Order> SeedPaidOrderAsync(decimal total = 26.00m)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
            Status = OrderStatus.Paid,
            PaidAt = _now,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x",
                Street = "Str. Test", Number = "1",
                City = "x", County = "x", PostalCode = "010101",
            },
            ShippingCostRon = 20m, SubtotalRon = total - 20m, TotalRon = total,
            NetTotalRon = decimal.Round(total / 1.19m, 2, MidpointRounding.AwayFromZero),
            VatRon      = decimal.Round(total * 0.19m / 1.19m, 2, MidpointRounding.AwayFromZero),
            VatRate     = 0.19m,
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task Creates_invoice_for_paid_order_with_snapshot_totals()
    {
        var order = await SeedPaidOrderAsync(total: 26.00m);

        var invoice = await _sut.CreateForOrderAsync(order.Id);
        await _db.SaveChangesAsync();   // simulate the webhook handler's commit

        invoice.Should().NotBeNull();
        invoice!.OrderId.Should().Be(order.Id);
        invoice.InvoiceNumber.Should().Be("FT-2026-00001");
        invoice.Series.Should().Be("FT");
        invoice.Number.Should().Be(1);
        invoice.NetTotalRon.Should().Be(order.NetTotalRon);
        invoice.VatRon.Should().Be(order.VatRon);
        invoice.TotalRon.Should().Be(order.TotalRon);
        invoice.AnafStatus.Should().Be(InvoiceAnafStatus.Pending);
        invoice.IssuedAt.Should().Be(order.PaidAt!.Value);   // legal date = Paid moment
    }

    [Fact]
    public async Task Replay_on_existing_invoice_returns_same_row_and_does_not_allocate_new_number()
    {
        var order = await SeedPaidOrderAsync();

        var first = await _sut.CreateForOrderAsync(order.Id);
        await _db.SaveChangesAsync();
        var second = await _sut.CreateForOrderAsync(order.Id);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.Id.Should().Be(first!.Id);
        second.InvoiceNumber.Should().Be(first.InvoiceNumber);

        // The DB has exactly one Invoice for this order — replay didn't duplicate.
        var rows = await _db.Invoices.CountAsync(i => i.OrderId == order.Id);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task Returns_null_when_order_does_not_exist()
    {
        var invoice = await _sut.CreateForOrderAsync(Guid.NewGuid());
        invoice.Should().BeNull();
    }

    [Fact]
    public async Task Order_overload_creates_invoice_without_reloading_the_order()
    {
        var seeded = await SeedPaidOrderAsync(total: 26.00m);

        var sql = new List<string>();
        using var db = CreateSqlLoggingDb(sql);
        var sut = MakeSut(db);
        var order = await db.Orders.FirstAsync(o => o.Id == seeded.Id);
        sql.Clear();   // the caller's own load is not what this asserts about

        var invoice = await sut.CreateForOrderAsync(order);
        await db.SaveChangesAsync();

        invoice.Should().NotBeNull();
        invoice!.OrderId.Should().Be(seeded.Id);
        invoice.InvoiceNumber.Should().Be("FT-2026-00001");
        sql.Should().NotContain(l => l.Contains("FROM \"Orders\""));
    }

    [Fact]
    public async Task Order_overload_replay_returns_same_row_and_does_not_allocate_new_number()
    {
        var order = await SeedPaidOrderAsync();

        var first = await _sut.CreateForOrderAsync(order);
        await _db.SaveChangesAsync();
        var second = await _sut.CreateForOrderAsync(order);

        second!.Id.Should().Be(first!.Id);
        (await _db.Invoices.CountAsync(i => i.OrderId == order.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Sequential_orders_get_monotone_numbers_within_same_year()
    {
        var a = await SeedPaidOrderAsync();
        var b = await SeedPaidOrderAsync();
        var c = await SeedPaidOrderAsync();

        await _sut.CreateForOrderAsync(a.Id);
        await _db.SaveChangesAsync();
        await _sut.CreateForOrderAsync(b.Id);
        await _db.SaveChangesAsync();
        await _sut.CreateForOrderAsync(c.Id);
        await _db.SaveChangesAsync();

        var numbers = await _db.Invoices
            .OrderBy(i => i.Number)
            .Select(i => i.InvoiceNumber)
            .ToListAsync();

        numbers.Should().Equal("FT-2026-00001", "FT-2026-00002", "FT-2026-00003");
    }

    private sealed class FakeClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeClock(DateTimeOffset now) { _now = now; }
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
