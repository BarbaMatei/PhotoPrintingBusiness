namespace PhotoPrint.API.Services;

/// <summary>
/// Thin wrapper around Stripe PaymentIntentService so the real HTTP call
/// can be replaced with a fake in tests.
/// </summary>
public interface IStripePaymentGateway
{
    /// <summary>
    /// Creates a Stripe PaymentIntent. When <paramref name="idempotencyKey"/> is
    /// non-null it is forwarded as Stripe's <c>RequestOptions.IdempotencyKey</c>,
    /// so duplicate charges are blocked at the gateway as well as in our DB.
    /// </summary>
    /// <returns>(ClientSecret, PaymentIntentId)</returns>
    Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        long amountBani,
        string currency,
        string orderIdMetadata,
        string? idempotencyKey = null,
        CancellationToken ct = default);

    /// <summary>Makes a PaymentIntent unconfirmable. Returns false when the gateway refuses —
    /// an intent already succeeded, or already cancelled — which callers treat as benign.</summary>
    Task<bool> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);
}
