using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PhotoPrint.API.DTOs.Coupons;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public class AdminCouponsIntegrationTests : IClassFixture<CouponFactory>
{
    private readonly CouponFactory _factory;
    private readonly HttpClient _client;

    public AdminCouponsIntegrationTests(CouponFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static CouponCreateRequest ValidCreate(string code = "MARKET25") => new(
        Code: code,
        Type: nameof(CouponType.Percent),
        Value: 25m,
        MinSubtotalRon: 50m,
        ValidFrom: DateTimeOffset.UtcNow.AddDays(-1),
        ValidUntil: DateTimeOffset.UtcNow.AddDays(30),
        MaxRedemptions: 100);

    [Fact]
    public async Task AdminEndpoints_NoAuth_Return401()
    {
        var list = await _client.GetAsync("/api/admin/coupons");
        var create = await _client.PostAsJsonAsync("/api/admin/coupons", ValidCreate());

        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoints_NonAdminUser_Return403()
    {
        var (_, customerToken) = await _factory.SeedUserWithJwtAsync();

        var response = await SendAsync(HttpMethod.Get, "/api/admin/coupons", customerToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCoupon_Valid_Returns201AndStoresItUppercase()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();

        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/coupons", token, ValidCreate("newyear9"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<CouponDto>();
        dto!.Code.Should().Be("NEWYEAR9");
        dto.IsActive.Should().BeTrue();
        dto.RedemptionsCount.Should().Be(0);
        response.Headers.Location?.ToString().Should().Contain(dto.Id.ToString());
    }

    [Fact]
    public async Task CreateCoupon_DuplicateCodeDifferingOnlyInCase_Returns409()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        await _factory.SeedCouponAsync(code: "DUPLIC8");

        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/coupons", token, ValidCreate("duplic8"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadErrorCodeAsync(response)).Should().Be(CouponErrorCodes.DuplicateCode);
    }

    [Fact]
    public async Task CreateCoupon_MalformedCode_Returns422()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();

        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/coupons", token, ValidCreate("no"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateCoupon_PercentValueOf100_IsRejected()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        var giveaway = ValidCreate("FREE100") with { Value = 100m };

        var response = await SendAsync(HttpMethod.Post, "/api/admin/coupons", token, giveaway);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateCoupon_ValidUntilBeforeValidFrom_IsRejected()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        var backwards = ValidCreate("BACKWRD1") with
        {
            ValidFrom = DateTimeOffset.UtcNow.AddDays(10),
            ValidUntil = DateTimeOffset.UtcNow.AddDays(1),
        };

        var response = await SendAsync(HttpMethod.Post, "/api/admin/coupons", token, backwards);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateCoupon_NonCodeFields_Succeeds()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        var coupon = await _factory.SeedCouponAsync(code: "EDITME1", value: 5m);

        var response = await SendAsync(
            HttpMethod.Put, $"/api/admin/coupons/{coupon.Id}", token,
            new CouponUpdateRequest("EDITME1", nameof(CouponType.Fixed), 12m, 0m,
                coupon.ValidFrom, coupon.ValidUntil, 50, true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CouponDto>();
        dto!.Value.Should().Be(12m);
        dto.MaxRedemptions.Should().Be(50);
    }

    [Fact]
    public async Task UpdateCoupon_CodeChangeAfterRedemption_Returns409()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        var coupon = await _factory.SeedCouponAsync(code: "LOCKED1", value: 5m);
        var order = await _factory.SeedBareOrderAsync();
        await _factory.SeedRedemptionAsync(coupon.Id, order.Id);

        var response = await SendAsync(
            HttpMethod.Put, $"/api/admin/coupons/{coupon.Id}", token,
            new CouponUpdateRequest("RENAMED1", nameof(CouponType.Fixed), 5m, 0m,
                coupon.ValidFrom, coupon.ValidUntil, null, true));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadErrorCodeAsync(response))
            .Should().Be(CouponErrorCodes.CodeImmutableAfterRedemption);
        (await _factory.ReadCouponAsync(coupon.Id))!.Code.Should().Be("LOCKED1");
    }

    [Fact]
    public async Task UpdateCoupon_UnknownId_Returns404()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();

        var response = await SendAsync(
            HttpMethod.Put, $"/api/admin/coupons/{Guid.NewGuid()}", token,
            new CouponUpdateRequest("GHOST123", nameof(CouponType.Fixed), 5m, 0m,
                DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), null, true));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCoupon_IsSoft_AndSecondCallReturns409()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        var coupon = await _factory.SeedCouponAsync(code: "SOFTDEL1");

        var first = await SendAsync(HttpMethod.Delete, $"/api/admin/coupons/{coupon.Id}", token);
        var second = await SendAsync(HttpMethod.Delete, $"/api/admin/coupons/{coupon.Id}", token);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadErrorCodeAsync(second)).Should().Be(CouponErrorCodes.CouponAlreadyInactive);

        var stored = await _factory.ReadCouponAsync(coupon.Id);
        stored.Should().NotBeNull();
        stored!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ListCoupons_FiltersByStatus()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        await _factory.SeedCouponAsync(code: "LISTON1", isActive: true);
        await _factory.SeedCouponAsync(code: "LISTOFF1", isActive: false);

        var response = await SendAsync(HttpMethod.Get, "/api/admin/coupons?status=inactive", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var codes = body.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(i => i.GetProperty("code").GetString())
            .ToList();

        codes.Should().Contain("LISTOFF1");
        codes.Should().NotContain("LISTON1");
    }

    [Fact]
    public async Task ListCoupons_ClampsPagingInsteadOfFailing()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        await _factory.SeedCouponAsync(code: "PAGING01");

        var response = await SendAsync(HttpMethod.Get, "/api/admin/coupons?page=0&size=9999", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        body.RootElement.GetProperty("size").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task ListRedemptions_ReturnsTheOrderNumberOfEachUse()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();
        var coupon = await _factory.SeedCouponAsync(code: "STATS001");
        var order = await _factory.SeedBareOrderAsync();
        await _factory.SeedRedemptionAsync(coupon.Id, order.Id, discountRon: 12.50m);

        var response = await SendAsync(
            HttpMethod.Get, $"/api/admin/coupons/{coupon.Id}/redemptions", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("total").GetInt32().Should().Be(1);
        var first = body.RootElement.GetProperty("items").EnumerateArray().Single();
        first.GetProperty("orderNumber").GetString().Should().Be(order.OrderNumber);
        first.GetProperty("discountRon").GetDecimal().Should().Be(12.50m);
    }

    [Fact]
    public async Task ListRedemptions_UnknownCoupon_Returns404()
    {
        var (_, token) = await _factory.SeedAdminWithJwtAsync();

        var response = await SendAsync(
            HttpMethod.Get, $"/api/admin/coupons/{Guid.NewGuid()}/redemptions", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType());
        return _client.SendAsync(request);
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String
            ? code.GetString()
            : null;
    }
}
