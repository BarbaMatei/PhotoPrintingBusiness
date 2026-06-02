using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PhotoPrint.API.Exceptions;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Typed <see cref="HttpClient"/> for the Sameday API. Owns the network
/// chokepoint and performs the JSON-to-domain mapping at the anti-corruption
/// boundary. Configured by <c>IHttpClientFactory</c> with:
///
///   outer:  SamedayAuthHandler        (bearer + 401-retry-once)
///   middle: SamedayResilienceHandler  (5xx/408/429 retry via Polly v8)
///   inner:  HttpClientHandler          (the real socket)
///
/// In bolt 036, only <see cref="AuthenticateAsync"/> is fully implemented;
/// the AWB / label / tracking methods are declared and throw
/// <see cref="NotImplementedException"/>, deferred to bolt 037.
/// </summary>
public sealed class SamedayClient : ISamedayClient
{
    private const string AuthenticatePath = "/api/authenticate";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<SamedayClient> _logger;

    public SamedayClient(HttpClient http, ILogger<SamedayClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<SamedayToken> AuthenticateAsync(SamedayCredentials credentials, CancellationToken ct = default)
    {
        // POST /api/authenticate with the credentials in the body. We deliberately
        // do NOT use Basic auth — the per-vendor docs specify the body shape and
        // it's the only format the recorded fixtures cover.
        using var request = new HttpRequestMessage(HttpMethod.Post, AuthenticatePath)
        {
            Content = JsonContent.Create(new
            {
                user = credentials.Username,
                password = credentials.Password,
                remember_me = false,
            }),
        };

        // SamedayAuthHandler short-circuits this path to skip the bearer attach,
        // so we go through the resilience pipeline only (5xx → retry; 4xx → no retry).
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new SamedayUnreachableException(AuthenticatePath, inner: ex);
        }

        using var _ = response;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new SamedayAuthException(AuthenticatePath);

        if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout)
            throw new SamedayUnreachableException(AuthenticatePath, httpStatus: (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadAsync(response, ct);
            throw new SamedayValidationException(AuthenticatePath, (int)response.StatusCode, body);
        }

        SamedayWireDtos.AuthenticateResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<SamedayWireDtos.AuthenticateResponse>(JsonOptions, ct);
        }
        catch (JsonException ex)
        {
            throw new SamedayProtocolException(AuthenticatePath, "response body was not valid JSON", ex);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Token) || payload.ExpireAtUtc is null)
            throw new SamedayProtocolException(AuthenticatePath, "response was missing 'token' or 'expire_at_utc'");

        return new SamedayToken(payload.Token, payload.ExpireAtUtc.Value);
    }

    public Task<AwbCreationResult> CreateAwbAsync(AwbCreationRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in bolt 037-awb-and-tracking-jobs.");

    public Task<Stream> GetLabelPdfAsync(string awbNumber, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in bolt 037-awb-and-tracking-jobs.");

    public Task<TrackingSnapshot> GetTrackingAsync(string awbNumber, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in bolt 037-awb-and-tracking-jobs.");

    private static async Task<string?> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            return null;
        }
    }
}
