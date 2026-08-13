using System.Net;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// Typed <see cref="HttpClient"/> for ANAF SPV. The HTTP pipeline (configured
/// by <c>IHttpClientFactory</c> in <c>Program.cs</c>):
///
///   outer:  AnafAuthHandler   (bearer + 401-retry-once per ADR-014)
///   inner:  Polly transient   (5xx / 408 / 429 with exponential backoff)
///
/// Body content is never logged — buyer PII would leak. Endpoint paths
/// and status codes are the only log-worthy signals.
/// </summary>
public sealed class AnafSpvClient : IAnafSpvClient
{
    private readonly HttpClient _http;
    private readonly AnafSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<AnafSpvClient> _logger;

    public AnafSpvClient(
        HttpClient http,
        IOptions<AnafSettings> settings,
        TimeProvider clock,
        ILogger<AnafSpvClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AnafUploadResult> UploadAsync(byte[] invoiceXml, CancellationToken ct = default)
    {
        const string endpoint = "upload?standard=UBL";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new ByteArrayContent(invoiceXml),
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new AnafUnreachableException(endpoint, inner: ex);
        }

        using var _ = response;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new AnafAuthException(endpoint);    // AnafAuthHandler already attempted refresh

        if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout)
            throw new AnafUnreachableException(endpoint, httpStatus: (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
            throw new AnafUnreachableException(endpoint, httpStatus: (int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var xml  = SafeParse(body, endpoint);

        // ANAF's "upload" response shape:
        //   <header xmlns="..." index_incarcare="12345" data_incarcare="2026-06-03 11:30:00" />
        // Or, when the upload was accepted but with validation errors:
        //   <header ...>
        //     <Errors errorMessage="..." />
        //   </header>
        var errorsEl = xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "Errors");
        if (errorsEl is not null)
        {
            var msg = errorsEl.Attribute("errorMessage")?.Value
                ?? errorsEl.Value
                ?? "ANAF returned errors with the upload response.";
            throw new AnafUploadException(msg);
        }

        var indexAttr = xml.Attribute("index_incarcare")?.Value
            ?? xml.Descendants().Select(e => e.Attribute("index_incarcare")?.Value).FirstOrDefault(v => v is not null);

        if (string.IsNullOrWhiteSpace(indexAttr))
            throw new AnafUploadException("ANAF upload response missing 'index_incarcare'.");

        _logger.LogInformation("anaf.spv.upload upload_id={UploadId}", indexAttr);
        return new AnafUploadResult(indexAttr, _clock.GetUtcNow());
    }

    public async Task<AnafStatusResult> GetStatusAsync(string uploadId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
            throw new ArgumentException("uploadId is required.", nameof(uploadId));

        var endpoint = $"stareMesaj?id_incarcare={Uri.EscapeDataString(uploadId)}";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(endpoint, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new AnafUnreachableException(endpoint, inner: ex);
        }

        using var _ = response;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new AnafAuthException(endpoint);

        if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout)
            throw new AnafUnreachableException(endpoint, httpStatus: (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
            throw new AnafUnreachableException(endpoint, httpStatus: (int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var xml  = SafeParse(body, endpoint);

        var stare = xml.Attribute("stare")?.Value
            ?? xml.Descendants().Select(e => e.Attribute("stare")?.Value).FirstOrDefault(v => v is not null);

        var status = MapStatus(stare);
        if (status == AnafExternalStatus.Unknown)
        {
            _logger.LogWarning(
                "anaf.spv.status-unrecognized upload_id={UploadId} stare={Stare}", uploadId, stare);
        }
        string? errorMessage = null;
        if (status == AnafExternalStatus.Rejected)
        {
            var errorsEl = xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "Errors");
            errorMessage = errorsEl?.Attribute("errorMessage")?.Value
                ?? errorsEl?.Value
                ?? "ANAF rejected the invoice (no error detail).";
        }

        _logger.LogInformation(
            "anaf.spv.status upload_id={UploadId} status={Status}",
            uploadId, status);

        return new AnafStatusResult(status, errorMessage, _clock.GetUtcNow());
    }

    private static AnafExternalStatus MapStatus(string? stare)
        => stare?.Trim().ToLowerInvariant() switch
        {
            "ok"            => AnafExternalStatus.Validated,
            "nok"           => AnafExternalStatus.Rejected,
            "in prelucrare" => AnafExternalStatus.InProgress,
            null            => AnafExternalStatus.Unknown,
            _               => AnafExternalStatus.Unknown,
        };

    private static XElement SafeParse(string body, string endpoint)
    {
        try
        {
            return XDocument.Parse(body).Root
                ?? throw new AnafUnreachableException(endpoint);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new AnafUnreachableException(endpoint, inner: ex);
        }
    }
}
