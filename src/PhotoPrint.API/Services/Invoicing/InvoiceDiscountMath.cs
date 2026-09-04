using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

public static class InvoiceDiscountMath
{
    public const string GenericAllowanceReason = "Reducere comercială";

    public static decimal VatRateFromInvoice(Invoice invoice)
    {
        if (invoice.Order is not null)
            return invoice.Order.VatRate;
        if (invoice.NetTotalRon == 0m)
            return 0m;

        var derived = invoice.VatRon / invoice.NetTotalRon;
        return decimal.Round(derived, 4, MidpointRounding.AwayFromZero);
    }

    public static decimal LineNetTotal(Order order, Invoice invoice)
    {
        if (order.DiscountRon <= 0m) return invoice.NetTotalRon;

        var rate = VatRateFromInvoice(invoice);
        var grossBeforeDiscount = order.SubtotalRon + order.ShippingCostRon;
        return VatCalculator.ExtractBreakdown(grossBeforeDiscount, rate).NetTotalRon;
    }

    public static decimal AllowanceNet(Order order, Invoice invoice)
        => LineNetTotal(order, invoice) - invoice.NetTotalRon;

    public static string AllowanceReason(string? couponCode)
        => string.IsNullOrWhiteSpace(couponCode)
            ? GenericAllowanceReason
            : $"Reducere {couponCode}";

    public static IReadOnlyList<(string Label, decimal Amount)> DiscountRows(Order order, Invoice invoice)
    {
        var allowanceNet = AllowanceNet(order, invoice);
        if (allowanceNet <= 0m) return [];

        return
        [
            ("Total linii (fără TVA):", LineNetTotal(order, invoice)),
            ($"{AllowanceReason(order.CouponCode)}:", -allowanceNet),
        ];
    }
}
