using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    // ── bolt 035 — payment idempotency ────────────────────────────────────────

    [Fact]
    public async Task CreateStripeIntent_SameIdempotencyKey_ReplaysOneOrderAndOneStripeCall()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 2.00m, quantity: 5);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var key = Guid.NewGuid().ToString();
        var callsBefore = _factory.StripeGateway.CreateCallCount;

        var first = await SendStripeIntent(client, ValidRequest, key);
        var second = await SendStripeIntent(client, ValidRequest, key);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var dto1 = await first.Content.ReadFromJsonAsync<StripeIntentResponse>();
        var dto2 = await second.Content.ReadFromJsonAsync<StripeIntentResponse>();

        // Same order + same secret on replay.
        Assert.Equal(dto1!.OrderId, dto2!.OrderId);
        Assert.Equal(dto1.ClientSecret, dto2.ClientSecret);

        // Stripe was hit exactly once across the two requests.
        Assert.Equal(callsBefore + 1, _factory.StripeGateway.CreateCallCount);
        // BUG-4: Stripe is keyed by the order id (stable per order), not the client key.
        Assert.Equal(dto1.OrderId.ToString(), _factory.StripeGateway.LastIdempotencyKey);

        // Exactly one order persisted for this key.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        var count = db.Orders.Count(o => o.IdempotencyKey == key);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateStripeIntent_SecondTenantReusesAnothersKey_DoesNotReceiveTheirOrder()
    {
        // Tenant A creates an intent under a key.
        var (userA, tokenA) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userA, unitPrice: 2.00m, quantity: 5);
        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

        var key = Guid.NewGuid().ToString();
        var respA = await SendStripeIntent(clientA, ValidRequest, key);
        Assert.Equal(HttpStatusCode.OK, respA.StatusCode);
        var dtoA = await respA.Content.ReadFromJsonAsync<StripeIntentResponse>();

        // Tenant B presents the SAME key (IDOR attempt via header).
        var (userB, tokenB) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userB, unitPrice: 2.00m, quantity: 5);
        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);

        var respB = await SendStripeIntent(clientB, ValidRequest, key);
        Assert.Equal(HttpStatusCode.OK, respB.StatusCode);
        var dtoB = await respB.Content.ReadFromJsonAsync<StripeIntentResponse>();

        // SEC-1: B must NOT be handed A's order — that order (and its live secret) is A's.
        Assert.NotEqual(dtoA!.OrderId, dtoB!.OrderId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        var orderA = await db.Orders.FindAsync(dtoA.OrderId);
        var orderB = await db.Orders.FindAsync(dtoB.OrderId);
        Assert.Equal(userA, orderA!.UserId);
        Assert.Equal(userB, orderB!.UserId); // B received only B's own order
    }

    [Fact]
    public async Task CreateStripeIntent_SameKey_DivergentProcessor_Returns409()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 2.00m, quantity: 5);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var key = Guid.NewGuid().ToString();

        var first = await SendStripeIntent(client, ValidRequest, key);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Same key, different processor → conflict.
        var divergent = ValidRequest with { PaymentProcessor = PaymentProcessor.EuPlatesc };
        var second = await SendStripeIntent(client, divergent, key);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        // OBS-1: the 409 body must NAME the divergent field, not just carry the status.
        using var body = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var divergentFields = body.RootElement.GetProperty("divergentFields")
            .EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("paymentProcessor", divergentFields);
    }

    [Fact]
    public async Task CreateStripeIntent_OverLengthIdempotencyKey_Returns400()
    {
        // SEC-2: a key longer than the documented 80-char ceiling must be rejected at the
        // filter with a 400, not accepted (dev/SQLite) or 500'd (prod Postgres truncation).
        // The cart is seeded so a passing request would otherwise be a 200 — the 400 is
        // attributable to the length check, not an empty cart.
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 2.00m, quantity: 5);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var overLength = new string('k', 81); // ceiling is 80

        var response = await SendStripeIntent(client, ValidRequest, overLength);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InitiateEuPlatesc_SameIdempotencyKey_ReturnsSameUrlAndOneOrder()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 1.50m, quantity: 4);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var key = Guid.NewGuid().ToString();

        var first = await SendEuPlatescInitiate(client, EuPlatescRequest, key);
        var second = await SendEuPlatescInitiate(client, EuPlatescRequest, key);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var dto1 = await first.Content.ReadFromJsonAsync<EuPlatescInitiateResponse>();
        var dto2 = await second.Content.ReadFromJsonAsync<EuPlatescInitiateResponse>();

        Assert.Equal(dto1!.OrderId, dto2!.OrderId);
        // Same redirect URL verbatim (persisted, not rebuilt with a fresh nonce).
        Assert.Equal(dto1.RedirectUrl, dto2.RedirectUrl);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        Assert.Equal(1, db.Orders.Count(o => o.IdempotencyKey == key));
    }

    [Fact]
    public async Task CreateStripeIntent_ReplayWithNullCachedSecret_RecoversByRecallingGateway()
    {
        // OBS-3 (review 035-v5): an earlier attempt created the order but died before
        // persisting the secret. A replay then resolves the same order with a null cached
        // secret and recovers by re-calling the gateway — safe because Stripe is keyed by
        // the stable order id — returning a usable secret without creating a second order.
        // This exercises the previously-unobserved recovery-replay completion path.
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 2.00m, quantity: 5);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var key = Guid.NewGuid().ToString();

        var first = await SendStripeIntent(client, ValidRequest, key);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var dto1 = await first.Content.ReadFromJsonAsync<StripeIntentResponse>();

        // Simulate the crash-before-persist state: the order exists (fresh, non-divergent)
        // but its secret was never saved.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
            var order = await db.Orders.FindAsync(dto1!.OrderId);
            order!.StripeClientSecret = null;
            await db.SaveChangesAsync();
        }

        var callsBefore = _factory.StripeGateway.CreateCallCount;
        var second = await SendStripeIntent(client, ValidRequest, key);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var dto2 = await second.Content.ReadFromJsonAsync<StripeIntentResponse>();
        Assert.Equal(dto1!.OrderId, dto2!.OrderId);            // same order — a replay, not a new one
        Assert.False(string.IsNullOrEmpty(dto2.ClientSecret)); // recovered a usable secret
        Assert.Equal(callsBefore + 1, _factory.StripeGateway.CreateCallCount); // gateway re-called

        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        Assert.Equal(1, vdb.Orders.Count(o => o.IdempotencyKey == key)); // still exactly one order
    }

    // QUAL-4 (review 035-v5): request building centralized in PaymentRequestHelpers.
    private static Task<HttpResponseMessage> SendStripeIntent(
        HttpClient client, CreateOrderRequest body, string idempotencyKey)
        => client.PostStripeIntentAsync(body, idempotencyKey);

    private static Task<HttpResponseMessage> SendEuPlatescInitiate(
        HttpClient client, CreateOrderRequest body, string idempotencyKey)
        => client.PostEuPlatescInitiateAsync(body, idempotencyKey);

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
