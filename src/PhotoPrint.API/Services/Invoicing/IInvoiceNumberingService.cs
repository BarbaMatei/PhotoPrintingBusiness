namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Allocates monotone, gap-free (in normal flow) invoice numbers per
/// <c>(series, year)</c> partition. See ADR-020 for the load-bearing
/// trade-off (Postgres SEQUENCE vs counter-table) and the operational
/// mitigation for the rare rollback-gap case.
/// </summary>
public interface IInvoiceNumberingService
{
    /// <summary>Allocates the next number for <paramref name="series"/> in
    /// <paramref name="year"/>. Atomic across concurrent callers.</summary>
    Task<InvoiceNumber> NextNumberAsync(
        string series, int year, CancellationToken ct = default);

    /// <summary>Raises the allocator past the highest number already stored for
    /// <paramref name="series"/>/<paramref name="year"/>, so a store restored without its
    /// allocator state stops handing out numbers that are already taken. No-op by default.</summary>
    Task ReconcileWithStoredInvoicesAsync(
        string series, int year, CancellationToken ct = default) => Task.CompletedTask;
}
