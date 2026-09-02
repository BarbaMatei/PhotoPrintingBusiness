using FluentAssertions;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Pins the Romanian VAT-extraction contract and — critically — its
/// rounding-mode invariant. The default .NET `decimal.Round(x, 2)` uses
/// banker's rounding (`ToEven`), which disagrees with our convention.
/// If a future PR drops the explicit `MidpointRounding.AwayFromZero`
/// argument from `VatCalculator`, the test
/// <see cref="Rounding_uses_AwayFromZero_not_banker_s_rounding"/> fails.
/// </summary>
public class VatCalculatorTests
{
    [Fact]
    public void Story_example_100_at_19_percent_yields_84_03_net_and_15_97_vat()
    {
        // The acceptance criterion in story 001.
        var result = VatCalculator.ExtractBreakdown(grossTotalRon: 100.00m, vatRate: 0.19m);

        result.NetTotalRon.Should().Be(84.03m);
        result.VatRon.Should().Be(15.97m);
        result.TotalRon.Should().Be(100.00m);
        result.VatRate.Should().Be(0.19m);
    }

    [Fact]
    public void Free_order_yields_zero_breakdown()
    {
        var result = VatCalculator.ExtractBreakdown(grossTotalRon: 0m, vatRate: 0.19m);

        result.NetTotalRon.Should().Be(0m);
        result.VatRon.Should().Be(0m);
        result.TotalRon.Should().Be(0m);
    }

    [Theory]
    [InlineData(50.00,  0.19, 42.02,  7.98)]
    [InlineData(75.00,  0.19, 63.03, 11.97)]
    [InlineData(119.00, 0.19, 100.00, 19.00)]
    [InlineData(1.00,   0.19, 0.84,   0.16)]   // tiny: 0.1597 → 0.16 via AwayFromZero
    [InlineData(0.01,   0.19, 0.01,   0.00)]   // a single cent: VAT is below the half-cent → 0
    public void Various_gross_totals_extract_correctly(
        decimal gross, decimal rate, decimal expectedNet, decimal expectedVat)
    {
        var result = VatCalculator.ExtractBreakdown(gross, rate);
        result.NetTotalRon.Should().Be(expectedNet);
        result.VatRon.Should().Be(expectedVat);
    }

    [Fact]
    public void Rounding_uses_AwayFromZero_not_banker_s_rounding()
    {
        // The invariant: the rounding mode is MidpointRounding.AwayFromZero,
        // not the .NET default (ToEven). Construct an input where the two modes
        // disagree, then assert AwayFromZero's answer.
        //
        // For gross = 21.00 at 19%:
        //   raw = 21.00 * 0.19 / 1.19 = 3.35294117647...
        //   → both modes round to 3.35 (no midpoint).
        //
        // For gross = 5.25 at 5%:
        //   raw = 5.25 * 0.05 / 1.05 = 0.25 exactly
        //   AwayFromZero → 0.25 (no rounding needed)
        //   ToEven       → 0.25 (no rounding needed)
        //
        // For gross = 1.05 at 5%:
        //   raw = 1.05 * 0.05 / 1.05 = 0.05 exactly → identical.
        //
        // We need a case where the truncated 3rd decimal is exactly 5 AND
        // the prior digit is even (so ToEven keeps it, AwayFromZero increments).
        //
        // gross = 0.105, rate = 0.10:
        //   raw_vat = 0.105 * 0.10 / 1.10 = 0.009545...
        //
        // Easier: construct directly with decimal.Round equivalents.
        //   2.005 rounded to 2 decimals:
        //     AwayFromZero → 2.01
        //     ToEven       → 2.00
        // We want a (gross, rate) that PRODUCES 2.005 as the raw VAT before rounding.
        //
        // Pick gross = 12.43 at 0.19:
        //   raw_vat = 12.43 * 0.19 / 1.19 = 1.9846...  → both modes → 1.98 (no midpoint).
        //
        // Use the direct test: round 0.015 to 1 decimal.
        var awayFromZero = decimal.Round(0.015m, 2, MidpointRounding.AwayFromZero);
        var toEven       = decimal.Round(0.015m, 2, MidpointRounding.ToEven);
        awayFromZero.Should().Be(0.02m);
        toEven.Should().Be(0.02m);   // 0.015 also rounds up under ToEven because the prior is 1 (odd)

        // Actually use a midpoint where the modes disagree: 0.025 rounded to 2 dp.
        decimal.Round(0.025m, 2, MidpointRounding.AwayFromZero).Should().Be(0.03m);
        decimal.Round(0.025m, 2, MidpointRounding.ToEven).Should().Be(0.02m);
        // VatCalculator MUST agree with AwayFromZero on this input. We can't
        // hand the calculator a raw 0.025 directly (it operates on gross/rate),
        // but we can construct a (gross, rate) whose raw VAT is exactly 0.025:
        //   gross * rate / (1 + rate) = 0.025
        //   gross = 0.025 * (1 + rate) / rate
        //   For rate = 0.25: gross = 0.025 * 1.25 / 0.25 = 0.125
        var result = VatCalculator.ExtractBreakdown(grossTotalRon: 0.125m, vatRate: 0.25m);
        // raw VAT = 0.025 → AwayFromZero rounds to 0.03 (not 0.02 like banker's).
        result.VatRon.Should().Be(0.03m);
    }

    [Fact]
    public void Net_plus_vat_equals_total_within_one_cent_for_random_inputs()
    {
        // Property: for any reasonable (gross, rate), the breakdown reconciles
        // to the gross within ±0.01 RON. Brute-force a wide range.
        for (var cents = 0; cents <= 1_000_000; cents += 137)   // ~7300 samples
        {
            var gross = cents / 100m;
            var result = VatCalculator.ExtractBreakdown(gross, 0.19m);
            var sum = result.NetTotalRon + result.VatRon;
            Math.Abs(sum - gross).Should().BeLessThanOrEqualTo(0.01m,
                because: $"net+vat must reconcile to total for gross={gross}");
        }
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(0.09)]
    [InlineData(0.19)]
    [InlineData(0.21)]
    [InlineData(0.27)]
    public void Reconciles_across_different_rates(decimal rate)
    {
        for (var cents = 1; cents <= 100_000; cents += 251)
        {
            var gross = cents / 100m;
            var result = VatCalculator.ExtractBreakdown(gross, rate);
            (result.NetTotalRon + result.VatRon).Should().BeApproximately(gross, 0.01m);
        }
    }

    [Fact]
    public void Negative_gross_throws()
    {
        var act = () => VatCalculator.ExtractBreakdown(-1m, 0.19m);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("grossTotalRon");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.0)]
    [InlineData(2.5)]
    public void Out_of_range_rate_throws(decimal rate)
    {
        var act = () => VatCalculator.ExtractBreakdown(100m, rate);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("vatRate");
    }

    [Fact]
    public void Rate_zero_is_accepted_and_yields_zero_vat()
    {
        // r = 0 is at the edge; the validator forbids it for VatSettings, but
        // the pure helper accepts it (and yields zero VAT). Useful for testing.
        var result = VatCalculator.ExtractBreakdown(100m, 0m);
        result.VatRon.Should().Be(0m);
        result.NetTotalRon.Should().Be(100m);
    }
}
