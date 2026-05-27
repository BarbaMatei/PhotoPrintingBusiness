using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Models;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

public class PaymentControllerIntegrationTests : IClassFixture<PaymentFactory>
{
    private readonly PaymentFactory _factory;
    private readonly HttpClient _client;

    private static readonly CreateOrderRequest ValidRequest = new(
        PaymentProcessor: PaymentProcessor.Stripe,
        DeliveryType: DeliveryType.Easybox,
        EasyboxLockerId: Guid.NewGuid(),
        ShippingAddress: null);

    private static readonly CreateOrderRequest EuPlatescRequest = new(
        PaymentProcessor: PaymentProcessor.EuPlatesc,
        DeliveryType: DeliveryType.Courier,
        EasyboxLockerId: null,
        ShippingAddress: new PhotoPrint.API.Models.ShippingAddressSnapshot
        {
            Street = "Str. Test", Number = "1", City = "București",
            County = "Ilfov", PostalCode = "010101", RecipientName = "Test", Phone = "0700000000",
        });

    public PaymentControllerIntegrationTests(PaymentFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── POST /api/payments/stripe/intent ──────────────────────────────────────

    [Fact]
    public async Task CreateStripeIntent_ValidCart_Returns200WithClientSecret()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 2.00m, quantity: 5);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/payments/stripe/intent", ValidRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<StripeIntentResponse>();
        Assert.NotNull(dto);
        Assert.Equal("pi_test_secret_fake", dto!.ClientSecret);
        Assert.NotEqual(Guid.Empty, dto.OrderId);
    }

    [Fact]
    public async Task CreateStripeIntent_EmptyCart_Returns400()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/payments/stripe/intent", ValidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateStripeIntent_NoAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/payments/stripe/intent", ValidRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── bolt 034 — server-side shipping cost ──────────────────────────────────

    [Fact]
    public async Task CreateStripeIntent_TamperedShippingCostInBody_IsIgnored_OrderTotalReflectsServerResolvedCost()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        // 5 prints × 2.00 RON each = 10.00 RON subtotal
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 2.00m, quantity: 5);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Send raw JSON that includes the now-removed shippingCostRon field with
        // a tampered (negative) value. The server must ignore it entirely.
        // Enums are serialized as integers on the wire (no JsonStringEnumConverter
        // is registered on the API). PaymentProcessor.Stripe = 0, DeliveryType.Easybox = 0.
        var tamperedJson = $$"""
            {
              "paymentProcessor": 0,
              "deliveryType": 0,
              "easyboxLockerId": "{{Guid.NewGuid()}}",
              "shippingAddress": null,
              "shippingCostRon": -100
            }
            """;

        var response = await client.PostAsync(
            "/api/payments/stripe/intent",
            new StringContent(tamperedJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<StripeIntentResponse>();
        Assert.NotNull(dto);

        // Pull the persisted order and verify the server-resolved shipping cost.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        var order = await db.Orders.FindAsync(dto!.OrderId);
        Assert.NotNull(order);

        // Easybox flat rate in appsettings.json is 20.00 RON.
        Assert.Equal(20.00m, order!.ShippingCostRon);
        // Subtotal 10.00 + shipping 20.00 = 30.00 RON. NEVER -90.00.
        Assert.Equal(10.00m, order.SubtotalRon);
        Assert.Equal(30.00m, order.TotalRon);
    }

    // ── POST /api/payments/euplatesc/initiate ─────────────────────────────────

    [Fact]
    public async Task InitiateEuPlatesc_ValidCart_Returns200WithRedirectUrl()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 1.50m, quantity: 4);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/payments/euplatesc/initiate", EuPlatescRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<EuPlatescInitiateResponse>();
        Assert.NotNull(dto);
        Assert.Contains("euplatesc.ro", dto!.RedirectUrl);
        Assert.NotEqual(Guid.Empty, dto.OrderId);
    }

    [Fact]
    public async Task InitiateEuPlatesc_EmptyCart_Returns400()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/payments/euplatesc/initiate", EuPlatescRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── POST /api/webhooks/stripe ─────────────────────────────────────────────

    [Fact]
    public async Task StripeWebhook_ValidSignature_PaymentSucceeded_TransitionsOrderToPaid()
    {
        var order = await _factory.SeedOrderAsync(paymentIntentId: "pi_wh_success", totalRon: 30.00m);

        // Build a fake Stripe event for payment_intent.succeeded
        var eventJson = $$"""
            {
              "id": "evt_test_001",
              "object": "event",
              "type": "payment_intent.succeeded",
              "data": {
                "object": {
                  "id": "pi_wh_success",
                  "object": "payment_intent",
                  "amount": 3000,
                  "currency": "ron",
                  "client_secret": "pi_wh_success_secret_abc"
                }
              }
            }
            """;

        _factory.StripeVerifier.ShouldThrow = false;

        var response = await _client.PostAsync(
            "/api/webhooks/stripe",
            new StringContent(eventJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify order status updated
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        var updated = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Paid, updated!.Status);
        Assert.NotNull(updated.PaidAt);
    }

    [Fact]
    public async Task StripeWebhook_InvalidSignature_Returns400()
    {
        _factory.StripeVerifier.ShouldThrow = true;

        var response = await _client.PostAsync(
            "/api/webhooks/stripe",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StripeWebhook_AlreadyPaidOrder_Returns200Silent()
    {
        var order = await _factory.SeedOrderAsync(
            paymentIntentId: "pi_already_paid", status: OrderStatus.Paid);

        var eventJson = $$"""
            {
              "id": "evt_dup",
              "object": "event",
              "type": "payment_intent.succeeded",
              "data": {
                "object": {
                  "id": "pi_already_paid",
                  "object": "payment_intent",
                  "amount": 100,
                  "currency": "ron",
                  "client_secret": "pi_already_paid_secret"
                }
              }
            }
            """;

        _factory.StripeVerifier.ShouldThrow = false;

        var response = await _client.PostAsync(
            "/api/webhooks/stripe",
            new StringContent(eventJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Status must remain Paid (no double-transition)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        var updated = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Paid, updated!.Status);
    }

    // ── POST /api/webhooks/euplatesc ──────────────────────────────────────────

    [Fact]
    public async Task EuPlatescIpn_ValidSignature_Action0_OrderPaid()
    {
        var order = await _factory.SeedOrderAsync(totalRon: 31.00m);

        // Build valid IPN fields with HMAC
        var fields = new Dictionary<string, string>
        {
            ["amount"]     = "31.00",
            ["curr"]       = "RON",
            ["invoice_id"] = order.Id.ToString(),
            ["ep_id"]      = "EP12345",
            ["merch_id"]   = "TEST_MERCH",
            ["action"]     = "0",
            ["message"]    = "Approved",
            ["approval"]   = "123456",
            ["timestamp"]  = "20260521120000",
            ["nonce"]      = "abcd1234abcd1234abcd1234abcd1234",
        };
        var ipnOrder = new[] { "amount", "curr", "invoice_id", "ep_id", "merch_id", "action", "message", "approval", "timestamp", "nonce" };
        fields["fp"] = PhotoPrint.API.Services.EuPlatescService.ComputeHmac(
            "000102030405060708090a0b0c0d0e0f",
            ipnOrder.Select(k => fields[k]).ToArray());

        var content = new FormUrlEncodedContent(fields);
        var response = await _client.PostAsync("/api/webhooks/euplatesc", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("<epayment>", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        var updated = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Paid, updated!.Status);
        Assert.Equal("EP12345", updated.EuPlatescTransactionId);
    }

    [Fact]
    public async Task EuPlatescIpn_InvalidSignature_ReturnsErrorXml()
    {
        var fields = new Dictionary<string, string>
        {
            ["invoice_id"] = Guid.NewGuid().ToString(),
            ["fp"] = "deadbeefdeadbeefdeadbeefdeadbeef",
        };

        var response = await _client.PostAsync(
            "/api/webhooks/euplatesc",
            new FormUrlEncodedContent(fields));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("<epayment>error</epayment>", body);
    }

    [Fact]
    public async Task EuPlatescIpn_AmountMismatch_NoStatusChange()
    {
        var order = await _factory.SeedOrderAsync(totalRon: 50.00m);

        // Build valid signature but with wrong amount
        var wrongAmount = "99.00"; // actual total is 50.00
        var fields = new Dictionary<string, string>
        {
            ["amount"]     = wrongAmount,
            ["curr"]       = "RON",
            ["invoice_id"] = order.Id.ToString(),
            ["ep_id"]      = "EP99",
            ["merch_id"]   = "TEST_MERCH",
            ["action"]     = "0",
            ["message"]    = "Approved",
            ["approval"]   = "999",
            ["timestamp"]  = "20260521130000",
            ["nonce"]      = "ffffffffffffffffffffffffffffffff",
        };
        var ipnOrder = new[] { "amount", "curr", "invoice_id", "ep_id", "merch_id", "action", "message", "approval", "timestamp", "nonce" };
        fields["fp"] = PhotoPrint.API.Services.EuPlatescService.ComputeHmac(
            "000102030405060708090a0b0c0d0e0f",
            ipnOrder.Select(k => fields[k]).ToArray());

        var response = await _client.PostAsync("/api/webhooks/euplatesc", new FormUrlEncodedContent(fields));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        var updated = await db.Orders.FindAsync(order.Id);
        // Status must remain unchanged
        Assert.Equal(OrderStatus.AwaitingPayment, updated!.Status);
    }
}
