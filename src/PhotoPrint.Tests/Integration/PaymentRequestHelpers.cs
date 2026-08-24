using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PhotoPrint.API.DTOs.Payments;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// The payment-endpoint POST builders were duplicated across
/// <see cref="PaymentControllerIntegrationTests"/> and <see cref="PaymentIdempotencyRelationalTests"/>
/// (build an <see cref="HttpRequestMessage"/>, attach the JSON body + the
/// <c>Idempotency-Key</c> header, send). Centralized here as <see cref="HttpClient"/>
/// extensions so the request shape lives in one place.
/// </summary>
internal static class PaymentRequestHelpers
{
    public static Task<HttpResponseMessage> PostStripeIntentAsync(
        this HttpClient client, CreateOrderRequest body, string idempotencyKey)
        => PostWithKeyAsync(client, "/api/payments/stripe/intent", body, idempotencyKey);

    private static Task<HttpResponseMessage> PostWithKeyAsync(
        HttpClient client, string url, CreateOrderRequest body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }
}
