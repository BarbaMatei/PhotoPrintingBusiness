using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PhotoPrint.API.DTOs.Cart;
using Xunit;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// Integration tests for GET/POST/DELETE /api/cart and POST /api/cart/merge.
/// Uses <see cref="CartFactory"/> which extends <see cref="UploadFactory"/> with
/// cart-specific seed helpers.
/// </summary>
public class CartControllerIntegrationTests : IClassFixture<CartFactory>
{
    private readonly CartFactory _factory;
    private readonly HttpClient _client;

    public CartControllerIntegrationTests(CartFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Auth enforcement ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetCart_NoAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/cart");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetCart_NoAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/cart",
            new { productId = Guid.NewGuid(), items = Array.Empty<object>() });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MergeCart_GuestToken_Returns401()
    {
        var guestId = await _factory.SeedGuestTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/merge");
        request.Headers.Add("X-Guest-Token", guestId.ToString());
        request.Content = JsonContent.Create(new CartMergeRequest(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        // merge requires JWT, guest token should not be accepted
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/cart ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCart_AuthJwt_EmptyCart_Returns200WithEmptyDto()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        dto.Should().NotBeNull();
        dto!.Groups.Should().BeEmpty();
        dto.Subtotal.Should().Be(0);
        dto.ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCart_GuestToken_Returns200()
    {
        var guestId = await _factory.SeedGuestTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Add("X-Guest-Token", guestId.ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/cart ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetCart_ValidJwtAndOwnUpload_Returns200WithDto()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var product = await _factory.SeedProductAsync();
        var size = product.Sizes.First();
        var upload = await _factory.SeedUploadAsync(userId: userId);

        var body = new CartRequest(product.Id, size.Id, FinishName: null,
            [new CartItemRequest(upload.Id, 2)]);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        dto.Should().NotBeNull();
        dto!.Groups.Should().HaveCount(1);
        dto.Groups[0].ProductId.Should().Be(product.Id);
        dto.Groups[0].Items.Should().HaveCount(1);
        dto.Groups[0].Items[0].UploadId.Should().Be(upload.Id);
        dto.Groups[0].Items[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task SetCart_GuestToken_Returns200()
    {
        var guestId = await _factory.SeedGuestTokenAsync();
        var product = await _factory.SeedProductAsync();
        var size = product.Sizes.First();
        var upload = await _factory.SeedUploadAsync(guestSessionId: guestId);

        var body = new CartRequest(product.Id, size.Id, FinishName: null,
            [new CartItemRequest(upload.Id, 1)]);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-Guest-Token", guestId.ToString());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        dto!.Groups.Should().HaveCount(1);
        dto.Groups[0].Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SetCart_UploadOwnedByDifferentUser_Returns403()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        var (otherUserId, _) = await _factory.SeedUserWithJwtAsync();
        var product = await _factory.SeedProductAsync();
        var size = product.Sizes.First();
        var upload = await _factory.SeedUploadAsync(userId: otherUserId); // wrong owner

        var body = new CartRequest(product.Id, size.Id, FinishName: null,
            [new CartItemRequest(upload.Id, 1)]);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── DELETE /api/cart ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCart_Returns204_ThenGetReturnsEmptyDto()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var product = await _factory.SeedProductAsync();
        var size = product.Sizes.First();
        var upload = await _factory.SeedUploadAsync(userId: userId);

        // First populate cart
        var setRequest = new HttpRequestMessage(HttpMethod.Post, "/api/cart")
        {
            Content = JsonContent.Create(new CartRequest(product.Id, size.Id, FinishName: null,
                [new CartItemRequest(upload.Id, 1)])),
        };
        setRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(setRequest);

        // Now delete
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/cart");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify cart is empty
        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getResponse = await _client.SendAsync(getRequest);
        var dto = await getResponse.Content.ReadFromJsonAsync<CartResponseDto>();
        dto!.Groups.Should().BeEmpty();
    }

    // ── POST /api/cart/merge ──────────────────────────────────────────────────

    [Fact]
    public async Task MergeCart_ValidJwt_GuestItemsAddedToUserCart()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var guestId = await _factory.SeedGuestTokenAsync();
        var product = await _factory.SeedProductAsync();
        var size = product.Sizes.First();
        var guestUpload = await _factory.SeedUploadAsync(guestSessionId: guestId);

        // Seed guest cart item directly
        await _factory.SeedCartItemAsync(product.Id, guestUpload.Id, size.Id, guestSessionId: guestId);

        var body = new CartMergeRequest(guestId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/merge")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        var allItems = dto!.Groups.SelectMany(g => g.Items).ToList();
        allItems.Should().HaveCount(1);
        allItems[0].UploadId.Should().Be(guestUpload.Id);
    }
}
