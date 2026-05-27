using System.Security.Cryptography;

namespace PhotoPrint.API.Services;

public class EmailTokenService : IEmailTokenService
{
    public (string rawToken, string tokenHash) GenerateEmailToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        // URL-safe base64
        raw = raw.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var hash = HashToken(raw);
        return (raw, hash);
    }

    public bool VerifyEmailToken(string rawToken, string storedHash)
    {
        var computedHash = System.Text.Encoding.UTF8.GetBytes(HashToken(rawToken));
        var storedBytes = System.Text.Encoding.UTF8.GetBytes(storedHash);
        return CryptographicOperations.FixedTimeEquals(computedHash, storedBytes);
    }

    private static string HashToken(string raw)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
