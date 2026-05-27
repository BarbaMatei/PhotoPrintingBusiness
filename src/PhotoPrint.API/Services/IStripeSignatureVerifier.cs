using Stripe;

namespace PhotoPrint.API.Services;

/// <summary>
/// Abstracts <see cref="EventUtility.ConstructEvent"/> so the webhook controller
/// can be tested without a real Stripe secret or valid signature header.
/// </summary>
public interface IStripeSignatureVerifier
{
    /// <summary>
    /// Constructs and validates a Stripe event from the raw JSON body.
    /// </summary>
    /// <exception cref="StripeException">Thrown when the signature is invalid.</exception>
    Event ConstructEvent(string json, string signature, string webhookSecret);
}
