using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Postgres implementation per ADR-020. Uses
/// <c>CREATE SEQUENCE IF NOT EXISTS</c> + <c>nextval()</c> for atomic
/// gap-tolerant numbering. The sequence is created idempotently on every
/// call — cheap, and removes any "did the migration cover this year?"
/// concern when the year rolls over.
///
/// Gap-on-rollback is the documented trade-off (the sequence advances
/// even if the calling transaction rolls back). Callers must invoke this
/// inside the same transaction that persists the <c>Invoice</c> row, and
/// must NOT perform external I/O inside that transaction.
/// </summary>
public sealed class PostgresInvoiceNumberingService : IInvoiceNumberingService
{
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<PostgresInvoiceNumberingService> _logger;

    public PostgresInvoiceNumberingService(
        PhotoPrintDbContext db,
        ILogger<PostgresInvoiceNumberingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<InvoiceNumber> NextNumberAsync(
        string series, int year, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(series))
            throw new ArgumentException("series is required", nameof(series));
        if (year is < 2000 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(year));

        // Quoted lowercase identifier mirrors the convention in the seed
        // migration ('invoice_seq_ft_2026'). The series is validated by
        // VatSettingsValidator to be uppercase ASCII letters only, so the
        // lowercase mapping is well-defined and SQL-injection-safe.
        var seqName = $"invoice_seq_{series.ToLowerInvariant()}_{year}";

        // Idempotent create — Postgres handles concurrent IF NOT EXISTS safely.
        await _db.Database.ExecuteSqlRawAsync(
            $"CREATE SEQUENCE IF NOT EXISTS \"{seqName}\" START 1 INCREMENT 1",
            ct);

        var next = await _db.Database
            .SqlQueryRaw<long>($"SELECT nextval('\"{seqName}\"') AS \"Value\"")
            .SingleAsync(ct);

        if (next < 1 || next > int.MaxValue)
        {
            _logger.LogError(
                "invoice.numbering.out-of-range series={Series} year={Year} value={Value}",
                series, year, next);
            throw new InvalidOperationException(
                $"Invoice sequence '{seqName}' returned out-of-range value {next}.");
        }

        return new InvoiceNumber(series, year, (int)next);
    }
}
