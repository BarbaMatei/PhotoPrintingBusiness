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
    public void Buyer_name_over_the_cius_ro_party_name_limit_is_truncated()
    {
        var (order, invoice) = Fixture();
        order.User!.FirstName = new string('a', 100);
        order.User!.LastName  = new string('b', 100);   // combined = 201 chars, over the 200-char PartyName limit

        var doc = BuildAndParse(order, invoice, Seller());

        var customer = doc.Root!.Element(Cac + "AccountingCustomerParty")!;
        var name = customer.Descendants(Cbc + "Name").First().Value;
        name.Length.Should().Be(200);
    }

    [Fact]
    public void Street_number_block_combined_over_the_cius_ro_street_name_limit_is_truncated()
    {
        var (order, invoice) = Fixture();
        order.ShippingAddress!.Street = new string('s', 100);
        order.ShippingAddress!.Number = "1";
        order.ShippingAddress!.Block  = new string('b', 60);   // combined = 163 chars, over the 150-char StreetName limit

        var doc = BuildAndParse(order, invoice, Seller());

        var streetName = doc.Root!.Element(Cac + "AccountingCustomerParty")!
            .Descendants(Cbc + "StreetName").First().Value;
        streetName.Length.Should().Be(150);
    }

    [Fact]
    public void City_over_the_cius_ro_city_name_limit_is_truncated()
    {
        var (order, invoice) = Fixture();
        order.ShippingAddress!.City = new string('c', 60);   // over the 50-char CityName limit

        var doc = BuildAndParse(order, invoice, Seller());

        var cityName = doc.Root!.Element(Cac + "AccountingCustomerParty")!
            .Descendants(Cbc + "CityName").First().Value;
        cityName.Length.Should().Be(50);
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

    // Refusing beats filing a document whose three mandatory buyer address elements are empty.
    [Fact]
    public void Locker_order_with_a_contact_only_snapshot_is_refused_rather_than_emitted_blank()
    {
        var (order, invoice) = Fixture();
        order.DeliveryType = DeliveryType.Easybox;
        order.ShippingAddress!.Street = "";
        order.ShippingAddress!.Number = "";
        order.ShippingAddress!.Block = null;
        order.ShippingAddress!.City = "";
        order.ShippingAddress!.PostalCode = "";

        var act = () => new InvoiceXmlBuilder().Build(order, invoice, Seller());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*StreetName, CityName, PostalZone*");
    }

    [Fact]
    public void Guest_locker_order_is_refused_for_the_same_reason()
    {
        var (order, invoice) = Fixture(isGuest: true);
        order.ShippingAddress!.Street = "";
        order.ShippingAddress!.City = "";
        order.ShippingAddress!.PostalCode = "";

        var act = () => new InvoiceXmlBuilder().Build(order, invoice, Seller());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void All_null_address_fields_are_refused_and_do_not_crash()
    {
        // OrderService substitutes an empty snapshot when the request omits one, leaving every field null.
        var (order, invoice) = Fixture();
        order.ShippingAddress = new ShippingAddressSnapshot();

        var act = () => new InvoiceXmlBuilder().Build(order, invoice, Seller());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*mandatory*");
    }

    // The worker keys its parking on this type: a plain InvalidOperationException keeps retrying.
    [Fact]
    public void A_missing_buyer_address_is_refused_as_not_buildable()
    {
        var (order, invoice) = Fixture();
        order.DeliveryType = DeliveryType.Easybox;
        order.ShippingAddress = new ShippingAddressSnapshot
        {
            RecipientName = "Ana Pop", Phone = "0712345678",
            Street = "", Number = "", City = "", County = "", PostalCode = "",
        };

        var act = () => new InvoiceXmlBuilder().Build(order, invoice, Seller());

        act.Should().Throw<InvoiceNotBuildableException>();
    }

    [Fact]
    public void An_order_with_no_items_is_refused_as_not_buildable_too()
    {
        var (order, invoice) = Fixture();
        order.Items.Clear();

        var act = () => new InvoiceXmlBuilder().Build(order, invoice, Seller());

        act.Should().Throw<InvoiceNotBuildableException>();
    }

    [Fact]
    public void A_complete_address_still_builds()
    {
        var (order, invoice) = Fixture();

        var doc = BuildAndParse(order, invoice, Seller());

        var addr = doc.Root!.Element(Cac + "AccountingCustomerParty")!.Descendants(Cac + "PostalAddress").First();
        addr.Element(Cbc + "StreetName")!.Value.Should().NotBeEmpty();
        addr.Element(Cbc + "CityName")!.Value.Should().NotBeEmpty();
        addr.Element(Cbc + "PostalZone")!.Value.Should().NotBeEmpty();
    }
    [Fact]
    public void Product_name_pasted_from_a_word_processor_still_parses()
    {
        var (order, invoice) = Fixture();
        // U+000B is Word's manual line break: XmlWriter emits it as a character reference no parser accepts.
        order.Items.First().ProductSnapshot.ProductName = "TablouCanvas";

        var doc = BuildAndParse(order, invoice, Seller());

        // Parsing at all is half the proof: before the guard, XmlWriter emitted a reference no parser accepts.
        var names = doc.Descendants().Where(e => e.Name.LocalName == "Name").Select(e => e.Value).ToList();
        names.Should().Contain(n => n.Contains("Tablou") && n.Contains("Canvas"));
        doc.ToString().Should().NotContain("");
    }

    [Fact]
    public void Seller_and_buyer_names_drop_xml_invalid_characters()
    {
        var (order, invoice) = Fixture();
        order.ShippingAddress!.RecipientName = "Ion Popescu";

        var doc = BuildAndParse(order, invoice, Seller());

        doc.ToString().Should().NotContain("");
    }
}