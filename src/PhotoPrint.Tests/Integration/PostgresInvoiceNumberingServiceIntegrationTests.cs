using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public sealed class PostgresInvoiceNumberingServiceIntegrationTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public PostgresInvoiceNumberingServiceIntegrationTests(PostgresTestDatabase database)
    {
        _database = database;
        database.ResetForTest();
    }


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
    public async Task ReconcileWithStoredInvoicesAsync_SequenceLagsTheStoredInvoices_AllocatesPastTheHighestStoredNumber()
    {
        var year = RandomYear();
        await SeedInvoicesAsync(year, series: "FT", numbers: [1, 2, 3, 4, 5]);
        using var sut = MakeSut();

        var beforeReconcile = await sut.NextNumberAsync("FT", year);
        await sut.ReconcileWithStoredInvoicesAsync("FT", year);
        var afterReconcile = await sut.NextNumberAsync("FT", year);

        beforeReconcile.Number.Should().Be(1, "a restore without the sequence leaves it at the start");
        afterReconcile.Number.Should().Be(6, "every number up to 5 is already taken");
    }

    [Fact]
    public async Task ReconcileWithStoredInvoicesAsync_SequenceIsAheadOfTheStoredInvoices_DoesNotHandOutATakenNumber()
    {
        var year = RandomYear();
        await SeedInvoicesAsync(year, series: "FT", numbers: [1]);
        using var sut = MakeSut();
        for (var i = 0; i < 4; i++) await sut.NextNumberAsync("FT", year);

        await sut.ReconcileWithStoredInvoicesAsync("FT", year);
        var next = await sut.NextNumberAsync("FT", year);

        next.Number.Should().Be(5, "reconciling must never wind a healthy sequence backwards");
    }

    [Fact]
    public async Task ReconcileWithStoredInvoicesAsync_AnotherYearHoldsHighNumbers_LeavesThisYearAtTheStart()
    {
        var year = RandomYear();
        await SeedInvoicesAsync(year - 1, series: "FT", numbers: [500]);
        using var sut = MakeSut();

        await sut.ReconcileWithStoredInvoicesAsync("FT", year);
        var next = await sut.NextNumberAsync("FT", year);

        next.Number.Should().Be(1, "the unique index partitions by the UTC year of IssuedAt");
    }

    private async Task SeedInvoicesAsync(int year, string series, int[] numbers)
    {
        using var db = _database.NewContext();
        foreach (var number in numbers)
        {
            var orderId = Guid.NewGuid();
            db.Orders.Add(TestOrders.Make(orderId));
            var invoice = TestOrders.MakeInvoice(orderId, series, number,
                invoiceNumber: $"{series}-{year}-{number:D5}");
            invoice.IssuedAt = new DateTimeOffset(year, 6, 3, 12, 0, 0, TimeSpan.Zero);
            db.Invoices.Add(invoice);
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task NextNumberAsync_LosesTheSequenceCreateRace_StillReturnsANumber()
    {
        var year = RandomYear();
        await using var rival = await UncommittedRelationCreator.SequenceAsync(
            _database.ConnectionString, $"invoice_seq_ft_{year}");

        var sut = MakeSut();
        Task<InvoiceNumber>? allocate = null;
        InvoiceNumber number = default;
        Exception? failure;

        try
        {
            allocate = Task.Run(() => sut.NextNumberAsync("FT", year));

            await rival.WaitUntilAnotherBackendBlocksAsync(TimeSpan.FromSeconds(10));
            await rival.CommitAsync();

            failure = await Record.ExceptionAsync(async () => number = await allocate);
        }
        finally
        {
            await rival.DisposeAsync();
            if (allocate is not null)
                await Record.ExceptionAsync(() => allocate);
            Record.Exception(() => sut.Dispose());
        }

        failure.Should().BeNull("the losing caller must draw a number instead of throwing");
        number.Number.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task NextNumberAsync_LosesTheRaceInsideTheCallerTransaction_TransactionStaysUsable()
    {
        var year = RandomYear();
        await using var rival = await UncommittedRelationCreator.SequenceAsync(
            _database.ConnectionString, $"invoice_seq_ft_{year}");

        var db = _database.NewContext();
        var transaction = await db.Database.BeginTransactionAsync();
        Task<InvoiceNumber>? allocate = null;
        InvoiceNumber number = default;
        Exception? failure;

        try
        {
            var service = new PostgresInvoiceNumberingService(
                db, NullLogger<PostgresInvoiceNumberingService>.Instance);
            allocate = Task.Run(() => service.NextNumberAsync("FT", year));

            await rival.WaitUntilAnotherBackendBlocksAsync(TimeSpan.FromSeconds(10));
            await rival.CommitAsync();

            failure = await Record.ExceptionAsync(async () =>
            {
                number = await allocate;
                await db.Database.ExecuteSqlRawAsync("SELECT 1");
                await transaction.CommitAsync();
            });
        }
        finally
        {
            await rival.DisposeAsync();
            if (allocate is not null)
                await Record.ExceptionAsync(() => allocate);
            await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask());
            await Record.ExceptionAsync(() => db.DisposeAsync().AsTask());
        }

        failure.Should().BeNull("the swallowed create must leave the caller transaction usable");
        number.Number.Should().BeGreaterThan(0);
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

    public Task ReconcileWithStoredInvoicesAsync(string series, int year, CancellationToken ct = default) =>
        _inner.ReconcileWithStoredInvoicesAsync(series, year, ct);

    public void Dispose() => _db.Dispose();
}
