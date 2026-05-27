using System.Security.Claims;
using PhotoPrint.API.Authentication;

namespace PhotoPrint.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the authenticated user's ID, or null for guest-only requests.</summary>
    public static Guid? GetUserIdOrNull(this ClaimsPrincipal principal)
    {
        // When authenticated as a guest, NameIdentifier is set to the session ID — not a user ID.
        if (principal.HasClaim(c => c.Type == GuestAuthenticationHandler.GuestSessionIdClaimType))
            return null;

        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>Returns the guest session ID, or null for authenticated user requests.</summary>
    public static Guid? GetGuestSessionIdOrNull(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(GuestAuthenticationHandler.GuestSessionIdClaimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
