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

        await EnsureSuccessOrThrowAsync(response, AuthenticatePath, ct);

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

        // Normalize to UTC: the wire value may deserialize with a non-UTC offset on a non-UTC host,
        // which would shift the cached token's expiry.
        return new SamedayToken(payload.Token, payload.ExpireAtUtc.Value.ToUniversalTime());
    }

    public async Task<AwbCreationResult> CreateAwbAsync(AwbCreationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = new SamedayWireDtos.AwbCreateRequest
        {
            PickupPoint    = request.PickupPointId,
            AwbPayment     = 1,
            Service        = request.ServiceId,
            LockerLastMile = request.LockerSamedayId,
            PackageWeight  = request.ParcelWeightKg,
            CashOnDelivery = request.CodAmountRon,
            Observation    = request.Observations,
            ClientInternalReference = request.OrderNumber, // per-order idempotency key
            AwbRecipient   = new SamedayWireDtos.AwbRecipient
            {
                Name        = request.RecipientName,
                PhoneNumber = request.RecipientPhone,
                Address     = request.RecipientAddress,
                City        = request.RecipientCity,
                County      = request.RecipientCounty,
                PostalCode  = request.RecipientPostalCode,
            },
            Parcels = Enumerable.Range(0, request.ParcelCount)
                .Select(_ => new SamedayWireDtos.AwbParcel { Weight = request.ParcelWeightKg })
                .ToList(),
        };

        var endpoint = "/api/awb";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(body),
        };

        HttpResponseMessage response;
        try { response = await _http.SendAsync(httpRequest, ct); }
        catch (HttpRequestException ex) { throw new SamedayUnreachableException(endpoint, inner: ex); }

        using var _ = response;

        await EnsureSuccessOrThrowAsync(response, endpoint, ct);

        SamedayWireDtos.AwbCreateResponse? payload;
        try { payload = await response.Content.ReadFromJsonAsync<SamedayWireDtos.AwbCreateResponse>(JsonOptions, ct); }
        catch (JsonException ex) { throw new SamedayProtocolException(endpoint, "response body was not valid JSON", ex); }

        if (payload is null
            || string.IsNullOrWhiteSpace(payload.AwbNumber)
            || string.IsNullOrWhiteSpace(payload.PdfLink))
        {
            throw new SamedayProtocolException(
                endpoint, "response was missing 'awbNumber' or 'pdfLink'");
        }

        return new AwbCreationResult(payload.AwbNumber, payload.PdfLink, payload.AwbCost);
    }

    public async Task<Stream> GetLabelPdfAsync(string awbNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(awbNumber))
            throw new ArgumentException("awbNumber is required.", nameof(awbNumber));

        var endpoint = $"/api/awb/{Uri.EscapeDataString(awbNumber)}/label";

        HttpResponseMessage response;
        try { response = await _http.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, ct); }
        catch (HttpRequestException ex) { throw new SamedayUnreachableException(endpoint, inner: ex); }

        // Not `using`-scoped: on success the caller owns the response + stream, so dispose only
        // on the throw paths.
        try
        {
            await EnsureSuccessOrThrowAsync(response, endpoint, ct);
        }
        catch
        {
            response.Dispose();
            throw;
        }

        // Caller owns the stream + the response disposal.
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task<TrackingSnapshot> GetTrackingAsync(string awbNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(awbNumber))
            throw new ArgumentException("awbNumber is required.", nameof(awbNumber));

        var endpoint = $"/api/awb/{Uri.EscapeDataString(awbNumber)}/tracking";

        HttpResponseMessage response;
        try { response = await _http.GetAsync(endpoint, ct); }
        catch (HttpRequestException ex) { throw new SamedayUnreachableException(endpoint, inner: ex); }

        using var _ = response;

        await EnsureSuccessOrThrowAsync(response, endpoint, ct);

        SamedayWireDtos.TrackingResponse? payload;
        try { payload = await response.Content.ReadFromJsonAsync<SamedayWireDtos.TrackingResponse>(JsonOptions, ct); }
        catch (JsonException ex) { throw new SamedayProtocolException(endpoint, "response body was not valid JSON", ex); }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Status))
            throw new SamedayProtocolException(endpoint, "response was missing 'status'");

        var state = MapTrackingState(payload.Status);

        // Null when the vendor gives no timestamp — the caller supplies its poll clock rather than
        // this method fabricating a wall-clock "now" that would land in DeliveredAt.
        var observedAt = payload.ObservedAt ?? payload.DeliveredAt;

        var historyFallback = observedAt ?? DateTimeOffset.UtcNow; // cosmetic history only
        var history = (payload.History ?? Array.Empty<SamedayWireDtos.TrackingHistoryEntry>())
            .Select(h => new TrackingEvent(
                State: MapTrackingState(h.Status ?? string.Empty),
                Description: h.Description ?? string.Empty,
                OccurredAt: h.OccurredAt ?? historyFallback))
            .ToList();

        return new TrackingSnapshot(awbNumber, state, observedAt, history);
    }

    /// <summary>
    /// Vendor status code → normalised <see cref="TrackingState"/>. The
    /// anti-corruption boundary for Sameday's wire vocabulary; every
    /// downstream consumer thinks in <see cref="TrackingState"/>, not the
    /// vendor codes.
    /// </summary>
    private static TrackingState MapTrackingState(string vendorCode)
    {
        return vendorCode.Trim().ToLowerInvariant() switch
        {
            "awb-issued"     or "pickup-pending"
                => TrackingState.Pending,

            "picked-up"      or "in-transit"
            or "arrived-at-sortation" or "out-for-pickup"
                => TrackingState.InTransit,

            "out-for-delivery" or "at-locker"
                => TrackingState.OutForDelivery,

            "delivered" or "delivered-to-locker-with-pickup"
                => TrackingState.Delivered,

            "failed-delivery" or "returned-to-sender" or "lost"
                => TrackingState.Failed,

            "cancelled"
                => TrackingState.Cancelled,

            _ => TrackingState.Unknown,
        };
    }

    // Single status-classification chokepoint, sharing SamedayPolicies.IsRetryableStatus with the
    // resilience pipeline so the two verdicts can't drift. Does NOT dispose the response.
    private static async Task EnsureSuccessOrThrowAsync(
        HttpResponseMessage response, string endpoint, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new SamedayAuthException(endpoint);
        if (SamedayPolicies.IsRetryableStatus(response.StatusCode))
            throw new SamedayUnreachableException(endpoint, httpStatus: (int)response.StatusCode);
        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadAsync(response, ct);
            throw new SamedayValidationException(endpoint, (int)response.StatusCode, body);
        }
    }

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
