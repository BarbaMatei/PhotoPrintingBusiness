using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Creates the <see cref="Invoice"/> row at the Paid transition. Called
/// from the Stripe webhook handler and the admin Paid transition inside their existing
/// transactional scope — adding the Invoice INSERT to the same
/// <c>SaveChangesAsync</c> preserves the gap-free numbering posture
/// (ADR-020): if the Paid transition rolls back, both the Order mutation
/// and the Invoice row disappear together.
///
/// Idempotent on order id: a replay returns the existing Invoice without
/// allocating a new number (defending bolt 035's payment-replay path).
/// </summary>
public interface IInvoiceCreationService
{
    /// <summary>
    /// Creates or returns the existing invoice for <paramref name="orderId"/>.
    /// Does NOT call <c>SaveChangesAsync</c> — the caller's transaction
    /// commits the batch. Returns <c>null</c> only when the order does not
    /// exist (a defensive guard; callers shouldn't reach this path).
    /// </summary>
    Task<Invoice?> CreateForOrderAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Same idempotent-create contract, for a caller that already holds the tracked <see cref="Order"/>.</summary>
    Task<Invoice?> CreateForOrderAsync(Order order, CancellationToken ct = default);
}
