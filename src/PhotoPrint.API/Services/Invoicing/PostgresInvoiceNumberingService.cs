using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
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

        // Lowercase mirrors the seed migration's 'invoice_seq_ft_2026'.
        var seqName = $"invoice_seq_{series.ToLowerInvariant()}_{year}";

        await PostgresSequences.EnsureAsync(_db.Database, seqName, ct);

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

    public async Task ReconcileWithStoredInvoicesAsync(
        string series, int year, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(series))
            throw new ArgumentException("series is required", nameof(series));
        if (year is < 2000 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(year));

        var seqName = $"invoice_seq_{series.ToLowerInvariant()}_{year}";
        await PostgresSequences.EnsureAsync(_db.Database, seqName, ct);

        // Mirrors uq_invoices_series_year_number: the year is derived from IssuedAt in UTC, not stored.
        var highest = await _db.Database.SqlQuery<int>($"""
            SELECT COALESCE(MAX("Number"), 0) AS "Value" FROM "Invoices"
            WHERE "Series" = {series}
              AND (EXTRACT(YEAR FROM ("IssuedAt" AT TIME ZONE 'UTC'))::int) = {year}
            """).SingleAsync(ct);

        if (highest <= 0) return;

        var reconciled = await _db.Database.SqlQuery<long>($"""
            SELECT setval(
                {seqName}::regclass,
                GREATEST({(long)highest}, COALESCE((
                    SELECT last_value FROM pg_sequences
                    WHERE schemaname = current_schema() AND sequencename = {seqName}), 0)),
                true) AS "Value"
            """).SingleAsync(ct);

        _logger.LogWarning(
            "invoice.numbering.sequence-reconciled series={Series} year={Year} highest_stored={Highest} sequence_now={Reconciled}",
            series, year, highest, reconciled);
    }
}
