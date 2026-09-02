using System.Net;
using System.Net.Http.Headers;

namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// Attaches the bearer token to every outbound ANAF SPV call and implements
/// the "401 → invalidate → re-auth → retry once → <see cref="AnafAuthException"/>"
/// rule (same shape as <c>SamedayAuthHandler</c>).
/// Lives OUTSIDE the Polly resilience pipeline so a session refresh does
/// not burn the transport-retry budget.
/// </summary>
public sealed class AnafAuthHandler : DelegatingHandler
{
    private readonly IAnafTokenProvider _tokenProvider;
    private readonly ILogger<AnafAuthHandler> _logger;

    public AnafAuthHandler(
        IAnafTokenProvider tokenProvider,
        ILogger<AnafAuthHandler> logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await AttachTokenAsync(request, cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();
        _logger.LogInformation(
            "anaf.auth.401-refresh path={Path}",
            request.RequestUri?.AbsolutePath ?? "(unknown)");

        _tokenProvider.Invalidate();

        var retry = await CloneAsync(request, cancellationToken);
        await AttachTokenAsync(retry, cancellationToken);

        var retryResponse = await base.SendAsync(retry, cancellationToken);
        if (retryResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            retryResponse.Dispose();
            throw new AnafAuthException(request.RequestUri?.AbsolutePath ?? "(unknown)");
        }
        return retryResponse;
    }

    private async Task AttachTokenAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage source, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version       = source.Version,
            VersionPolicy = source.VersionPolicy,
        };

        if (source.Content is not null)
        {
            var bytes = await source.Content.ReadAsByteArrayAsync(ct);
            var cloned = new ByteArrayContent(bytes);
            foreach (var header in source.Content.Headers)
                cloned.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = cloned;
        }

        foreach (var header in source.Headers)
        {
            if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                continue;
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var kvp in source.Options)
            clone.Options.TryAdd(kvp.Key, kvp.Value);

        return clone;
    }
}
