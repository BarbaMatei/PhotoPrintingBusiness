using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// UBL 2.1 + CIUS-RO XML builder. Hand-rolled via <see cref="XDocument"/> —
/// XSD-generated bindings would be 10× the size and harder to audit
/// (story 001 technical note). Per-line VAT category is always <c>S</c>
/// (standard rate 19%) in v1; reduced/exempt slots exist in the model
/// but no code path selects them yet.
///
/// Currency emission: every monetary element carries <c>currencyID="RON"</c>
/// and is formatted with <see cref="CultureInfo.InvariantCulture"/> to
/// guarantee a dot decimal separator regardless of host locale.
/// </summary>
public sealed class InvoiceXmlBuilder : IInvoiceXmlBuilder
{
    private static readonly XNamespace Inv  = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace Cac  = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc  = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    private const string CustomizationId  = "urn:cen.eu:en16931:2017#compliant#urn:efactura.mfinante.ro:CIUS-RO:1.0.1";
    private const string CurrencyCode     = "RON";
    private const string InvoiceTypeCode  = "380";  // Commercial invoice
    private const string VatCategoryStandard = "S";
    private const string TaxSchemeIdVat   = "VAT";
    private const string GuestBuyerName   = "Persoană fizică";

    public byte[] Build(Order order, Invoice invoice, SellerSettings seller)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(seller);

        if (order.Items.Count == 0)
            throw new InvoiceNotBuildableException(
                $"Cannot build invoice {invoice.InvoiceNumber}: order has no items.");

        InvoiceAddressFormatter.EnsureBuyerAddressUsable(order);

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Inv + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", Cac),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
                new XElement(Cbc + "UBLVersionID",     "2.1"),
                new XElement(Cbc + "CustomizationID",  CustomizationId),
                new XElement(Cbc + "ID",               invoice.InvoiceNumber),       // BT-1
                new XElement(Cbc + "IssueDate",        invoice.IssuedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),  // BT-2
                new XElement(Cbc + "InvoiceTypeCode",  InvoiceTypeCode),             // BT-3
                new XElement(Cbc + "Note",             BuildNote(order)),             // BT-22
                new XElement(Cbc + "DocumentCurrencyCode", CurrencyCode),
                BuildSupplierParty(seller),                                            // BG-4 / BT-31,32
                BuildCustomerParty(order),                                             // BG-7 / BT-44+
                BuildPaymentMeans(seller),                                             // BG-19
                BuildAllowance(order, invoice),
                BuildTaxTotal(invoice),                                                // BG-23
                BuildLegalMonetaryTotal(order, invoice),
                BuildInvoiceLines(order, invoice)                                      // BG-25
            ));

        using var stream = new MemoryStream();
        using (var writer = new System.Xml.XmlTextWriter(stream, new UTF8Encoding(false)))
        {
            writer.Formatting = System.Xml.Formatting.Indented;
            doc.Save(writer);
        }
        return stream.ToArray();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildNote(Order order)
    {
        var note = $"Order {order.OrderNumber}";
        if (!string.IsNullOrWhiteSpace(order.AwbNumber))
            note += $" / AWB {order.AwbNumber}";
        return note;
    }

    private static XElement BuildSupplierParty(SellerSettings seller)
    {
        return new XElement(Cac + "AccountingSupplierParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", seller.Name)),
                new XElement(Cac + "PostalAddress",
                    new XElement(Cbc + "StreetName",      seller.Address.Line1),
                    new XElement(Cbc + "CityName",        seller.Address.City),
                    new XElement(Cbc + "PostalZone",      seller.Address.PostalCode),
                    new XElement(Cac + "Country",
                        new XElement(Cbc + "IdentificationCode", seller.Address.CountryCode))),
                new XElement(Cac + "PartyTaxScheme",
                    new XElement(Cbc + "CompanyID", seller.Cui),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", TaxSchemeIdVat))),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", seller.Name),
                    new XElement(Cbc + "CompanyID",        seller.RegistrationNumber))));
    }

    private static XElement BuildCustomerParty(Order order)
    {
        var isGuest = order.UserId is null && order.User is null;

        var rawBuyerName = isGuest
            ? GuestBuyerName
            : (order.User?.FirstName + " " + order.User?.LastName).Trim() is { Length: > 0 } full
                ? full
                : (order.ShippingAddress?.RecipientName ?? GuestBuyerName);
        var buyerName = InvoiceAddressFormatter.Truncate(rawBuyerName, InvoiceAddressFormatter.PartyNameMaxLength);

        var addr = order.ShippingAddress!;

        var streetName = InvoiceAddressFormatter.Truncate(
            InvoiceAddressFormatter.FormatStreetName(addr.Street, addr.Number, addr.Block),
            InvoiceAddressFormatter.StreetNameMaxLength);
        var cityName = InvoiceAddressFormatter.Truncate(addr.City, InvoiceAddressFormatter.CityNameMaxLength);
        var postalZone = InvoiceAddressFormatter.Truncate(addr.PostalCode, InvoiceAddressFormatter.CityNameMaxLength);

        var party = new XElement(Cac + "Party",
            new XElement(Cac + "PartyName",
                new XElement(Cbc + "Name", buyerName)),
            new XElement(Cac + "PostalAddress",
                new XElement(Cbc + "StreetName",  streetName),
                new XElement(Cbc + "CityName",    cityName),
                new XElement(Cbc + "PostalZone", postalZone),
                new XElement(Cac + "Country",
                    new XElement(Cbc + "IdentificationCode", "RO"))),
            new XElement(Cac + "PartyLegalEntity",
                new XElement(Cbc + "RegistrationName", buyerName)));

        // BT-48 (BuyerVATIdentifier) is omitted for guest / individual buyers
        // per story 001's guest edge case. B2B with a CUI would populate
        // PartyTaxScheme here — out of scope for v1.

        return new XElement(Cac + "AccountingCustomerParty", party);
    }

    private static XElement BuildPaymentMeans(SellerSettings seller)
    {
        // PaymentMeansCode = 42 → "Payment to bank account" (UBL code list 4461).
        // The block is required by CIUS-RO; if the seller doesn't run an
        // IBAN-collecting business, the iban is omitted but the element stays.
        var pm = new XElement(Cac + "PaymentMeans",
            new XElement(Cbc + "PaymentMeansCode", "42"));

        if (!string.IsNullOrWhiteSpace(seller.IbanRon))
        {
            pm.Add(new XElement(Cac + "PayeeFinancialAccount",
                new XElement(Cbc + "ID", seller.IbanRon)));
        }

        return pm;
    }

    private static XElement BuildTaxTotal(Invoice invoice)
    {
        return new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", CurrencyCode),
                FormatMoney(invoice.VatRon)),
            new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxableAmount",
                    new XAttribute("currencyID", CurrencyCode),
                    FormatMoney(invoice.NetTotalRon)),
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", CurrencyCode),
                    FormatMoney(invoice.VatRon)),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cbc + "ID",      VatCategoryStandard),
                    new XElement(Cbc + "Percent", FormatPercent(InvoiceDiscountMath.VatRateFromInvoice(invoice))),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", TaxSchemeIdVat)))));
    }

    private static XElement BuildLegalMonetaryTotal(Order order, Invoice invoice)
    {
        var lineNetTotal = InvoiceDiscountMath.LineNetTotal(order, invoice);
        var allowanceNet = lineNetTotal - invoice.NetTotalRon;

        var total = new XElement(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", CurrencyCode),
                FormatMoney(lineNetTotal)),
            new XElement(Cbc + "TaxExclusiveAmount",
                new XAttribute("currencyID", CurrencyCode),
                FormatMoney(invoice.NetTotalRon)));

        total.Add(new XElement(Cbc + "TaxInclusiveAmount",
            new XAttribute("currencyID", CurrencyCode),
            FormatMoney(invoice.TotalRon)));

        if (allowanceNet > 0m)
        {
            total.Add(new XElement(Cbc + "AllowanceTotalAmount",
                new XAttribute("currencyID", CurrencyCode),
                FormatMoney(allowanceNet)));
        }

        total.Add(new XElement(Cbc + "PayableAmount",
            new XAttribute("currencyID", CurrencyCode),
            FormatMoney(invoice.TotalRon)));

        return total;
    }

    private static XElement? BuildAllowance(Order order, Invoice invoice)
    {
        var allowanceNet = InvoiceDiscountMath.AllowanceNet(order, invoice);
        if (allowanceNet <= 0m) return null;

        var rate = InvoiceDiscountMath.VatRateFromInvoice(invoice);
        var reason = InvoiceDiscountMath.AllowanceReason(
            InvoiceAddressFormatter.StripXmlInvalid(order.CouponCode));

        return new XElement(Cac + "AllowanceCharge",
            new XElement(Cbc + "ChargeIndicator", "false"),
            new XElement(Cbc + "AllowanceChargeReason", reason),
            new XElement(Cbc + "Amount",
                new XAttribute("currencyID", CurrencyCode),
                FormatMoney(allowanceNet)),
            new XElement(Cac + "TaxCategory",
                new XElement(Cbc + "ID", VatCategoryStandard),
                new XElement(Cbc + "Percent", FormatPercent(rate)),
                new XElement(Cac + "TaxScheme",
                    new XElement(Cbc + "ID", TaxSchemeIdVat))));
    }

    private static IEnumerable<XElement> BuildInvoiceLines(Order order, Invoice invoice)
    {
        var rate = InvoiceDiscountMath.VatRateFromInvoice(invoice);

        var lines = order.Items
            .Select(item => (
                Description: InvoiceAddressFormatter.StripXmlInvalid($"{item.ProductSnapshot.ProductName} ({item.ProductSnapshot.Size}, {item.ProductSnapshot.Finish})"),
                Quantity: item.Quantity,
                GrossTotal: item.LineTotalRon))
            .ToList();
        if (order.ShippingCostRon > 0)
            lines.Add(("Transport", 1, order.ShippingCostRon));

        var netTotals = lines.Select(l => VatCalculator.ExtractBreakdown(l.GrossTotal, rate).NetTotalRon).ToList();
        var residual = InvoiceDiscountMath.LineNetTotal(order, invoice) - netTotals.Sum();
        if (residual != 0m)
            netTotals[^1] += residual;

        return lines.Select((line, i) =>
        {
            var netTotal = netTotals[i];
            // Derived from the reconciled net total, not an independent extraction — that drifts from the line total whenever Quantity > 1.
            var netUnitPrice = decimal.Round(netTotal / line.Quantity, 2, MidpointRounding.AwayFromZero);
            return BuildLine(
                id: i + 1,
                description: line.Description,
                quantity: line.Quantity,
                lineTotalRon: netTotal,
                unitPriceRon: netUnitPrice,
                vatRate: rate);
        });
    }

    private static XElement BuildLine(
        int id, string description, int quantity,
        decimal lineTotalRon, decimal unitPriceRon, decimal vatRate)
    {
        return new XElement(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", id.ToString(CultureInfo.InvariantCulture)),
            new XElement(Cbc + "InvoicedQuantity",
                new XAttribute("unitCode", "H87"),    // UBL code for "piece"
                quantity.ToString(CultureInfo.InvariantCulture)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", CurrencyCode),
                FormatMoney(lineTotalRon)),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Name",                description),
                new XElement(Cac + "ClassifiedTaxCategory",
                    new XElement(Cbc + "ID",      VatCategoryStandard),
                    new XElement(Cbc + "Percent", FormatPercent(vatRate)),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", TaxSchemeIdVat)))),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", CurrencyCode),
                    FormatMoney(unitPriceRon))));
    }

    private static string FormatMoney(decimal amount)
        => amount.ToString("F2", CultureInfo.InvariantCulture);

    private static string FormatPercent(decimal rate)
        => (rate * 100m).ToString("F2", CultureInfo.InvariantCulture);

}
