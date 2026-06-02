using System.Net;
using System.Net.Http.Headers;
using PhotoPrint.API.Exceptions;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Attaches the bearer token to every outbound Sameday call except
/// <c>/api/authenticate</c> itself, and implements the "401 → invalidate →
/// re-auth → retry once → <see cref="SamedayAuthException"/>" rule from
/// ADR-014. Lives OUTSIDE the resilience pipeline so a session refresh does
/// not burn the transport-retry budget.
/// </summary>
public sealed class SamedayAuthHandler : DelegatingHandler
{
    private const string AuthenticatePath = "/api/authenticate";

    private readonly ISamedayTokenProvider _tokenProvider;
    private readonly ILogger<SamedayAuthHandler> _logger;

    public SamedayAuthHandler(
        ISamedayTokenProvider tokenProvider,
        ILogger<SamedayAuthHandler> logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Bootstrap call to /api/authenticate must NOT carry a stale token —
        // forward unmodified so SamedayClient.AuthenticateAsync sees raw vendor behaviour.
        if (IsAuthenticatePath(request.RequestUri))
            return await base.SendAsync(request, cancellationToken);

        await AttachTokenAsync(request, cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // 401: invalidate cached token, fetch a fresh one, retry the request EXACTLY ONCE.
        response.Dispose();
        _logger.LogInformation(
            "Sameday returned 401 for {Method} {Path}; invalidating cached token and retrying once.",
            request.Method.Method,
            request.RequestUri?.AbsolutePath ?? "(unknown)");

        _tokenProvider.Invalidate();

        var retryRequest = await CloneAsync(request, cancellationToken);
        await AttachTokenAsync(retryRequest, cancellationToken);

        var retryResponse = await base.SendAsync(retryRequest, cancellationToken);
        if (retryResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            retryResponse.Dispose();
            throw new SamedayAuthException(retryRequest.RequestUri?.AbsolutePath ?? "(unknown)");
        }

        return retryResponse;
    }

    private async Task AttachTokenAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokenProvider.GetTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
    }

    private static bool IsAuthenticatePath(Uri? uri)
        => uri is not null
            && uri.AbsolutePath.EndsWith(AuthenticatePath, StringComparison.OrdinalIgnoreCase);

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy,
        };

        if (source.Content is not null)
        {
            // Buffer the body so the clone has its own readable stream. Every Sameday
            // call uses JsonContent (or no body), so this is always cheap.
            var bytes = await source.Content.ReadAsByteArrayAsync(ct);
            var cloned = new ByteArrayContent(bytes);
            foreach (var header in source.Content.Headers)
                cloned.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = cloned;
        }

        foreach (var header in source.Headers)
        {
            // Authorization is re-attached after the clone; skip it here.
            if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                continue;
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var kvp in source.Options)
            clone.Options.TryAdd(kvp.Key, kvp.Value);

        return clone;
    }
}
