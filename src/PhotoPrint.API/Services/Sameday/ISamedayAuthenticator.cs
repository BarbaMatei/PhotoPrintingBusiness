namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Narrow surface used by <see cref="SamedayTokenProvider"/> to fetch a fresh
/// token without taking a hard dependency on the full <see cref="ISamedayClient"/>.
/// Implemented by <c>SamedayClient.AuthenticateAsync</c>.
/// </summary>
public interface ISamedayAuthenticator
{
    Task<SamedayToken> AuthenticateAsync(SamedayCredentials credentials, CancellationToken ct = default);
}
