using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoPrint.API.Data;
using PhotoPrint.API.Services.Invoicing;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public sealed class PostgresInvoiceNumberingServiceIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fx;

    public PostgresInvoiceNumberingServiceIntegrationTests(PostgresFixture fx) => _fx = fx;

    private static int RandomYear() => Random.Shared.Next(3000, 3999);

    [SkippableFact]
    public async Task NextNumberAsync_SequentialCalls_ReturnsIncrementingNumbersWithinSameYear()
    {
        Skip.IfNot(_fx.Available, PostgresFixture.SkipReason);
        var year = RandomYear();
        using var sut = _fx.MakeSut();

        var a = await sut.NextNumberAsync("FT", year);
        var b = await sut.NextNumberAsync("FT", year);
        var c = await sut.NextNumberAsync("FT", year);

        a.Number.Should().Be(1);
        b.Number.Should().Be(2);
        c.Number.Should().Be(3);
    }

    [SkippableFact]
    public async Task NextNumberAsync_YearRolls_NewYearStartsItsOwnSequenceAtOne()
    {
        Skip.IfNot(_fx.Available, PostgresFixture.SkipReason);
        var yearA = RandomYear();
        var yearB = yearA + 1;
        using var sut = _fx.MakeSut();

        await sut.NextNumberAsync("FT", yearA);
        var secondInYearA = await sut.NextNumberAsync("FT", yearA);
        var firstInYearB = await sut.NextNumberAsync("FT", yearB);

        secondInYearA.Number.Should().Be(2);
        firstInYearB.Number.Should().Be(1);
    }

    [SkippableFact]
    public async Task NextNumberAsync_ConcurrentCallers_EachGetsADistinctNumber()
    {
        Skip.IfNot(_fx.Available, PostgresFixture.SkipReason);
        var year = RandomYear();
        const int concurrency = 20;

        var results = await Task.WhenAll(Enumerable.Range(0, concurrency).Select(async _ =>
        {
            using var sut = _fx.MakeSut();
            return await sut.NextNumberAsync("FT", year);
        }));

        results.Select(r => r.Number).Distinct().Should().HaveCount(concurrency);
    }
}

// Each MakeSut() call gets its own DbContext — concurrent callers must not share one.
public sealed class PostgresFixture
{
    public const string SkipReason =
        "Postgres connection not configured (set ConnectionStrings__Default to run). " +
        "These tests run in CI via the Postgres service container.";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Default");

    public bool Available => !string.IsNullOrEmpty(_connectionString);

    public PostgresInvoiceNumberingServiceHandle MakeSut()
    {
        var db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseNpgsql(_connectionString)
                .Options);
        return new PostgresInvoiceNumberingServiceHandle(db);
    }
}

public sealed class PostgresInvoiceNumberingServiceHandle : IInvoiceNumberingService, IDisposable
{
    private readonly PhotoPrintDbContext _db;
    private readonly PostgresInvoiceNumberingService _inner;

    public PostgresInvoiceNumberingServiceHandle(PhotoPrintDbContext db)
    {
        _db = db;
        _inner = new PostgresInvoiceNumberingService(db, NullLogger<PostgresInvoiceNumberingService>.Instance);
    }

    public Task<InvoiceNumber> NextNumberAsync(string series, int year, CancellationToken ct = default) =>
        _inner.NextNumberAsync(series, year, ct);

    public void Dispose() => _db.Dispose();
}
