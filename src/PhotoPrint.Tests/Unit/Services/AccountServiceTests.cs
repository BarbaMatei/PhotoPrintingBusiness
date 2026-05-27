using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Account;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class AccountServiceTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AccountService CreateSut(PhotoPrintDbContext db) =>
        new(db, new PasswordHasher<User>(), NullLogger<AccountService>.Instance);

    private static async Task<User> SeedUserAsync(
        PhotoPrintDbContext db,
        string password = "Password@1",
        bool hasPassword = true)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            NormalizedEmail = $"USER-{Guid.NewGuid():N}@EXAMPLE.COM",
            FirstName = "Ion",
            LastName = "Popescu",
            Phone = "0712345678",
            IsEmailConfirmed = true,
            GdprConsentAccepted = true,
        };
        if (hasPassword)
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // ── GetAccountAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAccount_ReturnsCorrectDto()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var result = await sut.GetAccountAsync(user.Id);

        result.FirstName.Should().Be("Ion");
        result.LastName.Should().Be("Popescu");
        result.Email.Should().Be(user.Email);
        result.Phone.Should().Be("0712345678");
        result.HasPassword.Should().BeTrue();
        result.LinkedProviders.Should().BeEmpty();
        result.DeletionRequested.Should().BeFalse();
    }

    [Fact]
    public async Task GetAccount_WithLinkedProvider_ReturnsProviders()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id,
            Provider = "Google",
            ProviderKey = "google-123",
        });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var result = await sut.GetAccountAsync(user.Id);

        result.LinkedProviders.Should().ContainSingle().Which.Should().Be("Google");
        result.HasPassword.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccount_WithNullPassword_HasPasswordFalse()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db, hasPassword: false);
        var sut = CreateSut(db);

        var result = await sut.GetAccountAsync(user.Id);

        result.HasPassword.Should().BeFalse();
    }

    [Fact]
    public async Task GetAccount_NonExistentUser_ThrowsNotFoundException()
    {
        var db = CreateDb();
        var sut = CreateSut(db);

        var act = () => sut.GetAccountAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── UpdateAccountAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAccount_UpdatesNameAndPhone()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        await sut.UpdateAccountAsync(user.Id, new UpdateAccountRequest("Andrei", "Ionescu", "0712345679"));

        var updated = await db.Users.FindAsync(user.Id);
        updated!.FirstName.Should().Be("Andrei");
        updated.LastName.Should().Be("Ionescu");
        updated.Phone.Should().Be("0712345679");
    }

    [Fact]
    public async Task UpdateAccount_NonExistentUser_ThrowsNotFoundException()
    {
        var db = CreateDb();
        var sut = CreateSut(db);

        var act = () => sut.UpdateAccountAsync(Guid.NewGuid(), new UpdateAccountRequest("A", "B", null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_UpdatesHash()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db, "OldPass@1");
        var oldHash = user.PasswordHash;
        var sut = CreateSut(db);

        await sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("OldPass@1", "NewPass@2", "NewPass@2"));

        var updated = await db.Users.FindAsync(user.Id);
        updated!.PasswordHash.Should().NotBe(oldHash);
    }

    [Fact]
    public async Task ChangePassword_RevokesAllActiveRefreshTokens()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db, "OldPass@1");
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "hash1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "hash2",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("OldPass@1", "NewPass@2", "NewPass@2"));

        var tokens = await db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        tokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ThrowsBadRequest()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db, "OldPass@1");
        var sut = CreateSut(db);

        var act = () => sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("WrongPass@1", "NewPass@2", "NewPass@2"));

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*incorectă*");
    }

    [Fact]
    public async Task ChangePassword_NoPassword_ThrowsBadRequest()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db, hasPassword: false);
        var sut = CreateSut(db);

        var act = () => sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("any", "NewPass@2", "NewPass@2"));

        await act.Should().ThrowAsync<BadRequestException>();
    }

    // ── RequestDeletionAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RequestDeletion_SetsDeletionRequestedAt()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);
        var before = DateTimeOffset.UtcNow;

        await sut.RequestDeletionAsync(user.Id);

        var updated = await db.Users.FindAsync(user.Id);
        updated!.DeletionRequestedAt.Should().NotBeNull();
        updated.DeletionRequestedAt.Should().BeOnOrAfter(before);
    }

    // ── Address CRUD ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAddresses_ReturnsUserAddresses()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        db.SavedAddresses.Add(new SavedAddress
        {
            UserId = user.Id, Label = "Acasă", FullName = "Ion Popescu",
            Phone = "0712345678", AddressLine = "Str. Exemplu 1",
            City = "București", County = "Ilfov", PostalCode = "010000",
        });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var result = await sut.GetAddressesAsync(user.Id);

        result.Should().HaveCount(1);
        result[0].Label.Should().Be("Acasă");
    }

    [Fact]
    public async Task AddAddress_PersistsAddress()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);
        var request = new SavedAddressRequest("Birou", "Ion Popescu", "0712345678",
            "Str. Muncii 5", "Cluj-Napoca", "Cluj", "400000", false);

        var result = await sut.AddAddressAsync(user.Id, request);

        result.Label.Should().Be("Birou");
        result.City.Should().Be("Cluj-Napoca");
        (await db.SavedAddresses.CountAsync(sa => sa.UserId == user.Id)).Should().Be(1);
    }

    [Fact]
    public async Task AddAddress_OverLimit_ThrowsConflict()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        for (var i = 0; i < 5; i++)
        {
            db.SavedAddresses.Add(new SavedAddress
            {
                UserId = user.Id, Label = $"Adresă {i}", FullName = "Test",
                Phone = "0712345678", AddressLine = $"Str. {i}", City = "Buc",
                County = "IF", PostalCode = "010000",
            });
        }
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var act = () => sut.AddAddressAsync(user.Id,
            new SavedAddressRequest("Extra", "Test", "0712345678", "Str. 6", "Buc", "IF", "010000", false));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AddAddress_WithDefault_ClearsExistingDefault()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        var existing = new SavedAddress
        {
            UserId = user.Id, Label = "Acasă", FullName = "Ion",
            Phone = "0712345678", AddressLine = "Str. 1", City = "Buc",
            County = "IF", PostalCode = "010000", IsDefault = true,
        };
        db.SavedAddresses.Add(existing);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.AddAddressAsync(user.Id,
            new SavedAddressRequest("Birou", "Ion", "0712345678", "Str. 2", "Buc", "IF", "010000", true));

        var addresses = await db.SavedAddresses.Where(sa => sa.UserId == user.Id).ToListAsync();
        addresses.Where(a => a.IsDefault).Should().HaveCount(1);
        addresses.Single(a => a.IsDefault).Label.Should().Be("Birou");
    }

    [Fact]
    public async Task UpdateAddress_UpdatesFields()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        var address = new SavedAddress
        {
            UserId = user.Id, Label = "Vechi", FullName = "Ion",
            Phone = "0712345678", AddressLine = "Str. 1", City = "Buc",
            County = "IF", PostalCode = "010000",
        };
        db.SavedAddresses.Add(address);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var result = await sut.UpdateAddressAsync(user.Id, address.Id,
            new SavedAddressRequest("Nou", "Andrei", "0712345679", "Str. 2", "Cluj", "CJ", "400000", false));

        result.Label.Should().Be("Nou");
        result.City.Should().Be("Cluj");
    }

    [Fact]
    public async Task UpdateAddress_OtherUsersAddress_ThrowsNotFound()
    {
        var db = CreateDb();
        var user1 = await SeedUserAsync(db);
        var user2 = await SeedUserAsync(db);
        var address = new SavedAddress
        {
            UserId = user1.Id, Label = "Acasă", FullName = "Ion",
            Phone = "0712345678", AddressLine = "Str. 1", City = "Buc",
            County = "IF", PostalCode = "010000",
        };
        db.SavedAddresses.Add(address);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var act = () => sut.UpdateAddressAsync(user2.Id, address.Id,
            new SavedAddressRequest("Hack", "Hacker", "0712345678", "Str.", "Buc", "IF", "010000", false));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAddress_RemovesAddress()
    {
        var db = CreateDb();
        var user = await SeedUserAsync(db);
        var address = new SavedAddress
        {
            UserId = user.Id, Label = "De șters", FullName = "Ion",
            Phone = "0712345678", AddressLine = "Str. 1", City = "Buc",
            County = "IF", PostalCode = "010000",
        };
        db.SavedAddresses.Add(address);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.DeleteAddressAsync(user.Id, address.Id);

        (await db.SavedAddresses.CountAsync(sa => sa.UserId == user.Id)).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAddress_OtherUsersAddress_ThrowsNotFound()
    {
        var db = CreateDb();
        var user1 = await SeedUserAsync(db);
        var user2 = await SeedUserAsync(db);
        var address = new SavedAddress
        {
            UserId = user1.Id, Label = "Privat", FullName = "Ion",
            Phone = "0712345678", AddressLine = "Str. 1", City = "Buc",
            County = "IF", PostalCode = "010000",
        };
        db.SavedAddresses.Add(address);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var act = () => sut.DeleteAddressAsync(user2.Id, address.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
