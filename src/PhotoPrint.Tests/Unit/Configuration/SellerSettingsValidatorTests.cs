using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Validators;

namespace PhotoPrint.Tests.Unit.Configuration;

/// <summary>
/// Boot-time validation guards for <see cref="SellerSettings"/>. Seller data
/// is embedded in every UBL invoice; a typo here invalidates every emitted
/// invoice — surface it at startup, not at the first ANAF rejection.
/// </summary>
public class SellerSettingsValidatorTests
{
    private static SellerSettings Valid() => new()
    {
        Name = "FotoTipar SRL",
        Cui = "RO12345678",
        RegistrationNumber = "J40/1234/2026",
        IbanRon = "",
        Address = new SellerAddress
        {
            Line1 = "Str. Test 1",
            City = "București",
            PostalCode = "010101",
            CountryCode = "RO",
        },
    };

    private readonly SellerSettingsValidator _sut = new();

    [Fact]
    public void Default_valid_settings_pass()
    {
        var result = _sut.Validate(null, Valid());
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("RO1234567890")]   // 10 digits
    [InlineData("RO12")]            // 2 digits (minimum)
    [InlineData("RO12345678")]      // typical
    public void Cui_in_RO_plus_2_to_10_digits_passes(string cui)
    {
        var s = Valid();
        s.Cui = cui;
        var result = _sut.Validate(null, s);
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345678")]        // missing RO prefix
    [InlineData("ro12345678")]      // lowercase
    [InlineData("RO1")]             // only 1 digit
    [InlineData("RO12345678901")]   // 11 digits — too long
    [InlineData("RO12345A")]        // letter in digits
    public void Invalid_cui_shapes_fail(string cui)
    {
        var s = Valid();
        s.Cui = cui;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Seller:Cui"));
    }

    [Fact]
    public void Missing_required_fields_aggregate_into_one_failure_result()
    {
        var s = new SellerSettings();  // everything empty
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Seller:Name"));
        result.Failures.Should().Contain(f => f.Contains("Seller:RegistrationNumber"));
        result.Failures.Should().Contain(f => f.Contains("Seller:Address:Line1"));
    }

    [Theory]
    [InlineData("DE")]
    [InlineData("FR")]
    [InlineData("RO")]
    public void Iso_alpha2_country_codes_pass(string cc)
    {
        var s = Valid();
        s.Address.CountryCode = cc;
        _sut.Validate(null, s).Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("R")]      // 1 char
    [InlineData("ROM")]    // 3 chars
    [InlineData("ro")]     // lowercase
    [InlineData("12")]     // digits
    public void Non_iso_alpha2_country_codes_fail(string cc)
    {
        var s = Valid();
        s.Address.CountryCode = cc;
        _sut.Validate(null, s).Failed.Should().BeTrue();
    }

    [Fact]
    public void IbanRon_is_optional()
    {
        var s = Valid();
        s.IbanRon = "";
        _sut.Validate(null, s).Succeeded.Should().BeTrue();

        s.IbanRon = "RO49AAAA1B31007593840000";
        _sut.Validate(null, s).Succeeded.Should().BeTrue();
    }
}
