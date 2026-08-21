using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// OrderNumberService against a real PostgreSQL database, exercising the per-year
/// <c>CREATE SEQUENCE</c> + <c>nextval</c> path rather than the InMemory count fallback.
/// </summary>
public class OrderNumberServicePostgresTests : IClassFixture<PostgresTestDatabase>, IDisposable
{
    private readonly PostgresTestDatabase _database;
    private readonly PhotoPrintDbContext _db;
    private readonly OrderNumberService _service;

    public OrderNumberServicePostgresTests(PostgresTestDatabase database)
    {
        _database = database;
        database.TruncateAllTables();

        _db = _database.NewContext();
        _service = new OrderNumberService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GenerateAsync_OnPostgres_ReturnsFormattedNumber_DoesNotThrow()
    {
        var number = await _service.GenerateAsync();

        Assert.StartsWith($"FT-{DateTime.UtcNow.Year}", number);
    }

    [Fact]
    public async Task GenerateAsync_OnPostgres_ProducesDistinctNumbersAcrossOrders()
    {
        var first = await _service.GenerateAsync();

        _db.Orders.Add(new Order
        {
            OrderNumber = first,
            ShippingAddress = new ShippingAddressSnapshot(),
        });
        await _db.SaveChangesAsync();

        var second = await _service.GenerateAsync();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task GenerateAsync_LosesTheSequenceCreateRace_StillReturnsANumber()
    {
        var year = DateTime.UtcNow.Year;
        await using var rival = await UncommittedSequenceCreator.StartAsync(
            _database.ConnectionString, $"order_number_seq_{year}");

        var db = _database.NewContext();
        string? number = null;
        Exception? failure;

        try
        {
            var generate = Task.Run(() => new OrderNumberService(db).GenerateAsync());

            await rival.WaitUntilAnotherBackendBlocksAsync(TimeSpan.FromSeconds(10));
            await rival.CommitAsync();

            failure = await Record.ExceptionAsync(async () => number = await generate);
        }
        finally
        {
            await Record.ExceptionAsync(() => db.DisposeAsync().AsTask());
        }

        Assert.True(failure is null, $"the losing caller threw instead of drawing a number: {failure}");
        Assert.StartsWith($"FT-{year}", number);
    }

    [Fact]
    public async Task GenerateAsync_ConcurrentCallers_EachGetADistinctNumber()
    {
        const int concurrency = 20;

        var numbers = await Task.WhenAll(Enumerable.Range(0, concurrency).Select(async _ =>
        {
            using var db = _database.NewContext();
            return await new OrderNumberService(db).GenerateAsync();
        }));

        Assert.Equal(concurrency, numbers.Distinct().Count());
    }
}
