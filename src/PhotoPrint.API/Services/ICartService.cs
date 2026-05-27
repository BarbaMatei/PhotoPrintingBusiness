using PhotoPrint.API.DTOs.Cart;

namespace PhotoPrint.API.Services;

public interface ICartService
{
    Task<CartResponseDto> GetCartAsync(Guid? userId, Guid? guestSessionId, CancellationToken ct = default);

    Task<CartResponseDto> SetCartAsync(Guid? userId, Guid? guestSessionId, CartRequest request, CancellationToken ct = default);

    Task ClearCartAsync(Guid? userId, Guid? guestSessionId, CancellationToken ct = default);

    /// <summary>
    /// Merges the guest session's cart into the authenticated user's cart.
    /// Conflict resolution: user's existing items win; guest uploads are transferred to the user.
    /// </summary>
    Task<CartResponseDto> MergeCartsAsync(Guid userId, Guid guestSessionId, CancellationToken ct = default);
}
