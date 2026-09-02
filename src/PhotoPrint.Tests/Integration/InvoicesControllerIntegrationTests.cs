using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Integration;

// Through the real pipeline: the dual-auth policy, the guest scheme and the admin role override
// are the subject — a controller-level test cannot see any of them.
public class InvoicesControllerIntegrationTests : IClassFixture<OrdersFactory>
{
    private readonly OrdersFactory _factory;

    public InvoicesControllerIntegrationTests(OrdersFactory factory) => _factory = factory;

    private HttpClient NewClient() => _factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private HttpClient GuestClient(Guid guestToken)
    {
        var client = NewClient();
        client.DefaultRequestHeaders.Add("X-Guest-Token", guestToken.ToString());
        return client;
    }

    private HttpClient UserClient(string jwt)
    {
        var client = NewClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    [Fact]
    public async Task GetInvoice_Anonymous_Returns401()
    {
        var response = await NewClient().GetAsync($"/api/orders/{Guid.NewGuid()}/invoice");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoice_OwnerWithNoInvoiceYet_Returns404WithARetryAfterMatchingThePoll()
    {
        var (userId, jwt) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(userId);

        var response = await UserClient(jwt).GetAsync($"/api/orders/{order.Id}/invoice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("1800", Assert.Single(response.Headers.GetValues("Retry-After")));
    }

    [Fact]
    public async Task GetInvoice_AnotherCustomersOrder_Returns403()
    {
        var (ownerId, _) = await _factory.SeedUserWithJwtAsync();
        var (_, attackerJwt) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(ownerId);

        var response = await UserClient(attackerJwt).GetAsync($"/api/orders/{order.Id}/invoice");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoice_GuestOwner_ReachesOwnershipNotAForbid()
    {
        var guestSessionId = await _factory.SeedGuestTokenAsync();
        var order = await _factory.SeedOrderAsync(null, guestSessionId: guestSessionId);

        var response = await GuestClient(guestSessionId).GetAsync($"/api/orders/{order.Id}/invoice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoice_GuestTokenOfAnotherSession_Returns403()
    {
        var ownerSessionId = await _factory.SeedGuestTokenAsync();
        var otherSessionId = await _factory.SeedGuestTokenAsync();
        var order = await _factory.SeedOrderAsync(null, guestSessionId: ownerSessionId);

        var response = await GuestClient(otherSessionId).GetAsync($"/api/orders/{order.Id}/invoice");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // The inspection-week runbook depends on this: an admin reads a customer's fiscal document.
    [Fact]
    public async Task GetInvoice_AdminOnACustomersOrder_IsNotForbidden()
    {
        var (customerId, _) = await _factory.SeedUserWithJwtAsync();
        var (_, adminJwt) = await _factory.SeedAdminWithJwtAsync();
        var order = await _factory.SeedOrderAsync(customerId);

        var response = await UserClient(adminJwt).GetAsync($"/api/orders/{order.Id}/invoice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
