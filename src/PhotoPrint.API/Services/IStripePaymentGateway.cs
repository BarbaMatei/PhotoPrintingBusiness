namespace PhotoPrint.API.Services;

/// <summary>
/// Thin wrapper around Stripe PaymentIntentService so the real HTTP call
/// can be replaced with a fake in tests.
/// </summary>
public interface IStripePaymentGateway
{
    /// <summary>
    /// Creates a Stripe PaymentIntent.
    /// </summary>
    /// <returns>(ClientSecret, PaymentIntentId)</returns>
    Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        long amountBani,
        string currency,
        string orderIdMetadata,
        CancellationToken ct = default);
}
