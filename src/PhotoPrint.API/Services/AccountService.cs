using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Account;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class AccountService : IAccountService
{
    private const int MaxAddresses = 5;

    private readonly PhotoPrintDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        PhotoPrintDbContext db,
        IPasswordHasher<User> passwordHasher,
        ILogger<AccountService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AccountDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Contul nu a fost găsit.");

        var providers = await _db.ExternalLogins
            .AsNoTracking()
            .Where(el => el.UserId == userId)
            .Select(el => el.Provider)
            .ToListAsync(cancellationToken);

        return new AccountDto(
            user.FirstName,
            user.LastName,
            user.Email,
            user.Phone,
            user.PasswordHash is not null,
            providers,
            user.DeletionRequestedAt.HasValue
        );
    }

    public async Task UpdateAccountAsync(Guid userId, UpdateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Contul nu a fost găsit.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account profile updated for user {UserId}", userId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Contul nu a fost găsit.");

        if (user.PasswordHash is null)
            throw new BadRequestException("Acest cont nu are o parolă configurată.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new BadRequestException("Parola curentă este incorectă.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

        // Revoke all active refresh tokens to force re-login on all devices
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.RevokedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}; {Count} refresh token(s) revoked", userId, activeTokens.Count);
    }

    public async Task RequestDeletionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Contul nu a fost găsit.");

        user.DeletionRequestedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account deletion requested for user {UserId}", userId);
    }

    // ── Addresses ─────────────────────────────────────────────────────────────

    public async Task<List<SavedAddressDto>> GetAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _db.SavedAddresses
            .AsNoTracking()
            .Where(sa => sa.UserId == userId)
            .OrderByDescending(sa => sa.IsDefault)
            .ThenBy(sa => sa.CreatedAt)
            .ToListAsync(cancellationToken);

        return addresses.Select(MapToDto).ToList();
    }

    public async Task<SavedAddressDto> AddAddressAsync(Guid userId, SavedAddressRequest request, CancellationToken cancellationToken = default)
    {
        var count = await _db.SavedAddresses
            .CountAsync(sa => sa.UserId == userId, cancellationToken);

        if (count >= MaxAddresses)
            throw new ConflictException($"Nu poți salva mai mult de {MaxAddresses} adrese.");

        // If new address is default, clear existing default
        if (request.IsDefault)
            await ClearDefaultAsync(userId, cancellationToken);

        var address = new SavedAddress
        {
            UserId = userId,
            Label = request.Label,
            FullName = request.FullName,
            Phone = request.Phone,
            AddressLine = request.AddressLine,
            City = request.City,
            County = request.County,
            PostalCode = request.PostalCode,
            IsDefault = request.IsDefault,
        };

        _db.SavedAddresses.Add(address);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved address {AddressId} added for user {UserId}", address.Id, userId);

        return MapToDto(address);
    }

    public async Task<SavedAddressDto> UpdateAddressAsync(Guid userId, Guid addressId, SavedAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await _db.SavedAddresses
            .FirstOrDefaultAsync(sa => sa.Id == addressId && sa.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Adresa nu a fost găsită.");

        // If updating to default, clear existing default (except this address)
        if (request.IsDefault && !address.IsDefault)
            await ClearDefaultAsync(userId, cancellationToken);

        address.Label = request.Label;
        address.FullName = request.FullName;
        address.Phone = request.Phone;
        address.AddressLine = request.AddressLine;
        address.City = request.City;
        address.County = request.County;
        address.PostalCode = request.PostalCode;
        address.IsDefault = request.IsDefault;
        address.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(address);
    }

    public async Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var address = await _db.SavedAddresses
            .FirstOrDefaultAsync(sa => sa.Id == addressId && sa.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Adresa nu a fost găsită.");

        _db.SavedAddresses.Remove(address);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved address {AddressId} deleted for user {UserId}", addressId, userId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task ClearDefaultAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await _db.SavedAddresses
            .Where(sa => sa.UserId == userId && sa.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var a in existing)
            a.IsDefault = false;
    }

    private static SavedAddressDto MapToDto(SavedAddress address) =>
        new(address.Id, address.Label, address.FullName, address.Phone,
            address.AddressLine, address.City, address.County, address.PostalCode, address.IsDefault);
}
