using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Validators;

namespace PhotoPrint.Tests.Unit.Configuration;

/// <summary>
/// Pins the VAT settings validation contract. Unlike Sameday / Sentry /
/// Observability, this validator has no <c>Enabled</c> guard — VAT is
/// unconditional.
/// </summary>
public class VatSettingsValidatorTests
{
    private readonly VatSettingsValidator _sut = new();

    [Fact]
    public void Defaults_are_valid()
    {
        var result = _sut.Validate(null, new VatSettings());
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.19)]
    [InlineData(0.05)]
    [InlineData(0.21)]
    [InlineData(0.001)]
    [InlineData(0.999)]
    public void Rate_in_open_interval_zero_one_is_valid(decimal rate)
    {
        var result = _sut.Validate(null, new VatSettings { Rate = rate });
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(-1)]
    public void Rate_at_or_outside_the_open_interval_fails(decimal rate)
    {
        var result = _sut.Validate(null, new VatSettings { Rate = rate });
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Vat:Rate"));
    }

    [Theory]
    [InlineData("FT")]
    [InlineData("FP")]
    [InlineData("FS")]
    [InlineData("FOTOTIPAR")]
    public void Valid_series_codes_are_accepted(string series)
    {
        var result = _sut.Validate(null, new VatSettings { InvoiceSeries = series });
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("F")]
    [InlineData("ft")]              // lowercase
    [InlineData("Ft")]              // mixed
    [InlineData("FT-2026")]         // contains dash and digits
    [InlineData("FT_X")]            // underscore
    [InlineData("TOOLONGSERIESCODE")] // 17 chars > 10
    public void Invalid_series_codes_fail(string series)
    {
        var result = _sut.Validate(null, new VatSettings { InvoiceSeries = series });
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Vat:InvoiceSeries"));
    }

    [Fact]
    public void Multiple_failures_aggregate()
    {
        var result = _sut.Validate(null, new VatSettings
        {
            Rate = 2.0m,
            InvoiceSeries = "lowercase",
        });
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(2);
    }
}
