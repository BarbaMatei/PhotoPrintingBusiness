namespace PhotoPrint.API.Exceptions;

/// <summary>
/// Thrown when an <c>Idempotency-Key</c> is already bound to an order owned by a
/// <b>different</b> caller. The global unique index spans tenants, so the caller's own
/// scoped lookup finds nothing yet their INSERT collides — a borrowed/guessed key or a
/// key-squatting probe. Distinct from <see cref="IdempotencyConflictException"/> (the
/// <i>same</i> caller resubmitting a divergent payload). Both map to HTTP 409; this one
/// carries no field detail (the caller owns no order to describe) but is logged as the
/// reserved <c>payments.idempotency.cross-tenant-conflict</c> event so the abuse signal
/// is distinguishable in incident triage.
/// </summary>
public sealed class IdempotencyKeyTakenException : ConflictException
{
    public IdempotencyKeyTakenException()
        : base("The Idempotency-Key is already associated with another request.")
    {
    }
}
