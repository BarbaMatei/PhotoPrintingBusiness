using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, string ipAddress, HttpResponse httpResponse, CancellationToken cancellationToken = default);
    Task<LoginResponse> RefreshTokenAsync(string rawRefreshToken, string ipAddress, HttpResponse httpResponse, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string rawRefreshToken, CancellationToken cancellationToken = default);
    Task ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
    Task ResendConfirmationAsync(string email, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken = default);
}
