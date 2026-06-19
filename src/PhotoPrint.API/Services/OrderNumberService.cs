using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.Services;

public class OrderNumberService : IOrderNumberService
{
    private readonly PhotoPrintDbContext _db;

    public OrderNumberService(PhotoPrintDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;

        // Non-Postgres providers have no per-year sequence: InMemory (tests) and SQLite
        // (local/dev — appsettings.Development.json sets DatabaseProvider=Sqlite) both use
        // a simple count-based number. The unique index on OrderNumber is the backstop;
        // these providers don't carry production write-concurrency. (BUG-6, review 035-v3.)
        if (_db.Database.ProviderName is "Microsoft.EntityFrameworkCore.InMemory"
            or "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var count = await _db.Orders.CountAsync(ct);
            return FormatOrderNumber(year, count + 1);
        }

        // PostgreSQL: use a per-year sequence to guarantee uniqueness under concurrency.
        var seqName = $"order_number_seq_{year}";

        await _db.Database.ExecuteSqlRawAsync($"""
            DO $$ BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE sequencename = '{seqName}') THEN
                CREATE SEQUENCE "{seqName}" START 1;
              END IF;
            END $$;
            """, ct);

        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT nextval('\"{seqName}\"')";
        var result = await cmd.ExecuteScalarAsync(ct);
        return FormatOrderNumber(year, Convert.ToInt64(result));
    }

    /// <summary>Pure formatting helper — exposed for unit testing.</summary>
    public static string FormatOrderNumber(int year, long seq)
        => $"FT-{year}{seq:D4}";
}
