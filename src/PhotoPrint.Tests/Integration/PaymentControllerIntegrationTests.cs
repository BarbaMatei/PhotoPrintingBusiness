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
        DeliveryType: DeliveryType.Easybox,
        EasyboxLockerId: Guid.NewGuid(),
        ShippingAddress: new PhotoPrint.API.Models.ShippingAddressSnapshot
        {
            RecipientName = "Test", Phone = "0700000000",
            Street = "Str. Test", Number = "1", City = "Cluj-Napoca",
            County = "Cluj", PostalCode = "400100",
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
        // Stripe is keyed by the order id (stable per order), not the client key.
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

        // B must NOT be handed A's order — that order (and its live secret) is A's.
        Assert.NotEqual(dtoA!.OrderId, dtoB!.OrderId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        var orderA = await db.Orders.FindAsync(dtoA.OrderId);
        var orderB = await db.Orders.FindAsync(dtoB.OrderId);
        Assert.Equal(userA, orderA!.UserId);
        Assert.Equal(userB, orderB!.UserId); // B received only B's own order
    }

    [Fact]
    public async Task CreateStripeIntent_SameKey_DivergentLocker_Returns409()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userId, unitPrice: 2.00m, quantity: 5);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var key = Guid.NewGuid().ToString();

        var first = await SendStripeIntent(client, ValidRequest, key);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Same key, different locker → conflict.
        var divergent = ValidRequest with { EasyboxLockerId = Guid.NewGuid() };
        var second = await SendStripeIntent(client, divergent, key);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        // The 409 body must NAME the divergent field, not just carry the status.
        using var body = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var divergentFields = body.RootElement.GetProperty("divergentFields")
            .EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("easyboxLockerId", divergentFields);
    }

    [Fact]
    public async Task CreateStripeIntent_OverLengthIdempotencyKey_Returns400()
    {
        // A key longer than the documented 80-char ceiling must be rejected at the
        // filter with a 400, not accepted silently or 500'd by a Postgres truncation error.
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
    public async Task CreateStripeIntent_ReplayWithNullCachedSecret_RecoversByRecallingGateway()
    {
        // An earlier attempt created the order but died before
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

    // Request building centralized in PaymentRequestHelpers.
    private static Task<HttpResponseMessage> SendStripeIntent(
        HttpClient client, CreateOrderRequest body, string idempotencyKey)
        => client.PostStripeIntentAsync(body, idempotencyKey);

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
        // is registered on the API). DeliveryType.Easybox = 0.
        var tamperedJson = $$"""
            {
              "deliveryType": 0,
              "easyboxLockerId": "{{Guid.NewGuid()}}",
              "shippingAddress": { "recipientName": "Test", "phone": "0700000000", "street": "Str. Test", "number": "1", "city": "Cluj-Napoca", "county": "Cluj", "postalCode": "400100" },
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
    public async Task StripeWebhook_PaymentSucceeded_EnqueuesPhotoPromotion()
    {
        // The webhook→promotion wiring had no test — deleting the
        // EnqueueAsync call shipped green while paid orders silently never promoted to cloud.
        var order = await _factory.SeedOrderAsync(paymentIntentId: "pi_wh_promo", totalRon: 30.00m);

        var eventJson = $$"""
            {
              "id": "evt_test_promo",
              "object": "event",
              "type": "payment_intent.succeeded",
              "data": {
                "object": {
                  "id": "pi_wh_promo",
                  "object": "payment_intent",
                  "amount": 3000,
                  "currency": "ron",
                  "client_secret": "pi_wh_promo_secret"
                }
              }
            }
            """;

        _factory.StripeVerifier.ShouldThrow = false;

        var response = await _client.PostAsync(
            "/api/webhooks/stripe",
            new StringContent(eventJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(order.Id, _factory.PhotoPromoter.Enqueued);
    }

    [Fact]
    public async Task StripeWebhook_PaymentSucceeded_EnqueuesAwbCreation()
    {
        // The webhook→AWB wiring had no test — deleting the NotifyPaidAsync call shipped
        // green while paid orders silently never got a shipping label.
        var order = await _factory.SeedOrderAsync(paymentIntentId: "pi_wh_awb", totalRon: 30.00m);

        var eventJson = $$"""
            {
              "id": "evt_test_awb",
              "object": "event",
              "type": "payment_intent.succeeded",
              "data": {
                "object": {
                  "id": "pi_wh_awb",
                  "object": "payment_intent",
                  "amount": 3000,
                  "currency": "ron",
                  "client_secret": "pi_wh_awb_secret"
                }
              }
            }
            """;

        _factory.StripeVerifier.ShouldThrow = false;

        var response = await _client.PostAsync(
            "/api/webhooks/stripe",
            new StringContent(eventJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(order.Id, _factory.AwbNotifier.Enqueued);
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

}
