using Sentry;
using Sentry.Protocol;
using Serilog;

namespace PhotoPrint.API.Configuration;

public static class SentryDataScrubbers
{
    public const string ScrubbedMarker = "<scrubbed>";
    public const string ScrubbedBodyMarker = "<scrubbed:request-body>";

    public static readonly string[] AllowedHeaders =
    {
        "Accept",
        "Accept-Encoding",
        "Accept-Language",
        "Content-Length",
        "Content-Type",
        "Host",
        "User-Agent",
        "X-Correlation-Id",
    };

    public static readonly string[] AllowedEnvKeys =
    {
        "SERVER_NAME",
        "SERVER_PORT",
    };

    public static readonly string[] AllowedExtraKeys = Array.Empty<string>();

    public static readonly string[] AllowedDiagnosticKeys =
    {
        "method",
        "http.method",
        "http.request.method",
        "status_code",
        "http.status_code",
        "http.response.status_code",
        "db.system",
        "otel.kind",
    };

    public static readonly string[] UrlValuedKeys =
    {
        "url",
        "uri",
        "http.url",
        "http.request.url",
        "server.address",
    };

    private static readonly char[] UrlTrimPoints = { '?', '#' };

    public static void Register(SentryOptions options)
    {
        options.SetBeforeSend((SentryEvent e, SentryHint _) => Scrub(e));
        options.SetBeforeSendTransaction((SentryTransaction t, SentryHint _) => Scrub(t));
        options.SetBeforeBreadcrumb((Breadcrumb b, SentryHint _) => Scrub(b));
    }

    public static SentryEvent? Scrub(SentryEvent e)
    {
        try
        {
            ScrubRequest(e.Request);
            ScrubUser(e.User);
            ScrubContexts(e.Contexts);
            RedactExtra(e.Extra.Keys, e.SetExtra);
            ScrubMessage(e.Message);
            ScrubExceptions(e.SentryExceptions);
            return e;
        }
        catch (Exception ex)
        {
            return Dropped<SentryEvent>(ex);
        }
    }

    public static SentryTransaction? Scrub(SentryTransaction transaction)
    {
        try
        {
            ScrubRequest(transaction.Request);
            ScrubUser(transaction.User);
            ScrubContexts(transaction.Contexts);
            RedactExtra(transaction.Extra.Keys, transaction.SetExtra);

            foreach (var span in transaction.Spans)
                ScrubSpan(span);

            return transaction;
        }
        catch (Exception ex)
        {
            return Dropped<SentryTransaction>(ex);
        }
    }

    public static Breadcrumb? Scrub(Breadcrumb breadcrumb)
    {
        try
        {
            var message = SanitizeDescription(breadcrumb.Message);
            var data = breadcrumb.Data;

            if (data is not null)
            {
                data = data.ToDictionary(
                    pair => pair.Key,
                    pair => ScrubDiagnosticValue(pair.Key, pair.Value) ?? ScrubbedMarker);
            }

            if (ReferenceEquals(message, breadcrumb.Message) && ReferenceEquals(data, breadcrumb.Data))
                return breadcrumb;

            return new Breadcrumb(message!, breadcrumb.Type!, data, breadcrumb.Category, breadcrumb.Level);
        }
        catch (Exception ex)
        {
            return Dropped<Breadcrumb>(ex);
        }
    }

    private static void ScrubRequest(SentryRequest? request)
    {
        if (request is null)
            return;

        request.Data = ScrubbedBodyMarker;
        request.QueryString = ScrubQueryString(request.QueryString);
        request.Url = SanitizeUrl(request.Url);

        if (request.Cookies is not null)
            request.Cookies = ScrubbedMarker;

        RedactValues(request.Headers, AllowedHeaders);
        RedactValues(request.Env, AllowedEnvKeys);
        RedactValues(request.Other, Array.Empty<string>());
    }

    private static void ScrubUser(SentryUser? user)
    {
        if (user is null)
            return;

        user.Email = null;
        user.Username = null;
        user.IpAddress = null;
        user.Other = new Dictionary<string, string>();

#pragma warning disable CS0618
        user.Segment = null;
#pragma warning restore CS0618
    }

    private static void ScrubContexts(SentryContexts? contexts)
    {
        if (contexts is null)
            return;

        if (!contexts.TryGetValue(Response.Type, out var value) || value is not Response response)
            return;

        response.Data = ScrubbedBodyMarker;

        if (response.Cookies is not null)
            response.Cookies = ScrubbedMarker;

        RedactValues(response.Headers, AllowedHeaders);
    }

    private static void ScrubMessage(SentryMessage? message)
    {
        if (message is null)
            return;

        if (message.Message is not null)
            message.Formatted = null;

        message.Params = null;
    }

    private static void ScrubExceptions(IEnumerable<SentryException>? exceptions)
    {
        if (exceptions is null)
            return;

        foreach (var exception in exceptions)
        {
            if (exception.Mechanism is { } mechanism)
                RedactValues(mechanism.Data, Array.Empty<string>());
        }
    }

    private static void ScrubSpan(SentrySpan span)
    {
        span.Description = SanitizeDescription(span.Description);

        foreach (var key in span.Extra.Keys.ToList())
            span.SetExtra(key, ScrubDiagnosticValue(key, span.Extra[key]?.ToString()) ?? ScrubbedMarker);

        foreach (var key in span.Tags.Keys.ToList())
            span.SetTag(key, ScrubDiagnosticValue(key, span.Tags[key]) ?? ScrubbedMarker);
    }

    private static void RedactExtra(IEnumerable<string> keys, Action<string, object?> setExtra)
    {
        foreach (var key in keys.ToList())
        {
            if (!IsAllowed(AllowedExtraKeys, key))
                setExtra(key, ScrubbedMarker);
        }
    }

    private static void RedactValues(IDictionary<string, string> bag, string[] allowed)
    {
        foreach (var key in bag.Keys.ToList())
        {
            if (!IsAllowed(allowed, key))
                bag[key] = ScrubbedMarker;
        }
    }

    private static void RedactValues(IDictionary<string, object> bag, string[] allowed)
    {
        foreach (var key in bag.Keys.ToList())
        {
            if (!IsAllowed(allowed, key))
                bag[key] = ScrubbedMarker;
        }
    }

    private static string? ScrubDiagnosticValue(string key, string? value)
    {
        if (value is null)
            return null;

        if (IsAllowed(AllowedDiagnosticKeys, key))
            return value;

        return IsAllowed(UrlValuedKeys, key) ? SanitizeUrl(value) : ScrubbedMarker;
    }

    private static string? ScrubQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return queryString;

        var prefix = queryString[0] == '?' ? "?" : string.Empty;
        var pairs = queryString[prefix.Length..].Split('&');
        var scrubbed = new string[pairs.Length];

        for (var i = 0; i < pairs.Length; i++)
        {
            var separator = pairs[i].IndexOf('=');
            var name = separator < 0 ? null : pairs[i][..separator];
            scrubbed[i] = name is not null && IsPlainParameterName(name)
                ? $"{name}={ScrubbedMarker}"
                : ScrubbedMarker;
        }

        return prefix + string.Join('&', scrubbed);
    }

    private static string? SanitizeDescription(string? description)
    {
        if (string.IsNullOrEmpty(description) || !description.Contains("://", StringComparison.Ordinal))
            return description;

        var tokens = description.Split(' ');

        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Contains("://", StringComparison.Ordinal))
                tokens[i] = SanitizeUrl(tokens[i])!;
        }

        return string.Join(' ', tokens);
    }

    private static string? SanitizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        var cut = url.IndexOfAny(UrlTrimPoints);
        var trimmed = cut < 0 ? url : url[..cut];

        var schemeEnd = trimmed.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
            return trimmed;

        var authorityStart = schemeEnd + 3;
        var authorityEnd = trimmed.IndexOf('/', authorityStart);
        var authority = authorityEnd < 0 ? trimmed[authorityStart..] : trimmed[authorityStart..authorityEnd];

        var credentials = authority.LastIndexOf('@');
        if (credentials < 0)
            return trimmed;

        var tail = authorityEnd < 0 ? string.Empty : trimmed[authorityEnd..];
        return $"{trimmed[..authorityStart]}{ScrubbedMarker}@{authority[(credentials + 1)..]}{tail}";
    }

    private static bool IsPlainParameterName(string name)
    {
        if (name.Length is 0 or > 64)
            return false;

        foreach (var c in name)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-' or '.' or '[' or ']'))
                return false;
        }

        return true;
    }

    private static bool IsAllowed(string[] allowed, string key) =>
        allowed.Contains(key, StringComparer.OrdinalIgnoreCase);

    // The SDK sends the original payload when this hook throws, so any failure must drop it.
    private static T? Dropped<T>(Exception ex)
        where T : class
    {
        Log.Error(ex, "Sentry payload scrubbing failed; payload dropped instead of sent unscrubbed");
        return null;
    }
}
