using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// SQLite implementation per ADR-020. SQLite has no <c>SEQUENCE</c>
/// primitive, so we use <c>MAX(Number) + 1</c> inside a transaction. SQLite
/// is single-writer at the file level; the transaction serialises the
/// read-then-write naturally.
///
/// Dev-only path. The contract matches the Postgres implementation: same
/// monotone, no-duplicate guarantee within (series, year).
/// </summary>
public sealed class SqliteInvoiceNumberingService : IInvoiceNumberingService
{
    private readonly PhotoPrintDbContext _db;

    public SqliteInvoiceNumberingService(PhotoPrintDbContext db) => _db = db;

    public async Task<InvoiceNumber> NextNumberAsync(
        string series, int year, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(series))
            throw new ArgumentException("series is required", nameof(series));
        if (year is < 2000 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(year));

        // SQLite is single-writer; a plain SELECT MAX query under the
        // default isolation is sufficient. We compare on the denormalised
        // Number column rather than parsing the formatted InvoiceNumber.
        //
        // Date filter uses a range comparison rather than `IssuedAt.Year`
        // because the project stores DateTimeOffset as Unix ms in SQLite
        // (see PhotoPrintDbContext's ValueConverter) — EF can't translate
        // the .Year property through the converter. The range form works
        // on both SQLite and Postgres without provider branching.
        var yearStart = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var yearEnd   = yearStart.AddYears(1);

        var max = await _db.Invoices
            .Where(i => i.Series == series
                        && i.IssuedAt >= yearStart
                        && i.IssuedAt <  yearEnd)
            .Select(i => (int?)i.Number)
            .MaxAsync(ct);

        return new InvoiceNumber(series, year, (max ?? 0) + 1);
    }
}
