namespace PhotoPrint.API.DTOs.Payments;

/// <summary>
/// Returned after successfully creating a Stripe PaymentIntent.
/// </summary>
public record StripeIntentResponse(string ClientSecret, Guid OrderId);
