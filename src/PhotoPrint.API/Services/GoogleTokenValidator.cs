using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Exceptions;

namespace PhotoPrint.API.Services;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientId;
    private readonly ILogger<GoogleTokenValidator> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public GoogleTokenValidator(
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleAuthSettings> settings,
        ILogger<GoogleTokenValidator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _clientId = settings.Value.ClientId;
        _logger = logger;
    }

    public async Task<GooglePayload> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        HttpResponseMessage response;

        try
        {
            var client = _httpClientFactory.CreateClient("Google");
            response = await client.GetAsync(
                $"tokeninfo?id_token={Uri.EscapeDataString(idToken)}", ct);
        }
        // A timeout keeps a TimeoutException at the base of the chain even once the caller cancels too.
        catch (OperationCanceledException ex)
            when (ct.IsCancellationRequested && ex.GetBaseException() is not TimeoutException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
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
        catch (OperationCanceledException ex)
            when (ct.IsCancellationRequested && ex.GetBaseException() is not TimeoutException)
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
