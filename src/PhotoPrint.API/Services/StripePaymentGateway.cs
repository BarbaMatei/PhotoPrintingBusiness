using Stripe;

namespace PhotoPrint.API.Services;

public class StripePaymentGateway : IStripePaymentGateway
{
    private readonly PaymentIntentService _service;

    public StripePaymentGateway(IStripeClient stripeClient)
    {
        _service = new PaymentIntentService(stripeClient);
    }

    public async Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        long amountBani,
        string currency,
        string orderIdMetadata,
        CancellationToken ct = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountBani,
            Currency = currency,
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = orderIdMetadata,
            },
        };

        var pi = await _service.CreateAsync(options, cancellationToken: ct);
        return (pi.ClientSecret, pi.Id);
    }
}
