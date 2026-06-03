using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Validators;

/// <summary>
/// Fails fast at startup when VAT is misconfigured. There is no
/// <c>Enabled</c> guard — VAT is unconditional (intent 016 / bolt 038).
/// </summary>
public sealed partial class VatSettingsValidator : IValidateOptions<VatSettings>
{
    [GeneratedRegex(@"^[A-Z]{2,10}$")]
    private static partial Regex SeriesRegex();

    public ValidateOptionsResult Validate(string? name, VatSettings options)
    {
        var failures = new List<string>();

        // The extraction formula r/(1+r) requires r ∈ (0, 1). r = 0 would mean
        // "no VAT" which is legally wrong for our jurisdiction; r ≥ 1 breaks
        // the formula mathematically.
        if (options.Rate <= 0m || options.Rate >= 1m)
            failures.Add("Vat:Rate must be between 0 (exclusive) and 1 (exclusive).");

        if (string.IsNullOrWhiteSpace(options.InvoiceSeries)
            || !SeriesRegex().IsMatch(options.InvoiceSeries))
        {
            failures.Add("Vat:InvoiceSeries must be 2–10 uppercase ASCII letters.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
