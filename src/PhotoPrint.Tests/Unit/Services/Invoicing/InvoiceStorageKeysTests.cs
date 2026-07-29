using FluentAssertions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

/// <summary>
/// ADR-007 / ADR-021 — storage key shape for invoice PDFs. The path is
/// caller-supplied; adapters persist bytes at the literal key. Stable key
/// shape matters because object lifecycle rules (S3 expirations, R2 cache
/// invalidation) match on this prefix.
/// </summary>
public class InvoiceStorageKeysTests
{
    [Fact]
    public void Pdf_key_partitions_by_year_then_month_then_invoice_number()
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "FT-2026-00042",
            IssuedAt = new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero),
        };

        InvoiceStorageKeys.ForPdf(invoice)
            .Should().Be("invoices/2026/06/FT-2026-00042.pdf");
    }

    [Fact]
    public void Pdf_key_uses_utc_components_so_timezone_changes_dont_repartition()
    {
        // 2027-01-01 00:30 UTC+02 = 2026-12-31 22:30 UTC.
        // The key must use the UTC components so a customer near midnight
        // on New Year's Eve doesn't straddle two object lifecycle buckets.
        var invoice = new Invoice
        {
            InvoiceNumber = "FT-2026-99999",
            IssuedAt = new DateTimeOffset(2027, 1, 1, 0, 30, 0, TimeSpan.FromHours(2)),
        };

        InvoiceStorageKeys.ForPdf(invoice)
            .Should().Be("invoices/2026/12/FT-2026-99999.pdf");
    }
}
