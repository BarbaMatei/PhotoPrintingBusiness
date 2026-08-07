using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Exceptions;

namespace PhotoPrint.API.Services;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    public static readonly TimeSpan RequestDeadline = TimeSpan.FromSeconds(5);

    // HttpClient cannot say whether its own timeout or the caller fired, so the deadline is owned
    // here and HttpClient.Timeout is only a backstop behind it.
    public static readonly TimeSpan HttpBackstop = RequestDeadline + TimeSpan.FromSeconds(10);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientId;
    private readonly ILogger<GoogleTokenValidator> _logger;
    private readonly TimeSpan _deadline;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public GoogleTokenValidator(
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleAuthSettings> settings,
        ILogger<GoogleTokenValidator> logger,
        TimeSpan? deadline = null)
    {
        _httpClientFactory = httpClientFactory;
        _clientId = settings.Value.ClientId;
        _logger = logger;
        _deadline = deadline ?? RequestDeadline;
    }

    public async Task<GooglePayload> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        HttpResponseMessage response;

        using var deadline = new CancellationTokenSource(_deadline);
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(ct, deadline.Token);

        try
        {
            var client = _httpClientFactory.CreateClient("Google");
            response = await client.GetAsync(
                $"tokeninfo?id_token={Uri.EscapeDataString(idToken)}", attempt.Token);
        }
        // Only the caller leaving is a cancellation; if our deadline also fired, Google really failed.
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested && !deadline.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Google tokeninfo endpoint unreachable");
            throw new BadGatewayException("Serviciu extern indisponibil.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedException("Autentificarea Google a eșuat.");
        }

        GoogleTokenInfoResponse? info;

        try
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            info = JsonSerializer.Deserialize<GoogleTokenInfoResponse>(json, _jsonOptions);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            throw new UnauthorizedException("Autentificarea Google a eșuat.");
        }

        if (info?.Sub is null || info.Email is null)
        {
            throw new UnauthorizedException("Autentificarea Google a eșuat.");
        }

        if (!string.Equals(info.Aud, _clientId, StringComparison.Ordinal))
        {
            throw new UnauthorizedException("Autentificarea Google a eșuat.");
        }

        return new GooglePayload(
            Sub: info.Sub,
            Email: info.Email,
            GivenName: info.GivenName ?? "",
            FamilyName: info.FamilyName ?? "",
            Picture: info.Picture);
    }

    private sealed class GoogleTokenInfoResponse
    {
        public string? Sub { get; set; }
        public string? Aud { get; set; }
        public string? Email { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }

        public string? Picture { get; set; }
    }
}
