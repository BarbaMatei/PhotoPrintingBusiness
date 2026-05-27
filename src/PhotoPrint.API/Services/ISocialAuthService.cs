using PhotoPrint.API.DTOs.Auth;

namespace PhotoPrint.API.Services;

public interface ISocialAuthService
{
    Task<GoogleLoginResponse> GoogleSignInAsync(
        string idToken,
        string ipAddress,
        HttpResponse httpResponse,
        CancellationToken ct = default);
}
