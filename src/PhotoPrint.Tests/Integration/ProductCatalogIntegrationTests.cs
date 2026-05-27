using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PhotoPrint.API.DTOs.Products;

namespace PhotoPrint.Tests.Integration;

public class ProductCatalogIntegrationTests : IAsyncLifetime
{
    private ProductCatalogFactory _factory = null!;
    private HttpClient _client = null!;

    // Seed GUIDs from ProductCatalogSeed — deterministic, used to test by-ID endpoints
    private static readonly Guid SeededProductId = new("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededSize10x15Id = new("b1000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        _factory = new ProductCatalogFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        await _factory.SeedAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ── GET /api/products ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCatalog_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCatalog_ReturnsSeededProduct()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        products.Should().NotBeNullOrEmpty();
        products![0].Name.Should().Be("Poze foto");
        products[0].ProductType.Should().Be("PhotoPrint");
    }

    [Fact]
    public async Task GetCatalog_ProductHasSixSizes()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        products![0].Sizes.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetCatalog_EachSizeHasThreeTiers()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        products![0].Sizes.Should().AllSatisfy(s =>
            s.PricingTiers.Should().HaveCount(3));
    }

    [Fact]
    public async Task GetCatalog_TiersAreSortedByMinQuantity()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        var tiers = products![0].Sizes[0].PricingTiers;
        tiers.Select(t => t.MinQuantity).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetCatalog_LastTierHasNullMaxQuantity()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        var lastTier = products![0].Sizes[0].PricingTiers.Last();
        lastTier.MaxQuantity.Should().BeNull();
    }

    [Fact]
    public async Task GetCatalog_ProductHasFinishes()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        products![0].Finishes.Should().HaveCount(2);
        products[0].Finishes.Should().Contain("Lucioasă");
        products[0].Finishes.Should().Contain("Mată");
    }

    // ── GET /api/products/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ValidId_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/products/{SeededProductId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsProductWithSizes()
    {
        var product = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{SeededProductId}");

        product.Should().NotBeNull();
        product!.Id.Should().Be(SeededProductId);
        product.Sizes.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/products/{id}/sizes/{sizeId}/price ───────────────────────────

    [Fact]
    public async Task CalculatePrice_ValidQuantityInFirstTier_ReturnsCorrectPrices()
    {
        var response = await _client.GetFromJsonAsync<PriceCalculationResponse>(
            $"/api/products/{SeededProductId}/sizes/{SeededSize10x15Id}/price?quantity=5");

        response.Should().NotBeNull();
        response!.Quantity.Should().Be(5);
        response.UnitPrice.Should().Be(1.20m);
        response.TotalPrice.Should().Be(6.00m);
        response.TierLabel.Should().Be("1-9");
        response.Currency.Should().Be("RON");
    }

    [Fact]
    public async Task CalculatePrice_ValidQuantityInMiddleTier_ReturnsCorrectPrices()
    {
        var response = await _client.GetFromJsonAsync<PriceCalculationResponse>(
            $"/api/products/{SeededProductId}/sizes/{SeededSize10x15Id}/price?quantity=20");

        response!.UnitPrice.Should().Be(0.90m);
        response.TotalPrice.Should().Be(18.00m);
        response.TierLabel.Should().Be("10-49");
    }

    [Fact]
    public async Task CalculatePrice_ValidQuantityInOpenEndedTier_ReturnsCorrectPrices()
    {
        var response = await _client.GetFromJsonAsync<PriceCalculationResponse>(
            $"/api/products/{SeededProductId}/sizes/{SeededSize10x15Id}/price?quantity=100");

        response!.UnitPrice.Should().Be(0.70m);
        response.TotalPrice.Should().Be(70.00m);
        response.TierLabel.Should().Be("50+");
    }

    [Fact]
    public async Task CalculatePrice_SizeLabel_IsReturned()
    {
        var response = await _client.GetFromJsonAsync<PriceCalculationResponse>(
            $"/api/products/{SeededProductId}/sizes/{SeededSize10x15Id}/price?quantity=1");

        response!.SizeLabel.Should().Be("10×15");
        response.SizeId.Should().Be(SeededSize10x15Id);
    }

    [Fact]
    public async Task CalculatePrice_QuantityZero_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            $"/api/products/{SeededProductId}/sizes/{SeededSize10x15Id}/price?quantity=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculatePrice_QuantityTooLarge_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            $"/api/products/{SeededProductId}/sizes/{SeededSize10x15Id}/price?quantity=10000");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculatePrice_UnknownSizeId_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            $"/api/products/{SeededProductId}/sizes/{Guid.NewGuid()}/price?quantity=5");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CalculatePrice_MissingQuantityParam_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            $"/api/products/{SeededProductId}/sizes/{SeededSize10x15Id}/price");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
