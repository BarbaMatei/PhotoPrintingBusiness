using PhotoPrint.API.DTOs.Auth;

namespace PhotoPrint.API.Services;

public interface IGuestSessionService
{
    Task<CreateGuestSessionResponse> CreateAsync(
        CreateGuestSessionRequest request,
        CancellationToken ct = default);

    Task ClaimAsync(
        Guid guestToken,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>Creates an anonymous pre-session (no contact info) for guests who
    /// haven't yet entered their details. Returns the session GUID used as the token.</summary>
    Task<CreateGuestSessionResponse> InitAsync(CancellationToken ct = default);

    /// <summary>Fills in contact info on an existing anonymous guest session.</summary>
    Task UpdateContactAsync(
        Guid sessionId,
        UpdateGuestContactRequest request,
        CancellationToken ct = default);
}
