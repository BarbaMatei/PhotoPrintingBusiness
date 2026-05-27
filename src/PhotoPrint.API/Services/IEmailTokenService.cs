namespace PhotoPrint.API.Services;

public interface IEmailTokenService
{
    (string rawToken, string tokenHash) GenerateEmailToken();
    bool VerifyEmailToken(string rawToken, string storedHash);
}
