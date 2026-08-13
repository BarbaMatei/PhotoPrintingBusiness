namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// In-process token cache for the Sameday API. Implementations
/// MUST serialize concurrent first-time fetches behind a single mutex and
/// MUST treat tokens as expired during their <c>SamedayToken.DefaultSafetyWindow</c>.
/// </summary>
public interface ISamedayTokenProvider
{
    /// <summary>Returns a non-expired <see cref="SamedayToken"/>, fetching a fresh
    /// one only if the cached token is missing or within its safety window.</summary>
    Task<SamedayToken> GetTokenAsync(CancellationToken ct = default);

    /// <summary>Drops the cached token. Called by <c>SamedayAuthHandler</c> on 401
    /// before re-fetching.</summary>
    void Invalidate();
}
