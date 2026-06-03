namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// Owns the OAuth bearer token used against ANAF SPV. In-process singleton
/// cache per ADR-013 (Sameday's pattern). 60s pre-expiry safety window;
/// thundering-herd gated by a <see cref="SemaphoreSlim"/>.
/// </summary>
public interface IAnafTokenProvider
{
    /// <summary>
    /// Returns a valid bearer token. Refreshes from the OAuth endpoint when
    /// the cached value is missing or within the safety window.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cached token. Called by <c>AnafAuthHandler</c> after
    /// a 401 to force a fresh token on the retry.
    /// </summary>
    void Invalidate();
}
