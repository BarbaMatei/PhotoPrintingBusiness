using Stripe;

namespace PhotoPrint.API.Services;

public class StripePaymentGateway : IStripePaymentGateway
{
    private readonly PaymentIntentService _service;

    public StripePaymentGateway(IStripeClient stripeClient)
    {
        _service = new PaymentIntentService(stripeClient);
    }

    public async Task<bool> CancelPaymentIntentAsync(
        string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            await _service.CancelAsync(paymentIntentId, cancellationToken: ct);
            return true;
        }
        catch (StripeException)
        {
            return false;
        }
    }

    public async Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        long amountBani,
        string currency,
        string orderIdMetadata,
        string? idempotencyKey = null,
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

        // Gateway-side dedupe: same key → Stripe returns the same PaymentIntent.
        var requestOptions = idempotencyKey is null
            ? null
            : new RequestOptions { IdempotencyKey = idempotencyKey };

        var pi = await _service.CreateAsync(options, requestOptions, ct);
        return (pi.ClientSecret, pi.Id);
    }
}
