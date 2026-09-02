using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Validators;

/// <summary>
/// Fails fast at boot when <see cref="SellerSettings"/> is misconfigured.
/// Seller fields are embedded in every UBL XML and PDF, so a typo here
/// silently invalidates every emitted invoice — surface it at startup.
/// Bolt 039 follows the project's <c>IValidateOptions&lt;T&gt;</c> pattern;
/// FluentValidation remains the path for controller DTOs.
/// </summary>
public sealed partial class SellerSettingsValidator : IValidateOptions<SellerSettings>
{
    [GeneratedRegex(@"^RO\d{2,10}$")]
    private static partial Regex RomanianCui();

    [GeneratedRegex(@"^[A-Z]{2}$")]
    private static partial Regex Iso3166Alpha2();

    public ValidateOptionsResult Validate(string? name, SellerSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Name))
            failures.Add("Seller:Name is required.");
        else if (options.Name.Length > 200)
            failures.Add("Seller:Name must be ≤ 200 characters.");

        if (string.IsNullOrWhiteSpace(options.Cui) || !RomanianCui().IsMatch(options.Cui))
            failures.Add("Seller:Cui must match '^RO\\d{2,10}$' (e.g. 'RO12345678').");

        if (string.IsNullOrWhiteSpace(options.RegistrationNumber))
            failures.Add("Seller:RegistrationNumber is required.");
        else if (options.RegistrationNumber.Length > 50)
            failures.Add("Seller:RegistrationNumber must be ≤ 50 characters.");

        if (string.IsNullOrWhiteSpace(options.Address.Line1))
            failures.Add("Seller:Address:Line1 is required.");
        if (string.IsNullOrWhiteSpace(options.Address.City))
            failures.Add("Seller:Address:City is required.");
        if (string.IsNullOrWhiteSpace(options.Address.PostalCode))
            failures.Add("Seller:Address:PostalCode is required.");
        if (string.IsNullOrWhiteSpace(options.Address.CountryCode)
            || !Iso3166Alpha2().IsMatch(options.Address.CountryCode))
            failures.Add("Seller:Address:CountryCode must be an ISO 3166-1 alpha-2 code (e.g. 'RO').");

        // IbanRon is optional (cash-on-delivery-only sellers exist).

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
