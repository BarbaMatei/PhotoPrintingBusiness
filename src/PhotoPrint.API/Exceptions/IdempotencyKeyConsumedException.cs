namespace PhotoPrint.API.Exceptions;

// Alone among the 409s it names the settled order, so the client can send the customer there.
public sealed class IdempotencyKeyConsumedException : ConflictException
{
    public Guid OrderId { get; }

    public IdempotencyKeyConsumedException(Guid orderId)
        : base("The Idempotency-Key belongs to an order that is no longer awaiting payment.")
    {
        OrderId = orderId;
    }
}
