using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Integration;

// Through the real pipeline: the dual-auth policy and the guest scheme are what is under test.
public class OrderPaymentStatusIntegrationTests : IClassFixture<OrdersFactory>
{
    private readonly OrdersFactory _factory;

    public OrderPaymentStatusIntegrationTests(OrdersFactory factory) => _factory = factory;

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
    public async Task GetPaymentStatus_Anonymous_Returns401()
    {
        var response = await NewClient().GetAsync($"/api/orders/{Guid.NewGuid()}/payment-status");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentStatus_GuestOwnerAwaitingPayment_Returns200WithStatus()
    {
        var guestSessionId = await _factory.SeedGuestTokenAsync();
        var order = await _factory.SeedOrderAsync(
            null, OrderStatus.AwaitingPayment, guestSessionId: guestSessionId);

        var response = await GuestClient(guestSessionId)
            .GetAsync($"/api/orders/{order.Id}/payment-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AwaitingPayment", body.GetProperty("status").GetString());
        Assert.Equal(order.OrderNumber, body.GetProperty("orderNumber").GetString());
        Assert.Equal(order.Id, body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetPaymentStatus_GuestOwnerPaidOrder_ReportsPaid()
    {
        var guestSessionId = await _factory.SeedGuestTokenAsync();
        var order = await _factory.SeedOrderAsync(
            null, OrderStatus.Paid, guestSessionId: guestSessionId);

        var response = await GuestClient(guestSessionId)
            .GetAsync($"/api/orders/{order.Id}/payment-status");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Paid", body.GetProperty("status").GetString());
        Assert.NotNull(body.GetProperty("paidAt").GetString());
    }

    [Fact]
    public async Task GetPaymentStatus_AnotherGuestsOrder_Returns403()
    {
        var ownerSessionId = await _factory.SeedGuestTokenAsync();
        var attackerSessionId = await _factory.SeedGuestTokenAsync();
        var order = await _factory.SeedOrderAsync(
            null, OrderStatus.AwaitingPayment, guestSessionId: ownerSessionId);

        var response = await GuestClient(attackerSessionId)
            .GetAsync($"/api/orders/{order.Id}/payment-status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentStatus_UserOwnOrder_Returns200()
    {
        var (userId, jwt) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(userId, OrderStatus.Paid);

        var response = await UserClient(jwt).GetAsync($"/api/orders/{order.Id}/payment-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentStatus_OtherUsersOrder_Returns403()
    {
        var (ownerId, _) = await _factory.SeedUserWithJwtAsync();
        var (_, attackerJwt) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(ownerId, OrderStatus.Paid);

        var response = await UserClient(attackerJwt)
            .GetAsync($"/api/orders/{order.Id}/payment-status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentStatus_UnknownOrder_Returns404()
    {
        var guestSessionId = await _factory.SeedGuestTokenAsync();

        var response = await GuestClient(guestSessionId)
            .GetAsync($"/api/orders/{Guid.NewGuid()}/payment-status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentStatus_IsNeverCached()
    {
        var guestSessionId = await _factory.SeedGuestTokenAsync();
        var order = await _factory.SeedOrderAsync(
            null, OrderStatus.AwaitingPayment, guestSessionId: guestSessionId);

        var response = await GuestClient(guestSessionId)
            .GetAsync($"/api/orders/{order.Id}/payment-status");

        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl!.NoStore);
        Assert.True(cacheControl.Private);
    }

    [Fact]
    public async Task GetPaymentStatus_GuestTokenAlongsideJwt_StillReadsTheGuestsOwnOrder()
    {
        // Both headers resolve as the guest, so ownership must be granted on the guest branch.
        var guestSessionId = await _factory.SeedGuestTokenAsync();
        var (_, jwt) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(
            null, OrderStatus.AwaitingPayment, guestSessionId: guestSessionId);

        var client = GuestClient(guestSessionId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync($"/api/orders/{order.Id}/payment-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
