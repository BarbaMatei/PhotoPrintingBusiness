namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Allocates monotone, gap-free (in normal flow) invoice numbers per
/// <c>(series, year)</c> partition. The Postgres sequence behind it advances
/// even when the calling transaction rolls back, so a rare numbering gap is
/// the accepted trade-off.
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
