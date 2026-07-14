using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PhotoPrint.API.DTOs.Orders;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Integration;

public class OrdersControllerIntegrationTests : IClassFixture<OrdersFactory>
{
    private readonly OrdersFactory _factory;
    private readonly HttpClient _anonClient;

    public OrdersControllerIntegrationTests(OrdersFactory factory)
    {
        _factory = factory;
        _anonClient = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private HttpClient AuthClient(string token)
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── GET /api/orders ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrders_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_NoOrders_Returns200WithEmptyList()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        var response = await AuthClient(token).GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("total").GetInt32());
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetOrders_WithOrders_ReturnsOrderList()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedOrderAsync(userId);
        await _factory.SeedOrderAsync(userId);

        var response = await AuthClient(token).GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("total").GetInt32());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        Assert.True(response.Headers.Contains("X-Total-Count"));
        Assert.Equal("2", response.Headers.GetValues("X-Total-Count").First());
    }

    [Fact]
    public async Task GetOrders_OnlyReturnsOwnOrders()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var (otherUserId, _) = await _factory.SeedUserWithJwtAsync();

        await _factory.SeedOrderAsync(userId);
        await _factory.SeedOrderAsync(otherUserId);

        var response = await AuthClient(token).GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task GetOrders_InvalidPage_Returns400()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        var response = await AuthClient(token).GetAsync("/api/orders?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_PageSizeTooLarge_Returns400()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        var response = await AuthClient(token).GetAsync("/api/orders?pageSize=51");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /api/orders/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderDetail_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderDetail_OwnOrder_Returns200WithDetail()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(userId);

        var response = await AuthClient(token).GetAsync($"/api/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        Assert.NotNull(dto);
        Assert.Equal(order.Id, dto!.Id);
        Assert.Equal(order.OrderNumber, dto.OrderNumber);
        Assert.Equal("Paid", dto.Status);
        Assert.Single(dto.Items);
        Assert.Equal("10x15 Test", dto.Items[0].ProductName);
    }

    [Fact]
    public async Task GetOrderDetail_OtherUsersOrder_Returns403()
    {
        var (ownerUserId, _) = await _factory.SeedUserWithJwtAsync();
        var (_, attackerToken) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(ownerUserId);

        var response = await AuthClient(attackerToken).GetAsync($"/api/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderDetail_UnknownId_Returns404()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        var response = await AuthClient(token).GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderDetail_EasyboxOrder_HasLockerName()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(userId, deliveryType: DeliveryType.Easybox);

        var response = await AuthClient(token).GetAsync($"/api/orders/{order.Id}");
        var dto = await response.Content.ReadFromJsonAsync<OrderDetailDto>();

        Assert.Equal("Easybox", dto!.DeliveryType);
        Assert.NotNull(dto.LockerName);
        Assert.Null(dto.ShippingAddress);
    }

    // ── GET /api/orders/{id}/photos (bolt 053) ────────────────────────────────

    [Fact]
    public async Task GetOrderPhotos_OwnOrder_Returns200WithPrivateNoStore()
    {
        // F11 (review 043-v1): the payload carries per-user presigned URLs, so the response
        // must be Cache-Control: private, no-store (matching the preview endpoint), never
        // shared-cacheable. F15: this also pins the owner → 200 auth path.
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(userId);

        var response = await AuthClient(token).GetAsync($"/api/orders/{order.Id}/photos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.Private);
        Assert.True(response.Headers.CacheControl.NoStore);
    }

    [Fact]
    public async Task GetOrderPhotos_Unauthenticated_Returns401()
    {
        // F15 (review 043-v1): the HTTP auth pipeline was untested — dropping [Authorize]
        // would redden nothing. This pins no-Bearer → 401.
        var response = await _anonClient.GetAsync($"/api/orders/{Guid.NewGuid()}/photos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderPhotos_OtherUsersOrder_Returns403()
    {
        // F15: cross-user access must be forbidden (the service's UserId==userId gate, wired
        // through the endpoint).
        var (ownerUserId, _) = await _factory.SeedUserWithJwtAsync();
        var (_, attackerToken) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(ownerUserId);

        var response = await AuthClient(attackerToken).GetAsync($"/api/orders/{order.Id}/photos");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderPhotos_UnknownId_Returns404()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        var response = await AuthClient(token).GetAsync($"/api/orders/{Guid.NewGuid()}/photos");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderPhotos_GuestTokenOnly_Returns401()
    {
        // F12 (review 043-v1): the endpoint is intentionally user-only (no DualAuth policy),
        // so a guest-token-only request is rejected — guests cannot reach order-history photos.
        // This pins the owner's "keep user-only" decision.
        var guestClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        guestClient.DefaultRequestHeaders.Add("X-Guest-Token", Guid.NewGuid().ToString());

        var response = await guestClient.GetAsync($"/api/orders/{Guid.NewGuid()}/photos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderDetail_CourierOrder_HasShippingAddress()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var order = await _factory.SeedOrderAsync(userId, deliveryType: DeliveryType.Courier);

        var response = await AuthClient(token).GetAsync($"/api/orders/{order.Id}");
        var dto = await response.Content.ReadFromJsonAsync<OrderDetailDto>();

        Assert.Equal("Courier", dto!.DeliveryType);
        Assert.NotNull(dto.ShippingAddress);
        Assert.Null(dto.LockerId);
        Assert.Equal("Cluj-Napoca", dto.ShippingAddress!.City);
    }
}
