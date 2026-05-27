using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using StripeEvent = Stripe.Event;
using StripeException = Stripe.StripeException;
using StripeClient = Stripe.StripeClient;
using IStripeClient = Stripe.IStripeClient;

namespace PhotoPrint.Tests.Integration;

// ── Fake services ─────────────────────────────────────────────────────────────

/// <summary>Returns a predictable fake client secret — no HTTP calls to Stripe.</summary>
public class FakeStripePaymentGateway : IStripePaymentGateway
{
    public Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        long amountBani, string currency, string orderIdMetadata, CancellationToken ct = default)
        => Task.FromResult(("pi_test_secret_fake", "pi_test_fake_id"));
}

/// <summary>Configurable Stripe signature verifier for integration tests.</summary>
public class FakeStripeSignatureVerifier : IStripeSignatureVerifier
{
    public bool ShouldThrow { get; set; }

    public StripeEvent ConstructEvent(string json, string signature, string webhookSecret)
    {
        if (ShouldThrow)
            throw new StripeException("Invalid signature");

        // Controller only needs stripeEvent.Type for routing.
        // It reads the PaymentIntent ID directly from the raw JSON body,
        // so we don't need Stripe.net's polymorphic Data.Object deserialization.
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var eventType = doc.RootElement.TryGetProperty("type", out var typeEl)
            ? typeEl.GetString() ?? ""
            : "";

        return new StripeEvent { Type = eventType };
    }
}

// ── Factory ───────────────────────────────────────────────────────────────────

/// <summary>
/// Extends <see cref="ShippingFactory"/> for payment/webhook integration tests.
/// Replaces Stripe and EuPlatesc services with test doubles.
/// </summary>
public class PaymentFactory : ShippingFactory
{
    public FakeStripeSignatureVerifier StripeVerifier { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"]     = "sk_test_fake",
                ["Stripe:WebhookSecret"] = "whsec_fake",
                ["EuPlatesc:MerchantId"] = "TEST_MERCH",
                ["EuPlatesc:SecretKey"]  = "000102030405060708090a0b0c0d0e0f",
                ["EuPlatesc:GatewayUrl"] = "https://secure.euplatesc.ro/tdsprocess/tranzactd.php",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace real Stripe client so it doesn't try to use an empty API key
            services.AddSingleton<IStripeClient>(new StripeClient("sk_test_fake"));
            services.AddScoped<IStripePaymentGateway>(_ => new FakeStripePaymentGateway());
            services.AddScoped<IStripeSignatureVerifier>(_ => StripeVerifier);
        });
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds a product + upload + cart item for the given user/guest.
    /// Returns (productId, uploadId).
    /// </summary>
    public async Task<(Guid ProductId, Guid UploadId)> SeedCartItemAsync(
        Guid? userId = null,
        Guid? guestSessionId = null,
        decimal unitPrice = 2.00m,
        int quantity = 3)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var product = new Product { Name = "Foto 10x15", IsActive = true };
        var size = new ProductSize
        {
            ProductId = product.Id, Label = "10x15", WidthMm = 100, HeightMm = 150, IsActive = true,
        };
        var tier = new PricingTier
        {
            ProductSizeId = size.Id, MinQuantity = 1, MaxQuantity = null, UnitPrice = unitPrice,
        };
        var finish = new ProductFinish { ProductId = product.Id, Name = "Lucios" };

        var upload = new Upload
        {
            UserId = userId,
            GuestSessionId = guestSessionId,
            OriginalFileName = "photo.jpg",
            FilePath = "/uploads/photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 1800,
            HeightPx = 1200,
        };

        var cartItem = new CartItem
        {
            UserId = userId,
            GuestSessionId = guestSessionId,
            UploadId = upload.Id,
            ProductId = product.Id,
            Quantity = quantity,
        };

        db.Products.Add(product);
        db.ProductSizes.Add(size);
        db.PricingTiers.Add(tier);
        db.ProductFinishes.Add(finish);
        db.Uploads.Add(upload);
        db.CartItems.Add(cartItem);
        await db.SaveChangesAsync();

        return (product.Id, upload.Id);
    }

    /// <summary>Seeds an Order in AwaitingPayment status and sets a fake PaymentIntentId.</summary>
    public async Task<Order> SeedOrderAsync(
        string paymentIntentId = "pi_test_fake_id",
        OrderStatus status = OrderStatus.AwaitingPayment,
        decimal totalRon = 26.00m)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var order = new Order
        {
            OrderNumber = "FT-20260001",
            Status = status,
            PaymentProcessor = PaymentProcessor.Stripe,
            PaymentIntentId = paymentIntentId,
            ShippingAddress = new PhotoPrint.API.Models.ShippingAddressSnapshot
            {
                Street = "Str. Test", Number = "1", City = "București",
                County = "Ilfov", PostalCode = "010101", RecipientName = "Test", Phone = "0700000000",
            },
            DeliveryType = DeliveryType.Courier,
            ShippingCostRon = 25.00m,
            SubtotalRon = totalRon - 25.00m,
            TotalRon = totalRon,
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }
}
