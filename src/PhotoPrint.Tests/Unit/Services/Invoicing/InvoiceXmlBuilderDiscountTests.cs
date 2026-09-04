using System.Globalization;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

public class InvoiceXmlBuilderDiscountTests
{
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    private const decimal Rate = 0.19m;

    private static SellerSettings Seller() => new()
    {
        Name = "FotoTipar SRL",
        Cui = "RO12345678",
        RegistrationNumber = "J40/1234/2026",
        IbanRon = "RO49AAAA1B31007593840000",
        Address = new SellerAddress
        {
            Line1 = "Str. Test 1", City = "București", PostalCode = "010101", CountryCode = "RO",
        },
    };

    private static (Order order, Invoice invoice) Fixture(
        decimal goods = 100.00m,
        decimal shippingCost = 20.00m,
        decimal discount = 0m,
        string? couponCode = null)
    {
        var payable = goods + shippingCost - discount;
        var vat = VatCalculator.ExtractBreakdown(payable, Rate);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-12345",
            Status = OrderStatus.Paid,
            PaidAt = new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), Email = "x@y.ro", FirstName = "Alex", LastName = "Pop" },
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "Str. Buyer", Number = "10",
                City = "Cluj-Napoca", County = "Cluj", PostalCode = "400100",
                RecipientName = "Alex Pop", Phone = "0700000000",
            },
            ShippingCostRon = shippingCost,
            SubtotalRon = goods,
            DiscountRon = discount,
            CouponCode = couponCode,
            TotalRon = payable,
            NetTotalRon = vat.NetTotalRon,
            VatRon = vat.VatRon,
            VatRate = Rate,
            Items = new List<OrderItem>
            {
                new()
                {
                    Quantity = 5, UnitPriceRon = goods / 5m, LineTotalRon = goods,
                    ProductId = Guid.NewGuid(),
                    ProductSnapshot = new ProductSnapshot
                    {
                        ProductName = "Foto 10x15", Size = "10x15", Finish = "Lucios",
                    },
                },
            },
        };

        var invoice = new Invoice
        {
            OrderId = order.Id,
            InvoiceNumber = "FT-2026-00001",
            Series = "FT", Number = 1,
            IssuedAt = order.PaidAt!.Value,
            NetTotalRon = order.NetTotalRon,
            VatRon = order.VatRon,
            TotalRon = order.TotalRon,
            Order = order,
            AnafStatus = InvoiceAnafStatus.Pending,
            CreatedAt = order.PaidAt!.Value,
        };

        return (order, invoice);
    }

    private static XDocument BuildAndParse(Order order, Invoice invoice)
        => XDocument.Parse(Encoding.UTF8.GetString(
            new InvoiceXmlBuilder().Build(order, invoice, Seller())));

    private static decimal Money(XElement? element)
        => decimal.Parse(element!.Value, CultureInfo.InvariantCulture);

    [Fact]
    public void Build_OrderWithoutDiscount_EmitsNoAllowanceCharge()
    {
        var (order, invoice) = Fixture();

        var doc = BuildAndParse(order, invoice);

        doc.Root!.Elements(Cac + "AllowanceCharge").Should().BeEmpty();
        doc.Descendants(Cbc + "AllowanceTotalAmount").Should().BeEmpty();

        var totals = doc.Root.Element(Cac + "LegalMonetaryTotal")!;
        Money(totals.Element(Cbc + "LineExtensionAmount"))
            .Should().Be(invoice.NetTotalRon);
        Money(totals.Element(Cbc + "TaxExclusiveAmount"))
            .Should().Be(invoice.NetTotalRon);
    }

    [Fact]
    public void Build_OrderWithDiscount_EmitsAllowanceChargeWithChargeIndicatorFalse()
    {
        var (order, invoice) = Fixture(discount: 30.00m, couponCode: "VARA30");

        var doc = BuildAndParse(order, invoice);

        var allowance = doc.Root!.Elements(Cac + "AllowanceCharge").Single();
        allowance.Element(Cbc + "ChargeIndicator")!.Value.Should().Be("false");
        allowance.Element(Cbc + "AllowanceChargeReason")!.Value.Should().Contain("VARA30");
        allowance.Element(Cac + "TaxCategory")!.Element(Cbc + "ID")!.Value.Should().Be("S");
        Money(allowance.Element(Cbc + "Amount")).Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Build_OrderWithDiscount_ReconcilesTaxExclusiveAgainstLinesMinusAllowance()
    {
        var (order, invoice) = Fixture(discount: 30.00m, couponCode: "VARA30");

        var doc = BuildAndParse(order, invoice);

        var totals = doc.Root!.Element(Cac + "LegalMonetaryTotal")!;
        var lineExtension = Money(totals.Element(Cbc + "LineExtensionAmount"));
        var allowanceTotal = Money(totals.Element(Cbc + "AllowanceTotalAmount"));
        var taxExclusive = Money(totals.Element(Cbc + "TaxExclusiveAmount"));
        var taxInclusive = Money(totals.Element(Cbc + "TaxInclusiveAmount"));
        var payable = Money(totals.Element(Cbc + "PayableAmount"));

        (lineExtension - allowanceTotal).Should().Be(taxExclusive);
        taxExclusive.Should().Be(invoice.NetTotalRon);
        taxInclusive.Should().Be(invoice.TotalRon);
        payable.Should().Be(invoice.TotalRon);
    }

    [Fact]
    public void Build_OrderWithDiscountAndShipping_KeepsTransportLinePositive()
    {
        var (order, invoice) = Fixture(
            goods: 100.00m, shippingCost: 20.00m, discount: 50.00m, couponCode: "HALFOFF");

        var doc = BuildAndParse(order, invoice);

        var lineAmounts = doc.Root!.Elements(Cac + "InvoiceLine")
            .Select(l => Money(l.Element(Cbc + "LineExtensionAmount")))
            .ToList();

        lineAmounts.Should().HaveCount(2);
        lineAmounts.Should().OnlyContain(a => a > 0m);
    }

    [Fact]
    public void Build_OrderWithDiscount_LineAmountsSumToLineExtensionTotal()
    {
        var (order, invoice) = Fixture(discount: 30.00m, couponCode: "VARA30");

        var doc = BuildAndParse(order, invoice);

        var lineSum = doc.Root!.Elements(Cac + "InvoiceLine")
            .Sum(l => Money(l.Element(Cbc + "LineExtensionAmount")));
        var lineExtension = Money(
            doc.Root.Element(Cac + "LegalMonetaryTotal")!.Element(Cbc + "LineExtensionAmount"));

        lineSum.Should().Be(lineExtension);
    }

    [Fact]
    public void Build_OrderWithDiscount_TaxTotalStillUsesTheDiscountedNet()
    {
        var (order, invoice) = Fixture(discount: 30.00m, couponCode: "VARA30");

        var doc = BuildAndParse(order, invoice);

        var taxTotal = doc.Root!.Element(Cac + "TaxTotal")!;
        Money(taxTotal.Element(Cbc + "TaxAmount")).Should().Be(invoice.VatRon);
        Money(taxTotal.Element(Cac + "TaxSubtotal")!.Element(Cbc + "TaxableAmount"))
            .Should().Be(invoice.NetTotalRon);
    }

    [Fact]
    public void Build_OrderWithDiscountButNoCouponCode_StillNamesTheAllowance()
    {
        var (order, invoice) = Fixture(discount: 15.00m, couponCode: null);

        var doc = BuildAndParse(order, invoice);

        var allowance = doc.Root!.Elements(Cac + "AllowanceCharge").Single();
        allowance.Element(Cbc + "AllowanceChargeReason")!.Value.Should().NotBeNullOrWhiteSpace();
    }
}
