using System.Globalization;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

public class InvoiceDiscountMathTests
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
        decimal goods = 250.00m,
        decimal shippingCost = 19.99m,
        decimal discount = 0m,
        string? couponCode = null,
        bool attachOrderToInvoice = true)
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
            Order = attachOrderToInvoice ? order : null,
            AnafStatus = InvoiceAnafStatus.Pending,
            CreatedAt = order.PaidAt!.Value,
        };

        return (order, invoice);
    }

    private static decimal Money(XElement? element)
        => decimal.Parse(element!.Value, CultureInfo.InvariantCulture);

    [Fact]
    public void DiscountRows_OrderWithoutDiscount_AddsNothingToThePdf()
    {
        var (order, invoice) = Fixture();

        InvoiceDiscountMath.DiscountRows(order, invoice).Should().BeEmpty();
    }

    [Fact]
    public void DiscountRows_PercentCoupon_ShowTheNetLineTotalAndTheNegativeAllowance()
    {
        var (order, invoice) = Fixture(discount: 25.00m, couponCode: "VARA10");

        var rows = InvoiceDiscountMath.DiscountRows(order, invoice);

        rows.Should().HaveCount(2);
        rows[0].Label.Should().Contain("Total linii");
        rows[0].Amount.Should().Be(226.88m);
        rows[1].Label.Should().Be("Reducere VARA10:");
        rows[1].Amount.Should().Be(-21.01m);
    }

    [Fact]
    public void DiscountRows_AreTheNetAllowance_NotTheGrossDiscount()
    {
        var (order, invoice) = Fixture(discount: 25.00m, couponCode: "VARA10");

        var allowance = -InvoiceDiscountMath.DiscountRows(order, invoice)[1].Amount;

        allowance.Should().BeLessThan(order.DiscountRon);
        allowance.Should().Be(InvoiceDiscountMath.LineNetTotal(order, invoice) - invoice.NetTotalRon);
    }

    [Fact]
    public void DiscountRows_ReconcileWithTheVatBaseThePdfPrintsBelowThem()
    {
        var (order, invoice) = Fixture(discount: 25.00m, couponCode: "VARA10");

        var rows = InvoiceDiscountMath.DiscountRows(order, invoice);

        rows.Sum(r => r.Amount).Should().Be(invoice.NetTotalRon);
        (invoice.NetTotalRon + invoice.VatRon).Should().Be(invoice.TotalRon);
    }

    [Fact]
    public void DiscountRows_MatchTheAllowanceNumbersFiledWithAnaf()
    {
        var (order, invoice) = Fixture(discount: 25.00m, couponCode: "VARA10");

        var doc = XDocument.Parse(Encoding.UTF8.GetString(
            new InvoiceXmlBuilder().Build(order, invoice, Seller())));
        var totals = doc.Root!.Element(Cac + "LegalMonetaryTotal")!;
        var rows = InvoiceDiscountMath.DiscountRows(order, invoice);

        rows[0].Amount.Should().Be(Money(totals.Element(Cbc + "LineExtensionAmount")));
        (-rows[1].Amount).Should().Be(Money(totals.Element(Cbc + "AllowanceTotalAmount")));
        invoice.NetTotalRon.Should().Be(Money(totals.Element(Cbc + "TaxExclusiveAmount")));
        invoice.TotalRon.Should().Be(Money(totals.Element(Cbc + "PayableAmount")));
    }

    [Fact]
    public void AllowanceReason_WithoutACouponCode_UsesTheGenericWording()
    {
        InvoiceDiscountMath.AllowanceReason(null)
            .Should().Be(InvoiceDiscountMath.GenericAllowanceReason);
        InvoiceDiscountMath.AllowanceReason("   ")
            .Should().Be(InvoiceDiscountMath.GenericAllowanceReason);
        InvoiceDiscountMath.AllowanceReason("VARA10").Should().Be("Reducere VARA10");
    }

    [Fact]
    public void LineNetTotal_WithoutDiscount_IsTheInvoiceNetItself()
    {
        var (order, invoice) = Fixture();

        InvoiceDiscountMath.LineNetTotal(order, invoice).Should().Be(invoice.NetTotalRon);
    }

    [Fact]
    public void VatRateFromInvoice_DetachedInvoice_DerivesTheRateFromItsOwnTotals()
    {
        var (_, invoice) = Fixture(discount: 25.00m, couponCode: "VARA10", attachOrderToInvoice: false);

        var rate = InvoiceDiscountMath.VatRateFromInvoice(invoice);

        rate.Should().BeApproximately(Rate, 0.001m);
    }

    [Fact]
    public void VatRateFromInvoice_ZeroNetInvoice_IsZeroInsteadOfDividingByZero()
    {
        var invoice = new Invoice
        {
            OrderId = Guid.NewGuid(),
            InvoiceNumber = "FT-2026-00002",
            Series = "FT", Number = 2,
            IssuedAt = DateTimeOffset.UtcNow,
            NetTotalRon = 0m, VatRon = 0m, TotalRon = 0m,
            AnafStatus = InvoiceAnafStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        InvoiceDiscountMath.VatRateFromInvoice(invoice).Should().Be(0m);
    }
}
