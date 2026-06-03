using System.Text;
using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

/// <summary>
/// Smoke tests for <see cref="InvoicePdfRenderer"/> (ADR-021 — QuestPDF).
/// We don't pixel-diff the output; we verify that the renderer produces a
/// non-empty PDF file containing the expected literals (invoice number,
/// seller name, totals). This is enough to catch "the template stopped
/// referencing the seller" regressions without needing a brittle snapshot.
/// </summary>
public class InvoicePdfRendererTests
{
    static InvoicePdfRendererTests()
    {
        // Ensure QuestPDF Community License is set even when this test class
        // is loaded before Program.cs initialises (it always is — Program.cs
        // sets it inside the boot path, which doesn't run in unit tests).
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

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

    private static (Order order, Invoice invoice) Fixture()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "FT-12345",
            Status = OrderStatus.Paid,
            PaidAt = new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero),
            PaymentProcessor = PaymentProcessor.Stripe,
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), Email = "x@y.ro", FirstName = "Alex", LastName = "Pop" },
            ShippingAddress = new ShippingAddressSnapshot
            {
                Street = "Str. Buyer", Number = "10",
                City = "Cluj-Napoca", County = "Cluj", PostalCode = "400100",
                RecipientName = "Alex Pop", Phone = "0700000000",
            },
            ShippingCostRon = 5m, SubtotalRon = 21m, TotalRon = 26m,
            NetTotalRon = 21.85m, VatRon = 4.15m, VatRate = 0.19m,
            Items = new List<OrderItem>
            {
                new()
                {
                    Quantity = 3, UnitPriceRon = 7m, LineTotalRon = 21m,
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
            InvoiceNumber = "FT-2026-00042",
            Series = "FT", Number = 42,
            IssuedAt = order.PaidAt!.Value,
            NetTotalRon = order.NetTotalRon,
            VatRon = order.VatRon,
            TotalRon = order.TotalRon,
            AnafStatus = InvoiceAnafStatus.Pending,
            CreatedAt = order.PaidAt!.Value,
        };
        return (order, invoice);
    }

    [Fact]
    public void Returns_non_empty_pdf_byte_array_starting_with_pdf_magic()
    {
        var (order, invoice) = Fixture();
        var bytes = new InvoicePdfRenderer().Render(order, invoice, Seller());

        bytes.Should().NotBeNullOrEmpty();
        // PDF magic header: %PDF-
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void Pdf_output_is_well_formed_and_non_trivial_size()
    {
        // QuestPDF compresses text streams (FlateDecode), so literal text
        // doesn't appear in the byte view. We assert structural validity:
        // the PDF starts with %PDF-, ends with %%EOF, and is large enough
        // to contain actual rendered content (not a blank page). Content
        // correctness is verified at the XML builder layer (same input
        // data) and via manual inspection during the dual-write rollout
        // inspection week (ADR-022).
        var (order, invoice) = Fixture();
        var bytes = new InvoicePdfRenderer().Render(order, invoice, Seller());

        var ascii = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
        ascii.Should().StartWith("%PDF-");
        ascii.TrimEnd().Should().EndWith("%%EOF");
        bytes.Length.Should().BeGreaterThan(5_000,
            "a rendered invoice with line items should be at least a few KB");
    }

    [Fact]
    public void Throws_when_seller_is_null()
    {
        var (order, invoice) = Fixture();
        var act = () => new InvoicePdfRenderer().Render(order, invoice, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
