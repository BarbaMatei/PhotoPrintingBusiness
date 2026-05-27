using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.DTOs.Products;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

public class AdminProductCatalogIntegrationTests : IAsyncLifetime
{
    private ProductCatalogFactory _factory = null!;
    private HttpClient _anonClient = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _customerClient = null!;

    private static readonly Guid SeededProductId   = new("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid SeededSize10x15Id = new("b1000000-0000-0000-0000-000000000001");

    public async Task InitializeAsync()
    {
        _factory = new ProductCatalogFactory();
        await _factory.SeedAsync();

        _anonClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        _adminClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        _customerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _customerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken("Customer"));
    }

    public async Task DisposeAsync()
    {
        _anonClient.Dispose();
        _adminClient.Dispose();
        _customerClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_NoAuth_Returns401()
    {
        var response = await _anonClient.PostAsJsonAsync("/api/admin/products", ValidCreateRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_CustomerRole_Returns403()
    {
        var response = await _customerClient.PostAsJsonAsync("/api/admin/products", ValidCreateRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/admin/products ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_Valid_Returns201WithProduct()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/admin/products", ValidCreateRequest("Test Produs"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Test Produs");
        product.Sizes.Should().HaveCount(1);
        product.Finishes.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateProduct_EmptySizes_Returns422()
    {
        var request = new { name = "No Sizes", productType = "PhotoPrint", sortOrder = 0, sizes = Array.Empty<object>() };
        var response = await _adminClient.PostAsJsonAsync("/api/admin/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateProduct_MissingName_Returns422()
    {
        var request = new { name = "", productType = "PhotoPrint", sortOrder = 0, sizes = new[] { new { label = "10×15", widthMm = 100, heightMm = 150 } } };
        var response = await _adminClient.PostAsJsonAsync("/api/admin/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── PUT /api/admin/products/{id} ──────────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_Valid_Returns200()
    {
        var request = new UpdateProductRequest { Name = "Poze color", ProductType = "PhotoPrint", SortOrder = 1 };
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/products/{SeededProductId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product!.Name.Should().Be("Poze color");
    }

    [Fact]
    public async Task UpdateProduct_UnknownId_Returns404()
    {
        var request = new UpdateProductRequest { Name = "X", ProductType = "PhotoPrint", SortOrder = 0 };
        var response = await _adminClient.PutAsJsonAsync($"/api/admin/products/{Guid.NewGuid()}", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/admin/products/{id}/status ─────────────────────────────────

    [Fact]
    public async Task SetProductStatus_Valid_Returns200()
    {
        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/admin/products/{SeededProductId}/status", new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetProductStatus_UnknownId_Returns404()
    {
        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/admin/products/{Guid.NewGuid()}/status", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/admin/products/{id} ──────────────────────────────────────

    [Fact]
    public async Task DeleteProduct_Valid_Returns204()
    {
        // Create a product to delete (don't delete the shared seeded one)
        var created = await CreateProductAsync("Para Stergere");
        var response = await _adminClient.DeleteAsync($"/api/admin/products/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_UnknownId_Returns404()
    {
        var response = await _adminClient.DeleteAsync($"/api/admin/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/admin/products/{id}/sizes ───────────────────────────────────

    [Fact]
    public async Task AddSize_Valid_Returns201()
    {
        var created = await CreateProductAsync("Produs Size Test");
        var request = new { label = "A5", widthMm = 148, heightMm = 210 };
        var response = await _adminClient.PostAsJsonAsync($"/api/admin/products/{created.Id}/sizes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var size = await response.Content.ReadFromJsonAsync<ProductSizeDto>();
        size!.Label.Should().Be("A5");
    }

    [Fact]
    public async Task AddSize_DuplicateLabel_Returns409()
    {
        var request = new { label = "10×15", widthMm = 100, heightMm = 150 };
        var response = await _adminClient.PostAsJsonAsync($"/api/admin/products/{SeededProductId}/sizes", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddSize_UnknownProduct_Returns404()
    {
        var request = new { label = "A5", widthMm = 148, heightMm = 210 };
        var response = await _adminClient.PostAsJsonAsync($"/api/admin/products/{Guid.NewGuid()}/sizes", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/admin/products/{id}/sizes/{sizeId}/status ─────────────────

    [Fact]
    public async Task SetSizeStatus_ActivateWithoutTiers_Returns422()
    {
        // Seeded sizes have no tiers initially in InMemory — wait, actually they DO have tiers from seed
        // Create a fresh product + size with no tiers
        var product = await CreateProductAsync("Size Status Test");
        var sizeId = product.Sizes[0].Id;

        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/admin/products/{product.Id}/sizes/{sizeId}/status", new { isActive = true });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SetSizeStatus_DeactivateAlwaysSucceeds()
    {
        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/admin/products/{SeededProductId}/sizes/{SeededSize10x15Id}/status",
            new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── PUT /api/admin/products/{id}/sizes/{sizeId}/pricing ───────────────────

    [Fact]
    public async Task ReplaceTiers_ValidTiers_Returns200WithUpdatedSize()
    {
        var request = new
        {
            tiers = new[]
            {
                new { minQuantity = 1, maxQuantity = (int?)9,   unitPrice = 1.50m },
                new { minQuantity = 10, maxQuantity = (int?)null, unitPrice = 1.00m },
            }
        };

        var response = await _adminClient.PutAsJsonAsync(
            $"/api/admin/products/{SeededProductId}/sizes/{SeededSize10x15Id}/pricing", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var size = await response.Content.ReadFromJsonAsync<ProductSizeDto>();
        size!.PricingTiers.Should().HaveCount(2);
        size.PricingTiers[0].UnitPrice.Should().Be(1.50m);
    }

    [Fact]
    public async Task ReplaceTiers_GapBetweenTiers_Returns422()
    {
        var request = new
        {
            tiers = new[]
            {
                new { minQuantity = 1,  maxQuantity = (int?)9,    unitPrice = 1.20m },
                new { minQuantity = 11, maxQuantity = (int?)null,  unitPrice = 0.90m }, // gap at 10
            }
        };

        var response = await _adminClient.PutAsJsonAsync(
            $"/api/admin/products/{SeededProductId}/sizes/{SeededSize10x15Id}/pricing", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ReplaceTiers_AscendingPrice_Returns422()
    {
        var request = new
        {
            tiers = new[]
            {
                new { minQuantity = 1,  maxQuantity = (int?)9,    unitPrice = 0.70m },
                new { minQuantity = 10, maxQuantity = (int?)null,  unitPrice = 1.20m }, // higher = invalid
            }
        };

        var response = await _adminClient.PutAsJsonAsync(
            $"/api/admin/products/{SeededProductId}/sizes/{SeededSize10x15Id}/pricing", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ReplaceTiers_EmptyTiers_Returns422()
    {
        var request = new { tiers = Array.Empty<object>() };
        var response = await _adminClient.PutAsJsonAsync(
            $"/api/admin/products/{SeededProductId}/sizes/{SeededSize10x15Id}/pricing", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ReplaceTiers_UnknownSize_Returns404()
    {
        var request = new
        {
            tiers = new[] { new { minQuantity = 1, maxQuantity = (int?)null, unitPrice = 1.00m } }
        };
        var response = await _adminClient.PutAsJsonAsync(
            $"/api/admin/products/{SeededProductId}/sizes/{Guid.NewGuid()}/pricing", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReplaceTiers_ThenActivateSize_Returns200()
    {
        var product = await CreateProductAsync("Activate After Tiers");
        var sizeId = product.Sizes[0].Id;

        // Add tiers
        var tierRequest = new
        {
            tiers = new[] { new { minQuantity = 1, maxQuantity = (int?)null, unitPrice = 1.00m } }
        };
        await _adminClient.PutAsJsonAsync($"/api/admin/products/{product.Id}/sizes/{sizeId}/pricing", tierRequest);

        // Now activate
        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/admin/products/{product.Id}/sizes/{sizeId}/status", new { isActive = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CreateProductRequest ValidCreateRequest(string name = "Test Product") => new()
    {
        Name        = name,
        ProductType = "PhotoPrint",
        SortOrder   = 0,
        Sizes       = [new CreateProductSizeRequest { Label = "10×15", WidthMm = 100, HeightMm = 150 }],
    };

    private async Task<ProductDto> CreateProductAsync(string name)
    {
        var response = await _adminClient.PostAsJsonAsync("/api/admin/products", ValidCreateRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    private static string GenerateToken(string role)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(TestKeys.RsaPrivateKeyPem);

        var creds = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@fototipar.ro"),
            new Claim(ClaimTypes.Role, role),
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
