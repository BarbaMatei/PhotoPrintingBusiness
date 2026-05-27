using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

public class ShippingControllerIntegrationTests : IClassFixture<ShippingFactory>
{
    private readonly ShippingFactory _factory;
    private readonly HttpClient _client;

    public ShippingControllerIntegrationTests(ShippingFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── GET /api/shipping/lockers ─────────────────────────────────────────────

    [Fact]
    public async Task GetLockers_NoFilter_ReturnsAllActive()
    {
        await _factory.SeedLockersAsync("București", 3);
        var response = await _client.GetAsync("/api/shipping/lockers");
        response.EnsureSuccessStatusCode();
        var lockers = await response.Content.ReadFromJsonAsync<List<LockerDto>>();
        Assert.NotNull(lockers);
        Assert.NotEmpty(lockers);
    }

    [Fact]
    public async Task GetLockers_WithCityFilter_ReturnsCityLockers()
    {
        await _factory.SeedLockersAsync("Timișoara", 2);
        var response = await _client.GetAsync("/api/shipping/lockers?city=Timi");
        response.EnsureSuccessStatusCode();
        var lockers = await response.Content.ReadFromJsonAsync<List<LockerDto>>();
        Assert.NotNull(lockers);
        Assert.All(lockers, l => Assert.Contains("Timi", l.City));
    }

    [Fact]
    public async Task GetLockers_NoMatchingCity_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/shipping/lockers?city=Atlantida");
        response.EnsureSuccessStatusCode();
        var lockers = await response.Content.ReadFromJsonAsync<List<LockerDto>>();
        Assert.NotNull(lockers);
        Assert.Empty(lockers);
    }

    [Fact]
    public async Task GetLockers_NoAuth_Returns200()
    {
        // Public endpoint — should work without any auth header
        var response = await _client.GetAsync("/api/shipping/lockers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── GET /api/shipping/cost ────────────────────────────────────────────────

    [Fact]
    public async Task GetCost_Easybox_Returns200WithCostRon()
    {
        var response = await _client.GetAsync("/api/shipping/cost?type=Easybox");
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ShippingCostDto>();
        Assert.NotNull(dto);
        Assert.Equal(20.00m, dto.CostRon);
    }

    [Fact]
    public async Task GetCost_Courier_Returns200WithCostRon()
    {
        var response = await _client.GetAsync("/api/shipping/cost?type=Courier");
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ShippingCostDto>();
        Assert.NotNull(dto);
        Assert.Equal(25.00m, dto.CostRon);
    }

    [Fact]
    public async Task GetCost_InvalidType_Returns400()
    {
        var response = await _client.GetAsync("/api/shipping/cost?type=Telepathy");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCost_NoAuth_Returns200()
    {
        var response = await _client.GetAsync("/api/shipping/cost?type=Easybox");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── POST /api/shipping/awb ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAwb_NoAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/shipping/awb", new { orderId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GenerateAwb_AsAdmin_Returns200WithManualTrue()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateAdminToken());

        var response = await adminClient.PostAsJsonAsync("/api/shipping/awb",
            new { orderId = Guid.NewGuid() });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AwbResultDto>();
        Assert.NotNull(result);
        Assert.True(result.Manual);
    }

    private static string GenerateAdminToken()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(TestKeys.RsaPrivateKeyPem);
        var creds = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "admin@fototipar.ro"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: "fototipar",
            audience: "fototipar-spa",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
