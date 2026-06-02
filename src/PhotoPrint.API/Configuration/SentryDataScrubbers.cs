using Sentry;

namespace PhotoPrint.API.Configuration;

/// <summary>
/// PII scrubber applied via <c>SentryOptions.SetBeforeSend</c>. Every event
/// leaving the SDK passes through here; if a sensitive value ever reaches
/// Sentry, this list is wrong and gets a CR.
///
/// Keep: stack trace, exception type/message, structured tags (correlation_id,
/// user_id, environment, release), query string, route.
/// Scrub: full request body (always), Authorization/Cookie/Set-Cookie/
/// X-Guest-Token headers, and any extra/header key whose name contains a
/// sensitive substring (email, phone, password, etc.).
/// </summary>
public static class SentryDataScrubbers
{
    public const string ScrubbedMarker = "<scrubbed>";
    public const string ScrubbedBodyMarker = "<scrubbed:request-body>";

    public static readonly string[] SensitiveHeaders =
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Guest-Token",
    };

    public static readonly string[] SensitiveFieldNames =
    {
        "email",
        "phone",
        "password",
        "confirmPassword",
        "currentPassword",
        "newPassword",
    };

    public static SentryEvent? Scrub(SentryEvent e)
    {
        var req = e.Request;
        if (req is not null)
        {
            req.Data = ScrubbedBodyMarker;

            foreach (var name in SensitiveHeaders)
            {
                if (req.Headers.ContainsKey(name))
                    req.Headers[name] = ScrubbedMarker;
            }

            foreach (var key in req.Headers.Keys.ToList())
            {
                if (IsSensitiveKey(key))
                    req.Headers[key] = ScrubbedMarker;
            }
        }

        foreach (var key in e.Extra.Keys.ToList())
        {
            if (IsSensitiveKey(key))
                e.SetExtra(key, ScrubbedMarker);
        }

        return e;
    }

    public static bool IsSensitiveKey(string key)
    {
        foreach (var sensitive in SensitiveFieldNames)
        {
            if (key.Contains(sensitive, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
