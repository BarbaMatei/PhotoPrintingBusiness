using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Account;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Integration;

public class AccountControllerIntegrationTests : IAsyncLifetime
{
    private readonly AccountFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── GET /api/account ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAccount_Authenticated_Returns200WithDto()
    {
        var (_, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountDto>();
        body!.FirstName.Should().Be("Test");
        body.Email.Should().NotBeNullOrEmpty();
        body.HasPassword.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccount_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/account");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PATCH /api/account ────────────────────────────────────────────────────

    [Fact]
    public async Task PatchAccount_ValidRequest_Returns200()
    {
        var (_, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        var response = await _client.PatchAsJsonAsync("/api/account", new
        {
            firstName = "Andrei",
            lastName = "Ionescu",
            phone = "0712345679",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PatchAccount_EmptyFirstName_Returns422()
    {
        var (_, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        var response = await _client.PatchAsJsonAsync("/api/account", new
        {
            firstName = "",
            lastName = "Pop",
            phone = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /api/account/change-password ─────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidRequest_Returns200()
    {
        var (_, token) = await _factory.SeedAndLoginAsync("Test@1234!");
        SetBearer(token);

        var response = await _client.PostAsJsonAsync("/api/account/change-password", new
        {
            currentPassword = "Test@1234!",
            newPassword = "NewTest@5678!",
            confirmNewPassword = "NewTest@5678!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        var (_, token) = await _factory.SeedAndLoginAsync("Test@1234!");
        SetBearer(token);

        var response = await _client.PostAsJsonAsync("/api/account/change-password", new
        {
            currentPassword = "WrongPass@1",
            newPassword = "NewTest@5678!",
            confirmNewPassword = "NewTest@5678!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DELETE /api/account ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAccount_Authenticated_Returns200AndSetsDeletionDate()
    {
        var (userId, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        var response = await _client.DeleteAsync("/api/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var user = await db.Users.FindAsync(userId);
        user!.DeletionRequestedAt.Should().NotBeNull();
    }

    // ── GET /api/account/addresses ────────────────────────────────────────────

    [Fact]
    public async Task GetAddresses_Empty_Returns200WithEmptyList()
    {
        var (_, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/account/addresses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<SavedAddressDto>>();
        body.Should().BeEmpty();
    }

    // ── POST /api/account/addresses ───────────────────────────────────────────

    [Fact]
    public async Task PostAddress_ValidRequest_Returns201()
    {
        var (_, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync("/api/account/addresses", new
        {
            label = "Acasă",
            fullName = "Ion Popescu",
            phone = "0712345678",
            addressLine = "Str. Exemplu 1",
            city = "București",
            county = "Ilfov",
            postalCode = "010001",
            isDefault = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SavedAddressDto>();
        body!.Label.Should().Be("Acasă");
        body.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task PostAddress_InvalidPhone_Returns422()
    {
        var (_, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync("/api/account/addresses", new
        {
            label = "Acasă",
            fullName = "Ion",
            phone = "0123",
            addressLine = "Str. 1",
            city = "Buc",
            county = "IF",
            postalCode = "010001",
            isDefault = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostAddress_OverFiveLimit_Returns409()
    {
        var (userId, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
            for (var i = 0; i < 5; i++)
            {
                db.SavedAddresses.Add(new SavedAddress
                {
                    UserId = userId, Label = $"Adresă {i}", FullName = "Test",
                    Phone = "0712345678", AddressLine = $"Str. {i}", City = "Buc",
                    County = "IF", PostalCode = "010000",
                });
            }
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/account/addresses", new
        {
            label = "A 6-a",
            fullName = "Ion",
            phone = "0712345678",
            addressLine = "Str. 6",
            city = "Buc",
            county = "IF",
            postalCode = "010001",
            isDefault = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── DELETE /api/account/addresses/{id} ────────────────────────────────────

    [Fact]
    public async Task DeleteAddress_OwnAddress_Returns204()
    {
        var (userId, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        Guid addressId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
            var address = new SavedAddress
            {
                UserId = userId, Label = "De șters", FullName = "Ion",
                Phone = "0712345678", AddressLine = "Str. 1", City = "Buc",
                County = "IF", PostalCode = "010000",
            };
            db.SavedAddresses.Add(address);
            await db.SaveChangesAsync();
            addressId = address.Id;
        }

        var response = await _client.DeleteAsync($"/api/account/addresses/{addressId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAddress_NotFound_Returns404()
    {
        var (_, token) = await _factory.SeedAndLoginAsync();
        SetBearer(token);

        var response = await _client.DeleteAsync($"/api/account/addresses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
