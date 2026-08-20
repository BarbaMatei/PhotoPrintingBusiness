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

        // InMemory (tests) has no sequence support, so it falls back to a count-based
        // number; the unique index on OrderNumber is the backstop.
        if (_db.Database.ProviderName is DbProviders.InMemory)
        {
            var count = await _db.Orders.CountAsync(ct);
            return FormatOrderNumber(year, count + 1);
        }

        // PostgreSQL: use a per-year sequence to guarantee uniqueness under concurrency.
        var seqName = $"order_number_seq_{year}";

        // EF1002 flags the interpolation, but `seqName` is built
        // ONLY from `DateTime.UtcNow.Year` (a server-side int) — no client/user input
        // reaches it, so there is no injection vector. The interpolation also targets a
        // DDL identifier (CREATE SEQUENCE "<name>"), which cannot be a SQL parameter, so
        // suppression — not parameterization — is the correct resolution here.
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection — N/A: server-side int only, DDL identifier.
        await _db.Database.ExecuteSqlRawAsync($"""
            DO $$ BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE sequencename = '{seqName}') THEN
                CREATE SEQUENCE "{seqName}" START 1;
              END IF;
            END $$;
            """, ct);
#pragma warning restore EF1002

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
