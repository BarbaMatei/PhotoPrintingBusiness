using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// OrderNumberService must work on SQLite — the Development-env
/// provider (appsettings.Development.json → DatabaseProvider=Sqlite). Before the fix,
/// GenerateAsync's else-branch ran Postgres-only SQL (`DO $$ … nextval`) that throws on
/// SQLite, so local order creation 500'd. These run against a REAL SQLite database (not
/// the EF InMemory provider, whose dedicated count branch masks the gap).
/// </summary>
public class OrderNumberServiceSqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PhotoPrintDbContext _db;
    private readonly OrderNumberService _service;

    public OrderNumberServiceSqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new PhotoPrintDbContext(opts);
        _db.Database.EnsureCreated();
        _service = new OrderNumberService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GenerateAsync_OnSqlite_ReturnsFormattedNumber_DoesNotThrow()
    {
        var number = await _service.GenerateAsync();

        Assert.StartsWith($"FT-{DateTime.UtcNow.Year}", number);
    }

    [Fact]
    public async Task GenerateAsync_OnSqlite_ProducesDistinctNumbersAcrossOrders()
    {
        var first = await _service.GenerateAsync();

        // Persist an order carrying the first number so a count-based scheme advances.
        _db.Orders.Add(new Order
        {
            OrderNumber = first,
            ShippingAddress = new ShippingAddressSnapshot(),
        });
        await _db.SaveChangesAsync();

        var second = await _service.GenerateAsync();

        Assert.NotEqual(first, second);
    }
}
