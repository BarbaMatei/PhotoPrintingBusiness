using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

/// <summary>
/// Contract tests for <see cref="SqliteInvoiceNumberingService"/> — the
/// dev-path numbering implementation. We use an in-memory SQLite connection
/// (NOT EF InMemory) because the production code uses LINQ extension
/// methods that EF InMemory mishandles, and because the contract is about
/// monotone allocation under SQLite's serial-write model.
///
/// The Postgres implementation is contract-equivalent (ADR-020); covering
/// the SQLite path here is sufficient for the bolt's tests — the Postgres
/// path's `nextval()` atomicity is a Postgres guarantee, not our code.
/// </summary>
public class SqliteInvoiceNumberingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PhotoPrintDbContext _db;
    private readonly SqliteInvoiceNumberingService _sut;

    public SqliteInvoiceNumberingServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new PhotoPrintDbContext(opts);
        _db.Database.EnsureCreated();
        _sut = new SqliteInvoiceNumberingService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task SeedInvoiceAsync(string series, int year, int number)
    {
        // Need a real Order row (FK), so seed one with the minimum required fields.
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"FT-{Random.Shared.Next(100_000, 999_999)}",
            Status = OrderStatus.Paid,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "x", Phone = "x",
                Street = "x", Number = "1",
                City = "x", County = "x", PostalCode = "x",
            },
        };
        _db.Orders.Add(order);

        _db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            InvoiceNumber = $"{series}-{year:D4}-{number:D5}",
            Series = series,
            Number = number,
            IssuedAt = new DateTimeOffset(year, 6, 1, 0, 0, 0, TimeSpan.Zero),
            NetTotalRon = 100m, VatRon = 19m, TotalRon = 119m,
            AnafStatus = InvoiceAnafStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task First_number_in_empty_year_starts_at_one()
    {
        var result = await _sut.NextNumberAsync("FT", 2026);
        result.Series.Should().Be("FT");
        result.Year.Should().Be(2026);
        result.Number.Should().Be(1);
        result.ToString().Should().Be("FT-2026-00001");
    }

    [Fact]
    public async Task Next_number_increments_past_existing_max()
    {
        await SeedInvoiceAsync("FT", 2026, 1);
        await SeedInvoiceAsync("FT", 2026, 2);
        await SeedInvoiceAsync("FT", 2026, 7);

        var result = await _sut.NextNumberAsync("FT", 2026);
        result.Number.Should().Be(8);
    }

    [Fact]
    public async Task Series_partition_is_independent()
    {
        await SeedInvoiceAsync("FT", 2026, 5);
        var fp = await _sut.NextNumberAsync("FP", 2026);
        fp.Number.Should().Be(1);
    }

    [Fact]
    public async Task Year_partition_is_independent()
    {
        await SeedInvoiceAsync("FT", 2026, 42);
        var next2027 = await _sut.NextNumberAsync("FT", 2027);
        next2027.Number.Should().Be(1);
        next2027.ToString().Should().Be("FT-2027-00001");
    }

    [Fact]
    public async Task Sequential_calls_produce_monotone_numbers()
    {
        // The numbering service alone doesn't INSERT the invoice — callers
        // do. So sequential calls return the same number until somebody
        // persists. We simulate the realistic "allocate-then-insert" cycle.
        for (var expected = 1; expected <= 5; expected++)
        {
            var n = await _sut.NextNumberAsync("FT", 2026);
            n.Number.Should().Be(expected);
            await SeedInvoiceAsync("FT", 2026, n.Number);
        }
    }

    [Fact]
    public async Task InvoiceNumber_ToString_pads_to_five_digits()
    {
        // Seed up through 99 then allocate next → should format as 00100.
        for (var i = 1; i <= 99; i++) await SeedInvoiceAsync("FT", 2026, i);
        var n = await _sut.NextNumberAsync("FT", 2026);
        n.ToString().Should().Be("FT-2026-00100");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Empty_series_throws(string series)
    {
        var act = () => _sut.NextNumberAsync(series, 2026);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(10000)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Year_out_of_range_throws(int year)
    {
        var act = () => _sut.NextNumberAsync("FT", year);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
