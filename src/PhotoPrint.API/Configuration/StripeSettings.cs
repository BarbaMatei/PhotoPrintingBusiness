namespace PhotoPrint.API.Configuration;

public class StripeSettings
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string PublishableKey { get; set; } = "";

    public decimal MinimumChargeRon { get; set; } = 2.00m;
}
