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
    /// <summary>Number of times a PaymentIntent was actually created — lets tests
    /// assert that an idempotent replay did NOT hit the gateway a second time.</summary>
    public int CreateCallCount { get; private set; }

    /// <summary>The most recent idempotency key forwarded to the gateway.</summary>
    public string? LastIdempotencyKey { get; private set; }

    public Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        long amountBani, string currency, string orderIdMetadata,
        string? idempotencyKey = null, CancellationToken ct = default)
    {
        CreateCallCount++;
        LastIdempotencyKey = idempotencyKey;
        return Task.FromResult(("pi_test_secret_fake", "pi_test_fake_id"));
    }
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

/// <summary>Records promotion enqueues so tests can assert the webhook→promotion wiring
/// (D59, review 043-v7) without a running worker consuming the channel.</summary>
public class RecordingPhotoPromoter : IOrderPhotoPromoter
{
    public List<Guid> Enqueued { get; } = new();

    public ValueTask EnqueueAsync(Guid orderId, CancellationToken ct = default)
    {
        Enqueued.Add(orderId);
        return ValueTask.CompletedTask;
    }

    public Task<PromotionOutcome> PromoteOrderAsync(Guid orderId, CancellationToken ct = default)
        => Task.FromResult(PromotionOutcome.Empty);
}

// ── Factory ───────────────────────────────────────────────────────────────────

/// <summary>
/// Extends <see cref="ShippingFactory"/> for payment/webhook integration tests.
/// Replaces Stripe and EuPlatesc services with test doubles.
/// </summary>
public class PaymentFactory : ShippingFactory
{
    public FakeStripeSignatureVerifier StripeVerifier { get; } = new();

    /// <summary>Shared fake gateway so tests can inspect call count / forwarded key
    /// across multiple requests (bolt 035 idempotency).</summary>
    public FakeStripePaymentGateway StripeGateway { get; } = new();

    /// <summary>Shared recording promoter — asserts the webhook→promotion wiring (D59).</summary>
    public RecordingPhotoPromoter PhotoPromoter { get; } = new();

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
            services.AddScoped<IStripePaymentGateway>(_ => StripeGateway);
            services.AddScoped<IStripeSignatureVerifier>(_ => StripeVerifier);
            services.AddScoped<IOrderPhotoPromoter>(_ => PhotoPromoter);
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

        // QUAL-3 (review 035-v8): shared canonical cart graph — see TestCartSeed.
        var graph = TestCartSeed.Build(userId, guestSessionId, unitPrice, quantity);
        graph.AddTo(db);
        await db.SaveChangesAsync();

        return (graph.Product.Id, graph.Upload.Id);
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
