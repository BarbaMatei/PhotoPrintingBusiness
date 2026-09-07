using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PhotoPrint.API.DTOs.Cart;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public class CartCouponEndpointsIntegrationTests : IClassFixture<CouponFactory>
{
    private readonly CouponFactory _factory;
    private readonly HttpClient _client;

    public CartCouponEndpointsIntegrationTests(CouponFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ApplyCoupon_NoAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/cart/coupon", new ApplyCouponRequest("VARA25"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApplyCoupon_ValidCode_Returns200WithDiscountedPreview()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);
        await _factory.SeedCouponAsync(code: "VARA10", type: CouponType.Fixed, value: 10m);

        var response = await ApplyAsync(token, "VARA10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        dto!.CouponCode.Should().Be("VARA10");
        dto.CouponType.Should().Be("Fixed");
        dto.DiscountRon.Should().Be(10.00m);
        dto.TotalRon.Should().Be(dto.Subtotal - 10.00m);
        (dto.NetTotalRon + dto.VatRon).Should().Be(dto.TotalRon);
    }

    [Fact]
    public async Task ApplyCoupon_LowerCaseInput_MatchesTheStoredCode()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);
        await _factory.SeedCouponAsync(code: "TOAMNA5", value: 5m);

        var response = await ApplyAsync(token, "toamna5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        dto!.CouponCode.Should().Be("TOAMNA5");
    }

    [Fact]
    public async Task ApplyCoupon_UnknownCode_Returns422WithInvalidCouponCode()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);

        var response = await ApplyAsync(token, "NOSUCH1");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ReadErrorCodeAsync(response)).Should().Be(CouponErrorCodes.InvalidCoupon);
    }

    [Fact]
    public async Task ApplyCoupon_InactiveAndExpiredCodes_AreIndistinguishableFromUnknown()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);
        await _factory.SeedCouponAsync(code: "INACTIVE1", isActive: false);
        await _factory.SeedCouponAsync(
            code: "EXPIRED1",
            validFrom: DateTimeOffset.UtcNow.AddDays(-10),
            validUntil: DateTimeOffset.UtcNow.AddDays(-1));

        var inactive = await ApplyAsync(token, "INACTIVE1");
        var expired = await ApplyAsync(token, "EXPIRED1");

        inactive.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        expired.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ReadErrorCodeAsync(inactive)).Should().Be(CouponErrorCodes.InvalidCoupon);
        (await ReadErrorCodeAsync(expired)).Should().Be(CouponErrorCodes.InvalidCoupon);
    }

    [Fact]
    public async Task ApplyCoupon_BelowMinimum_Returns422WithMinSubtotalCode()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);
        await _factory.SeedCouponAsync(code: "BIGONLY1", minSubtotalRon: 10_000m);

        var response = await ApplyAsync(token, "BIGONLY1");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ReadErrorCodeAsync(response)).Should().Be(CouponErrorCodes.MinSubtotalNotMet);
    }

    [Fact]
    public async Task ApplyCoupon_EmptyCart_Returns422WithEmptyCartCode()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        await _factory.SeedCouponAsync(code: "EMPTY1");

        var response = await ApplyAsync(token, "EMPTY1");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ReadErrorCodeAsync(response)).Should().Be(CouponErrorCodes.EmptyCart);
    }

    [Fact]
    public async Task ApplyCoupon_Twice_ReplacesSilently()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);
        await _factory.SeedCouponAsync(code: "FIRST1", value: 5m);
        await _factory.SeedCouponAsync(code: "SECOND1", value: 9m);

        await ApplyAsync(token, "FIRST1");
        var second = await ApplyAsync(token, "SECOND1");

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await second.Content.ReadFromJsonAsync<CartResponseDto>();
        dto!.CouponCode.Should().Be("SECOND1");
        dto.DiscountRon.Should().Be(9.00m);
    }

    [Fact]
    public async Task GetCart_AfterApply_ReportsTheDiscount()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);
        await _factory.SeedCouponAsync(code: "PERSIST1", value: 7m);
        await ApplyAsync(token, "PERSIST1");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        dto!.CouponCode.Should().Be("PERSIST1");
        dto.DiscountRon.Should().Be(7.00m);
    }

    [Fact]
    public async Task ClearCoupon_Returns200WithTheUndiscountedCart()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);
        await _factory.SeedCouponAsync(code: "CLEARME1", value: 7m);
        await ApplyAsync(token, "CLEARME1");

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/cart/coupon");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        dto!.CouponCode.Should().BeNull();
        dto.DiscountRon.Should().Be(0m);
        dto.TotalRon.Should().Be(dto.Subtotal);
    }

    [Fact]
    public async Task ClearCoupon_WhenNoneApplied_IsStillOk()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        await SeedCartAsync(userId);

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/cart/coupon");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApplyCoupon_GuestToken_IsAccepted()
    {
        var guestSessionId = await _factory.SeedGuestTokenAsync();
        await SeedGuestCartAsync(guestSessionId);
        await _factory.SeedCouponAsync(code: "GUEST123", value: 6m);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/coupon");
        request.Headers.Add("X-Guest-Token", guestSessionId.ToString());
        request.Content = JsonContent.Create(new ApplyCouponRequest("GUEST123"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CartResponseDto>();
        dto!.CouponCode.Should().Be("GUEST123");
    }

    private Task<HttpResponseMessage> ApplyAsync(string token, string code)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/coupon");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new ApplyCouponRequest(code));
        return _client.SendAsync(request);
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String
            ? code.GetString()
            : null;
    }

    private async Task SeedCartAsync(Guid userId)
    {
        var product = await _factory.SeedProductAsync(unitPrice: 20.00m);
        var upload = await _factory.SeedUploadAsync(userId: userId);
        await _factory.SeedCartItemAsync(
            product.Id, upload.Id, product.Sizes.First().Id, userId: userId, quantity: 5);
    }

    private async Task SeedGuestCartAsync(Guid guestSessionId)
    {
        var product = await _factory.SeedProductAsync(unitPrice: 20.00m);
        var upload = await _factory.SeedUploadAsync(guestSessionId: guestSessionId);
        await _factory.SeedCartItemAsync(
            product.Id, upload.Id, product.Sizes.First().Id,
            guestSessionId: guestSessionId, quantity: 5);
    }
}
