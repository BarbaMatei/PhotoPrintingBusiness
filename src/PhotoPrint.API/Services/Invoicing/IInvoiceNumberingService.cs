namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Allocates monotone, gap-free (in normal flow) invoice numbers per
/// <c>(series, year)</c> partition. See ADR-020 for the load-bearing
/// trade-off (Postgres SEQUENCE vs counter-table) and the operational
/// mitigation for the rare rollback-gap case.
///
/// Implementations are provider-aware: <c>PostgresInvoiceNumberingService</c>
/// uses <c>nextval()</c>; <c>SqliteInvoiceNumberingService</c> uses
/// <c>MAX + 1</c> inside a transaction (single-writer DB makes this safe).
/// </summary>
public interface IInvoiceNumberingService
{
    /// <summary>Allocates the next number for <paramref name="series"/> in
    /// <paramref name="year"/>. Atomic across concurrent callers.</summary>
    Task<InvoiceNumber> NextNumberAsync(
        string series, int year, CancellationToken ct = default);
}
