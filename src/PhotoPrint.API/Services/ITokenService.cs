using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    (string rawToken, string tokenHash) GenerateRefreshToken();
}
