using Stripe;

namespace PhotoPrint.API.Services;

public class StripeSignatureVerifier : IStripeSignatureVerifier
{
    public Event ConstructEvent(string json, string signature, string webhookSecret)
        => EventUtility.ConstructEvent(json, signature, webhookSecret);
}
