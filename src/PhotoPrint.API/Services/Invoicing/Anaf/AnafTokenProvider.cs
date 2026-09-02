using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// In-process singleton OAuth token cache for ANAF SPV.
/// Loads a PKCS#12 client cert once at first refresh and attaches it to
/// every token request thereafter. Cert path and password come from the
/// <see cref="AnafSettings"/> options block (env vars in prod).
///
/// Logging: token refresh is logged at Information without the bearer
/// value, expiry, or cert metadata. The token bytes never reach a Serilog
/// sink.
/// </summary>
public sealed class AnafTokenProvider : IAnafTokenProvider, IDisposable
{
    private readonly AnafSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<AnafTokenProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly TimeSpan SafetyWindow = TimeSpan.FromSeconds(60);

    private string? _token;
    private DateTimeOffset _expiresAt;
    private X509Certificate2? _cert;

    public AnafTokenProvider(
        IOptions<AnafSettings> settings,
        TimeProvider clock,
        ILogger<AnafTokenProvider> logger)
    {
        _settings = settings.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        if (_token is not null && _expiresAt > now + SafetyWindow)
            return _token;

        await _gate.WaitAsync(ct);
        try
        {
            now = _clock.GetUtcNow();
            if (_token is not null && _expiresAt > now + SafetyWindow)
                return _token;

            await RefreshAsync(ct);
            return _token!;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate()
    {
        _token = null;
        _expiresAt = default;
    }

    public void Dispose()
    {
        _gate.Dispose();
        _cert?.Dispose();
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        _cert ??= LoadCert();

        using var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };
        handler.ClientCertificates.Add(_cert);

        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(30);

        var endpoint = new Uri(new Uri(_settings.BaseUrl, UriKind.Absolute), "oauth/token");
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("grant_type",    "client_credentials"),
            new KeyValuePair<string,string>("client_id",     _settings.ClientId),
            new KeyValuePair<string,string>("client_secret", _settings.ClientSecret),
        });

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(endpoint, content, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new AnafUnreachableException(endpoint.AbsolutePath, inner: ex);
        }

        using var _ = response;

        if (!response.IsSuccessStatusCode)
        {
            // Don't log body — may carry vendor diagnostic text we shouldn't
            // expose. Status alone is the actionable signal.
            throw new AnafUnreachableException(endpoint.AbsolutePath, httpStatus: (int)response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
            ?? throw new AnafUnreachableException(endpoint.AbsolutePath);

        if (string.IsNullOrWhiteSpace(payload.AccessToken) || payload.ExpiresIn <= 0)
            throw new AnafUnreachableException(endpoint.AbsolutePath);

        _token = payload.AccessToken;
        _expiresAt = _clock.GetUtcNow().AddSeconds(payload.ExpiresIn);

        _logger.LogInformation("anaf.token.refreshed");
    }

    private X509Certificate2 LoadCert()
    {
        try
        {
            // MachineKeySet | PersistKeySet: the cert is loaded from a
            // file owned by the host; we don't ship per-process key
            // containers. Ephemeral would also work but adds platform
            // quirks on Linux.
            return new X509Certificate2(
                _settings.CertPath,
                _settings.CertPassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to load ANAF PKCS#12 client certificate. " +
                "Check Anaf:CertPath / Anaf:CertPassword. " +
                "The file must be readable by the API process.",
                ex);
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
        [JsonPropertyName("expires_in")]   public int    ExpiresIn   { get; init; }
        [JsonPropertyName("token_type")]   public string TokenType   { get; init; } = "Bearer";
    }
}
