using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public sealed class PostgresInvoiceNumberingServiceIntegrationTests : IDisposable
{
    private readonly PostgresTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static int RandomYear() => Random.Shared.Next(3000, 3999);

    // Each MakeSut() call gets its own DbContext — concurrent callers must not share one.
    private PostgresInvoiceNumberingServiceHandle MakeSut() =>
        new(_database.NewContext());

    [Fact]
    public async Task NextNumberAsync_SequentialCalls_ReturnsIncrementingNumbersWithinSameYear()
    {
        var year = RandomYear();
        using var sut = MakeSut();

        var a = await sut.NextNumberAsync("FT", year);
        var b = await sut.NextNumberAsync("FT", year);
        var c = await sut.NextNumberAsync("FT", year);

        a.Number.Should().Be(1);
        b.Number.Should().Be(2);
        c.Number.Should().Be(3);
    }

    [Fact]
    public async Task NextNumberAsync_YearRolls_NewYearStartsItsOwnSequenceAtOne()
    {
        var yearA = RandomYear();
        var yearB = yearA + 1;
        using var sut = MakeSut();

        await sut.NextNumberAsync("FT", yearA);
        var secondInYearA = await sut.NextNumberAsync("FT", yearA);
        var firstInYearB = await sut.NextNumberAsync("FT", yearB);

        secondInYearA.Number.Should().Be(2);
        firstInYearB.Number.Should().Be(1);
    }

    [Fact]
    public async Task NextNumberAsync_ConcurrentCallers_EachGetsADistinctNumber()
    {
        var year = RandomYear();
        const int concurrency = 20;

        var results = await Task.WhenAll(Enumerable.Range(0, concurrency).Select(async _ =>
        {
            using var sut = MakeSut();
            return await sut.NextNumberAsync("FT", year);
        }));

        results.Select(r => r.Number).Distinct().Should().HaveCount(concurrency);
    }
}

public sealed class PostgresInvoiceNumberingServiceHandle : IInvoiceNumberingService, IDisposable
{
    private readonly PhotoPrint.API.Data.PhotoPrintDbContext _db;
    private readonly PostgresInvoiceNumberingService _inner;

    public PostgresInvoiceNumberingServiceHandle(PhotoPrint.API.Data.PhotoPrintDbContext db)
    {
        _db = db;
        _inner = new PostgresInvoiceNumberingService(db, NullLogger<PostgresInvoiceNumberingService>.Instance);
    }

    public Task<InvoiceNumber> NextNumberAsync(string series, int year, CancellationToken ct = default) =>
        _inner.NextNumberAsync(series, year, ct);

    public void Dispose() => _db.Dispose();
}
