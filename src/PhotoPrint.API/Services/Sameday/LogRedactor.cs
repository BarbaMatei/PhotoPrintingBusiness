using System.Net.Http.Headers;

namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Helper for emitting outbound HTTP traces without leaking secrets. The
/// only header value ever produced is the literal string <c>"Bearer ***"</c>
/// when an <c>Authorization</c> header is present; everything else is
/// dropped from the redacted view. Keeps a single chokepoint for log-string
/// formatting so a future caller cannot bypass the redaction by hand-rolling
/// their own log string.
/// </summary>
internal static class LogRedactor
{
    public const string RedactedBearer = "Bearer ***";

    public static string Authorization(AuthenticationHeaderValue? header)
        => header is null ? "(none)" : RedactedBearer;
}
