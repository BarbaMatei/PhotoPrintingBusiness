using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Singleton in-process token cache. The <see cref="SemaphoreSlim"/>
/// makes the "many simultaneous first calls" case fetch exactly one token
/// across the host. A 60 s safety window in front of the Sameday-supplied
/// <c>expire_at_utc</c> absorbs clock skew and in-flight latency.
/// </summary>
public sealed class SamedayTokenProvider : ISamedayTokenProvider, IDisposable
{
    private readonly ISamedayAuthenticator _authenticator;
    private readonly SamedaySettings _settings;
    private readonly ILogger<SamedayTokenProvider> _logger;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private SamedayToken? _current;

    public SamedayTokenProvider(
        ISamedayAuthenticator authenticator,
        IOptions<SamedaySettings> settings,
        ILogger<SamedayTokenProvider> logger,
        TimeProvider clock)
    {
        _authenticator = authenticator;
        _settings = settings.Value;
        _logger = logger;
        _clock = clock;
    }

    public async Task<SamedayToken> GetTokenAsync(CancellationToken ct = default)
    {
        // Fast path: cached and not yet inside the safety window.
        var snapshot = _current;
        if (snapshot is not null && snapshot.IsValid(_clock.GetUtcNow()))
            return snapshot;

        await _gate.WaitAsync(ct);
        try
        {
            // Re-check under the gate — another caller may have refreshed.
            snapshot = _current;
            if (snapshot is not null && snapshot.IsValid(_clock.GetUtcNow()))
                return snapshot;

            var credentials = new SamedayCredentials(_settings.Username, _settings.Password);
            var fresh = await _authenticator.AuthenticateAsync(credentials, ct);
            _current = fresh;

            _logger.LogInformation(
                "Sameday token refreshed. ExpiresAt={ExpiresAt:o}",
                fresh.ExpiresAt);

            return fresh;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() => _current = null;

    public void Dispose() => _gate.Dispose();
}
