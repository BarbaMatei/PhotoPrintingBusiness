using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Invoicing;
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

/// <summary>
/// In-memory invoice numbering for integration tests. Bolt 039 wires
/// <see cref="IInvoiceCreationService"/> into the webhook handler, which
/// calls <see cref="IInvoiceNumberingService"/>. The production Postgres
/// implementation uses raw SQL <c>nextval()</c> which the InMemory provider
/// can't execute; this fake walks an in-process counter per (series, year).
/// </summary>
public class FakeInvoiceNumberingService : IInvoiceNumberingService
{
    private readonly Dictionary<(string Series, int Year), int> _counters = new();
    private readonly object _lock = new();

    public Task<InvoiceNumber> NextNumberAsync(string series, int year, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var key = (series, year);
            _counters.TryGetValue(key, out var current);
            _counters[key] = current + 1;
            return Task.FromResult(new InvoiceNumber(series, year, current + 1));
        }
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
                // Bolt 039: Seller fiscal identity is validated at boot when
                // bolt 038's invoice path is reachable. Populate test values
                // matching SellerSettingsValidator (Cui matches ^RO\d{2,10}$,
                // CountryCode is ISO alpha-2).
                ["Seller:Name"]                  = "Test Seller SRL",
                ["Seller:Cui"]                   = "RO12345678",
                ["Seller:RegistrationNumber"]    = "J40/1234/2026",
                ["Seller:Address:Line1"]         = "Str. Exemplu 1",
                ["Seller:Address:City"]          = "București",
                ["Seller:Address:PostalCode"]    = "010101",
                ["Seller:Address:CountryCode"]   = "RO",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace real Stripe client so it doesn't try to use an empty API key
            services.AddSingleton<IStripeClient>(new StripeClient("sk_test_fake"));
            services.AddScoped<IStripePaymentGateway>(_ => StripeGateway);
            services.AddScoped<IStripeSignatureVerifier>(_ => StripeVerifier);

            // Bolt 039: the production Postgres numbering service uses raw SQL
            // nextval() which InMemory can't execute. Swap in the in-process
            // counter so the webhook → InvoiceCreationService path works in tests.
            var numberingDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(IInvoiceNumberingService));
            if (numberingDesc is not null)
                services.Remove(numberingDesc);
            services.AddSingleton<IInvoiceNumberingService, FakeInvoiceNumberingService>();
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
