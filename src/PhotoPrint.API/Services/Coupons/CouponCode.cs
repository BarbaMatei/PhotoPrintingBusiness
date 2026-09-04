using System.Text.RegularExpressions;

namespace PhotoPrint.API.Services.Coupons;

public static partial class CouponCode
{
    public static string Normalize(string? raw)
        => (raw ?? string.Empty).Trim().ToUpperInvariant();

    public static bool IsWellFormed(string? raw)
        => CodePattern().IsMatch(Normalize(raw));

    [GeneratedRegex("^[A-Z0-9]{4,20}$")]
    private static partial Regex CodePattern();
}
