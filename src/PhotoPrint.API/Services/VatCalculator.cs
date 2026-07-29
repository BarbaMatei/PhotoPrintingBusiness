namespace PhotoPrint.API.Services;

/// <summary>
/// Pure helper that extracts the VAT breakdown from a VAT-inclusive (gross)
/// total at a given rate. Romanian convention: customer-facing prices are
/// gross; VAT is extracted, never added on top.
///
/// Rounding is fixed at <see cref="MidpointRounding.AwayFromZero"/> per
/// ADR-019. The default <c>decimal.Round(x, 2)</c> overload (no mode argument)
/// uses banker's rounding which disagrees with Romanian accountancy
/// convention and ANAF tooling — callers must NOT change the mode in any
/// regulatory path.
///
/// Note on shipping: callers are expected to fold shipping into the gross
/// before calling here. Shipping is treated as VAT-inclusive at the same
/// rate as goods (the simpler and more common Romanian B2C convention).
/// If this changes (e.g. B2B EU customers), the formula stays; the call
/// site supplies a different gross composition.
/// </summary>
public static class VatCalculator
{
    public static VatBreakdown ExtractBreakdown(decimal grossTotalRon, decimal vatRate)
    {
        if (grossTotalRon < 0m)
            throw new ArgumentOutOfRangeException(nameof(grossTotalRon),
                "Gross total must be non-negative.");
        if (vatRate < 0m || vatRate >= 1m)
            throw new ArgumentOutOfRangeException(nameof(vatRate),
                "VAT rate must be in [0, 1).");

        var vat = decimal.Round(
            grossTotalRon * vatRate / (1m + vatRate),
            decimals: 2,
            mode: MidpointRounding.AwayFromZero);

        var net = decimal.Round(
            grossTotalRon - vat,
            decimals: 2,
            mode: MidpointRounding.AwayFromZero);

        return new VatBreakdown(net, vat, grossTotalRon, vatRate);
    }
}

/// <summary>
/// Immutable VAT breakdown derived from <c>(grossTotalRon, vatRate)</c>.
/// Invariant: <c>|NetTotalRon + VatRon - TotalRon| ≤ 0.01</c>.
/// </summary>
public readonly record struct VatBreakdown(
    decimal NetTotalRon, decimal VatRon, decimal TotalRon, decimal VatRate);
