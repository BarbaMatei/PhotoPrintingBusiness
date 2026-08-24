using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// The cross-tenant idempotency-key collision must surface as a
/// 409 at the HTTP layer, but the default integration stack uses EF InMemory, which does
/// not enforce the unique index. This class runs against a real PostgreSQL database
/// (<see cref="PostgresPaymentFactory"/>) so the global unique index actually rejects the
/// second tenant's INSERT and the controller maps it to 409 — the production-realistic
/// behavior the InMemory test can't reach.
/// </summary>
public class PaymentIdempotencyRelationalTests : IClassFixture<PostgresPaymentFactory>
{
    private readonly PostgresPaymentFactory _factory;

    // Courier delivery avoids the Order → EasyboxLocker FK (PostgreSQL enforces it), so the
    // only constraint the second tenant's INSERT can violate is the unique idempotency index.
    private static readonly CreateOrderRequest CourierStripeRequest = new(
        DeliveryType: DeliveryType.Courier,
        EasyboxLockerId: null,
        ShippingAddress: new ShippingAddressSnapshot
        {
            Street = "Str. Test", Number = "1", City = "București",
            County = "Ilfov", PostalCode = "010101", RecipientName = "Test", Phone = "0700000000",
        });

    public PaymentIdempotencyRelationalTests(PostgresPaymentFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateStripeIntent_SecondTenantReusesAnothersKey_Returns409_OnRealUniqueIndex()
    {
        var key = Guid.NewGuid().ToString();

        // Tenant A claims the key.
        var (userA, tokenA) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userA, unitPrice: 2.00m, quantity: 3);
        var respA = await SendStripeIntent(tokenA, key);
        Assert.Equal(HttpStatusCode.OK, respA.StatusCode);
        var dtoA = await respA.Content.ReadFromJsonAsync<StripeIntentResponse>();

        // Tenant B presents the SAME key. Owner-scoped resolution finds nothing for B,
        // so B's INSERT hits the global unique index → caught → clean 409 (never a 500,
        // never A's order).
        var (userB, tokenB) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCartItemAsync(userId: userB, unitPrice: 2.00m, quantity: 3);
        var respB = await SendStripeIntent(tokenB, key);

        Assert.Equal(HttpStatusCode.Conflict, respB.StatusCode);

        // Exactly one order carries the key, and it is A's — B created nothing.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrint.API.Data.PhotoPrintDbContext>();
        Assert.Equal(1, db.Orders.Count(o => o.IdempotencyKey == key));
        var keyed = db.Orders.Single(o => o.IdempotencyKey == key);
        Assert.Equal(dtoA!.OrderId, keyed.Id);
        Assert.Equal(userA, keyed.UserId);
        Assert.NotEqual(userB, keyed.UserId);
    }

    private Task<HttpResponseMessage> SendStripeIntent(string bearerToken, string idempotencyKey)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        // Request building centralized in PaymentRequestHelpers.
        return client.PostStripeIntentAsync(CourierStripeRequest, idempotencyKey);
    }
}
