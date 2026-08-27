using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public class MigrationChainTests
{
    // Nothing executed the chain against Postgres until the SQLite drop, and the composite invoice index was rejected as non-immutable on the first real boot.
    [Fact]
    public async Task The_chain_applies_to_a_fresh_postgres_database_and_leaves_no_pending_migration()
    {
        using var database = PostgresTestDatabase.Throwaway();
        using var db = database.NewContext();

        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await db.Database.GetAppliedMigrationsAsync()).Should().NotBeEmpty();
    }

    // Drift that adds no column — an index, a length, a precision — is invisible to the tests below, and the next scaffolded migration would diff against the stale snapshot.
    [Fact]
    public void The_model_snapshot_still_matches_the_model()
    {
        using var db = new PhotoPrintDbContext(
            new DbContextOptionsBuilder<PhotoPrintDbContext>()
                .UseNpgsql("Host=localhost;Database=pp_model_comparison_only")
                .Options);

        db.Database.HasPendingModelChanges().Should().BeFalse(
            "the model changed with no migration scaffolded, so run dotnet ef migrations add");
    }

    [Fact]
    public async Task The_composite_invoice_index_exists_after_migrating()
    {
        using var database = PostgresTestDatabase.Throwaway();
        using var db = database.NewContext();

        var found = await db.Database
            .SqlQueryRaw<string>(
                "SELECT indexname AS \"Value\" FROM pg_indexes WHERE indexname = {0}",
                PhotoPrintDbContext.InvoiceSeriesYearNumberIndexName)
            .ToListAsync();

        found.Should().ContainSingle(
            "the index is raw SQL in the migration, so only a real migrate proves it was accepted");
    }

    [Fact]
    public async Task The_composite_invoice_index_rejects_a_repeated_series_year_number()
    {
        using var database = PostgresTestDatabase.Throwaway();
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();

        using var db = database.NewContext();
        db.Orders.Add(TestOrders.Make(orderA));
        db.Orders.Add(TestOrders.Make(orderB));
        await db.SaveChangesAsync();

        db.Invoices.Add(TestOrders.MakeInvoice(orderA, series: "FT", number: 900));
        await db.SaveChangesAsync();

        // Same series, year and number but a different InvoiceNumber string, so only the composite index catches it.
        db.Invoices.Add(TestOrders.MakeInvoice(orderB, series: "FT", number: 900, invoiceNumber: "FT-2026-00900-dup"));

        var act = () => db.SaveChangesAsync();

        var ex = await act.Should().ThrowAsync<DbUpdateException>();
        ex.Which.InnerException.Should().BeOfType<Npgsql.PostgresException>()
            .Which.ConstraintName.Should().Be(PhotoPrintDbContext.InvoiceSeriesYearNumberIndexName);
    }
}
