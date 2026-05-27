using PhotoPrint.API.DTOs.Account;

namespace PhotoPrint.API.Services;

public interface IAccountService
{
    Task<AccountDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateAccountAsync(Guid userId, UpdateAccountRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task RequestDeletionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<SavedAddressDto>> GetAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SavedAddressDto> AddAddressAsync(Guid userId, SavedAddressRequest request, CancellationToken cancellationToken = default);
    Task<SavedAddressDto> UpdateAddressAsync(Guid userId, Guid addressId, SavedAddressRequest request, CancellationToken cancellationToken = default);
    Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}
