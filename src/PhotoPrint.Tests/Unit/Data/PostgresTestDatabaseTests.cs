using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Unit.Data;

public class PostgresTestDatabaseTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public PostgresTestDatabaseTests(PostgresTestDatabase database)
    {
        _database = database;
        database.ResetForTest();
    }

    [Fact]
    public async Task ResetForTest_ClearsAParentAndTheRowThatReferencesIt()
    {
        var orderId = Guid.NewGuid();

        await using (var seed = _database.NewContext())
        {
            seed.Orders.Add(TestOrders.Make(orderId));
            seed.Invoices.Add(TestOrders.MakeInvoice(orderId, series: "FT", number: 7001));
            await seed.SaveChangesAsync();
        }

        _database.ResetForTest();

        await using var verify = _database.NewContext();
        (await verify.Orders.CountAsync()).Should().Be(0);
        (await verify.Invoices.CountAsync()).Should().Be(0,
            "a reset that deletes parents before children would leave the child behind or throw");
    }

    [Fact]
    public async Task ResetForTest_RewindsASequenceNoTableColumnOwns()
    {
        await using (var consume = _database.NewContext())
        {
            var first = await NextInvoiceNumber(consume);
            var second = await NextInvoiceNumber(consume);

            first.Should().Be(1);
            second.Should().Be(2);
        }

        _database.ResetForTest();

        await using var afterReset = _database.NewContext();
        (await NextInvoiceNumber(afterReset)).Should().Be(1,
            "invoice numbering restarts each year, so a reused database must not carry the counter on");
    }

    [Fact]
    public async Task ResetForTest_DropsASequenceCreatedAtRuntime()
    {
        await using (var create = _database.NewContext())
            await create.Database.ExecuteSqlRawAsync("CREATE SEQUENCE \"pp_probe_seq_2026\" START 1");

        _database.ResetForTest();

        await using var verify = _database.NewContext();
        var found = await verify.Database
            .SqlQueryRaw<string>(
                "SELECT sequencename AS \"Value\" FROM pg_sequences WHERE sequencename = 'pp_probe_seq_2026'")
            .ToListAsync();

        found.Should().BeEmpty(
            "a per-year sequence left behind by one test makes the next test's create a duplicate");
    }

    private static async Task<long> NextInvoiceNumber(PhotoPrint.API.Data.PhotoPrintDbContext db) =>
        (await db.Database
            .SqlQueryRaw<long>("SELECT nextval('\"invoice_seq_ft_2026\"') AS \"Value\"")
            .ToListAsync())
        .Single();
}
