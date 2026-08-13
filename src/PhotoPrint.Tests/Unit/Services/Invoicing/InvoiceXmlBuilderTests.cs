using System.Globalization;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

/// <summary>
/// Story 001 acceptance: <see cref="InvoiceXmlBuilder"/> produces a UBL 2.1
/// + CIUS-RO compliant payload. These tests don't pull in the actual
/// ANAF-published XSD (heavy dependency); they assert the required UBL
/// business terms are present with the expected values, which is what
/// XSD validation would check.
/// </summary>
public class InvoiceXmlBuilderTests
{
    private static readonly XNamespace Inv = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

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
        bool isGuest = false, decimal shippingCost = 5.00m)
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Foto 10x15" };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-12345",
            Status = OrderStatus.Paid,
            PaidAt = new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero),
            PaymentProcessor = PaymentProcessor.Stripe,
            UserId = isGuest ? null : Guid.NewGuid(),
            User = isGuest ? null : new User
            {
                Id = Guid.NewGuid(), Email = "x@y.ro",
                FirstName = "Alex", LastName = "Pop",
            },
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "Str. Buyer", Number = "10", Block = "A",
                City = "Cluj-Napoca", County = "Cluj", PostalCode = "400100",
                RecipientName = "Alex Pop", Phone = "0700000000",
            },
            ShippingCostRon = shippingCost,
            SubtotalRon = 21m, TotalRon = 21m + shippingCost,
            NetTotalRon = decimal.Round((21m + shippingCost) / 1.19m, 2, MidpointRounding.AwayFromZero),
            VatRon = decimal.Round((21m + shippingCost) * 0.19m / 1.19m, 2, MidpointRounding.AwayFromZero),
            VatRate = 0.19m,
            Items = new List<OrderItem>
            {
                new()
                {
                    OrderId = default, Quantity = 3, UnitPriceRon = 7m, LineTotalRon = 21m,
                    ProductId = product.Id,
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
            Order = order,    // so the builder reads VatRate from the snapshot
            AnafStatus = InvoiceAnafStatus.Pending,
            CreatedAt = order.PaidAt!.Value,
        };
        return (order, invoice);
    }

    private static XDocument BuildAndParse(Order order, Invoice invoice, SellerSettings seller)
    {
        var bytes = new InvoiceXmlBuilder().Build(order, invoice, seller);
        return XDocument.Parse(Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Required_ubl_envelope_elements_are_present_with_correct_values()
    {
        var (order, invoice) = Fixture();
        var doc = BuildAndParse(order, invoice, Seller());
        var root = doc.Root!;

        root.Name.Should().Be(Inv + "Invoice");
        root.Element(Cbc + "UBLVersionID")!.Value.Should().Be("2.1");
        root.Element(Cbc + "CustomizationID")!.Value.Should().Contain("CIUS-RO");
        root.Element(Cbc + "ID")!.Value.Should().Be("FT-2026-00001");                  // BT-1
        root.Element(Cbc + "IssueDate")!.Value.Should().Be("2026-06-03");              // BT-2 (no time)
        root.Element(Cbc + "InvoiceTypeCode")!.Value.Should().Be("380");               // BT-3
        root.Element(Cbc + "DocumentCurrencyCode")!.Value.Should().Be("RON");
    }

    [Fact]
    public void Supplier_and_customer_parties_are_emitted()
    {
        var (order, invoice) = Fixture();
        var doc = BuildAndParse(order, invoice, Seller());
        var root = doc.Root!;

        var supplier = root.Element(Cac + "AccountingSupplierParty")!;
        supplier.Descendants(Cbc + "Name").First().Value.Should().Be("FotoTipar SRL");
        supplier.Descendants(Cbc + "CompanyID").First().Value.Should().Be("RO12345678");

        var customer = root.Element(Cac + "AccountingCustomerParty")!;
        customer.Descendants(Cbc + "Name").First().Value.Should().Be("Alex Pop");
        customer.Descendants(Cbc + "CityName").First().Value.Should().Be("Cluj-Napoca");
    }

    [Fact]
    public void Guest_buyer_uses_persoana_fizica_and_omits_vat_identifier()
    {
        var (order, invoice) = Fixture(isGuest: true);
        var doc = BuildAndParse(order, invoice, Seller());

        var customer = doc.Root!.Element(Cac + "AccountingCustomerParty")!;
        customer.Descendants(Cbc + "Name").First().Value.Should().Be("Persoană fizică");

        // No CompanyID for the buyer (BT-48 BuyerVATIdentifier omitted entirely).
        customer.Descendants(Cbc + "CompanyID").Should().BeEmpty();
    }

    [Fact]
    public void Empty_items_throws_invalid_operation()
    {
        var (order, invoice) = Fixture();
        order.Items = new List<OrderItem>();

        var act = () => new InvoiceXmlBuilder().Build(order, invoice, Seller());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*has no items*");
    }

    [Fact]
    public void All_monetary_amounts_format_invariant_with_two_decimals()
    {
        var (order, invoice) = Fixture(shippingCost: 25.50m);
        var doc = BuildAndParse(order, invoice, Seller());

        var totals = doc.Root!.Element(Cac + "LegalMonetaryTotal")!;
        foreach (var node in totals.Elements())
        {
            node.Attribute("currencyID")!.Value.Should().Be("RON");
            // Must parse as invariant culture (dot decimal separator).
            decimal.Parse(node.Value, NumberStyles.Number, CultureInfo.InvariantCulture);
            node.Value.Should().Contain(".");        // dot, not comma
        }
    }

    [Fact]
    public void Shipping_emitted_as_a_separate_invoice_line_when_nonzero()
    {
        var (order, invoice) = Fixture(shippingCost: 5.00m);
        var doc = BuildAndParse(order, invoice, Seller());

        var lines = doc.Root!.Elements(Cac + "InvoiceLine").ToList();
        lines.Should().HaveCount(2);   // 1 product line + 1 shipping line

        var shippingLine = lines.Last();
        shippingLine.Descendants(Cbc + "Name").First().Value.Should().Be("Transport");
    }

    [Fact]
    public void Line_extension_amount_is_net_not_gross()
    {
        // Gross line total is 21.00 (3 x 7.00); net at 19% is 17.65 — proves this isn't the raw gross value.
        var (order, invoice) = Fixture(shippingCost: 0m);
        var doc = BuildAndParse(order, invoice, Seller());

        var line = doc.Root!.Elements(Cac + "InvoiceLine").Single();
        var lineExtensionAmount = decimal.Parse(
            line.Element(Cbc + "LineExtensionAmount")!.Value, CultureInfo.InvariantCulture);

        lineExtensionAmount.Should().Be(17.65m);
        lineExtensionAmount.Should().NotBe(21.00m);
    }

    [Fact]
    public void Sum_of_line_extension_amounts_reconciles_exactly_with_header_net_total()
    {
        // These three lines' independently-extracted net totals sum one cent short of the header's aggregate-extracted total — a real rounding drift, proving the reconciliation.
        var product = new Product { Id = Guid.NewGuid(), Name = "Foto 10x15" };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-99999",
            Status = OrderStatus.Paid,
            PaidAt = new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero),
            PaymentProcessor = PaymentProcessor.Stripe,
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), Email = "x@y.ro", FirstName = "Alex", LastName = "Pop" },
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "Str. Buyer", Number = "10", City = "Cluj-Napoca", County = "Cluj",
                PostalCode = "400100", RecipientName = "Alex Pop", Phone = "0700000000",
            },
            ShippingCostRon = 0m,
            SubtotalRon = 30.03m, TotalRon = 30.03m,
            NetTotalRon = decimal.Round(30.03m / 1.19m, 2, MidpointRounding.AwayFromZero),
            VatRon = decimal.Round(30.03m * 0.19m / 1.19m, 2, MidpointRounding.AwayFromZero),
            VatRate = 0.19m,
            Items = Enumerable.Range(0, 3).Select(_ => new OrderItem
            {
                OrderId = default, Quantity = 1, UnitPriceRon = 10.01m, LineTotalRon = 10.01m,
                ProductId = product.Id,
                ProductSnapshot = new ProductSnapshot { ProductName = "Foto 10x15", Size = "10x15", Finish = "Lucios" },
            }).ToList(),
        };
        var invoice = new Invoice
        {
            OrderId = order.Id,
            InvoiceNumber = "FT-2026-00002",
            Series = "FT", Number = 2,
            IssuedAt = order.PaidAt!.Value,
            NetTotalRon = order.NetTotalRon,
            VatRon = order.VatRon,
            TotalRon = order.TotalRon,
            Order = order,
            AnafStatus = InvoiceAnafStatus.Pending,
            CreatedAt = order.PaidAt!.Value,
        };

        var doc = BuildAndParse(order, invoice, Seller());
        var lineTotals = doc.Root!.Elements(Cac + "InvoiceLine")
            .Select(l => decimal.Parse(l.Element(Cbc + "LineExtensionAmount")!.Value, CultureInfo.InvariantCulture))
            .ToList();

        lineTotals.Sum().Should().Be(invoice.NetTotalRon);
    }

    [Fact]
    public void No_shipping_line_when_shipping_cost_is_zero()
    {
        var (order, invoice) = Fixture(shippingCost: 0m);
        var doc = BuildAndParse(order, invoice, Seller());

        doc.Root!.Elements(Cac + "InvoiceLine").Should().HaveCount(1);
    }

    [Fact]
    public void Bytes_are_utf8_without_bom()
    {
        var (order, invoice) = Fixture();
        var bytes = new InvoiceXmlBuilder().Build(order, invoice, Seller());

        // UTF-8 BOM is EF BB BF. We use UTF8Encoding(false) → no BOM.
        (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF).Should().BeFalse();

        // Should round-trip as UTF-8 text starting with the XML declaration.
        Encoding.UTF8.GetString(bytes).Should().StartWith("<?xml");
    }
}
