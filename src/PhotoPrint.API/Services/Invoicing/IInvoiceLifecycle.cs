using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// CAS façade over <c>ExecuteUpdateAsync</c> per ADR-016 for
/// <see cref="Invoice.AnafStatus"/> transitions. Every status mutation in
/// bolt 039 — worker poll outcomes, admin retries, give-up after backoff
/// exhaustion — goes through one of these methods.
///
/// Each returns <c>true</c> on success; <c>false</c> when the CAS predicate
/// missed (another worker won the race, or the row was admin-edited in the
/// meantime). Callers log at Information and exit — not an error.
///
/// Method-per-transition (instead of a generic mutator delegate) matches
/// the project's existing CAS shape (see <c>ShipmentTrackingJob</c>,
/// <c>AdminOrderService</c>) and keeps each SetProperty list literal so
/// EF Core 8 can translate it.
/// </summary>
public interface IInvoiceLifecycle
{
    /// <summary>Pending → Submitted on a successful ANAF upload.</summary>
    Task<bool> MarkSubmittedAsync(Guid invoiceId, string anafUploadId, CancellationToken ct = default);

    /// <summary>Stays Pending; records the body-encoded ANAF error from a
    /// 200-with-errors response so it appears in the admin list.</summary>
    Task<bool> RecordPendingErrorAsync(Guid invoiceId, string errorMessage, CancellationToken ct = default);

    /// <summary>Records an error against a row in <paramref name="expected"/>, for failures that can strike either a Pending or a Submitted invoice.</summary>
    Task<bool> RecordErrorAsync(Guid invoiceId, string errorMessage, InvoiceAnafStatus expected, CancellationToken ct = default);

    /// <summary>Submitted → Accepted on a successful ANAF status poll.</summary>
    Task<bool> MarkAcceptedAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>Submitted → Rejected with an ANAF error message.
    /// Caller chooses Rejected vs Failed based on the backoff budget.</summary>
    Task<bool> MarkRejectedAsync(Guid invoiceId, string errorMessage, CancellationToken ct = default);

    /// <summary>Submitted → Failed (backoff budget exhausted).</summary>
    Task<bool> MarkFailedAsync(Guid invoiceId, string errorMessage, CancellationToken ct = default);

    /// <summary>Counts one upload whose outcome ANAF never confirmed, and moves the row
    /// Pending → Failed once <paramref name="maxOutcomes"/> of them have accumulated, so a
    /// blind re-post of the same invoice number cannot repeat for ever.</summary>
    Task<UnknownUploadOutcome> RecordUnknownUploadOutcomeAsync(
        Guid invoiceId, string errorMessage, string budgetSpentMessage, int maxOutcomes,
        CancellationToken ct = default);

    /// <summary>Pending → Failed for an invoice no retry can build, so it stops being re-attempted.</summary>
    Task<bool> ParkUnbuildableAsync(Guid invoiceId, string reason, CancellationToken ct = default);

    /// <summary>Rejected|Failed → Pending on an admin retry.
    /// Clears <c>AnafUploadId</c>, <c>LastError</c> and the blind re-post count.</summary>
    /// <summary>Gives up on a rejected invoice once its backoff schedule is spent: Rejected to
    /// Failed, claim released so the admin retry endpoint can pick it up.</summary>
    Task<bool> GiveUpOnRejectedAsync(Guid invoiceId, string reason, CancellationToken ct = default);

    Task<bool> RetryAsync(Guid invoiceId, InvoiceAnafStatus expected, CancellationToken ct = default);
}

public readonly record struct UnknownUploadOutcome(int Outcomes, bool Parked);
