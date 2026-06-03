using System.Globalization;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// QuestPDF document tree for a Romanian invoice (ADR-021). One <c>.cs</c>
/// file replaces the originally-planned Razor template — operationally
/// simpler, no Chromium dependency, deterministic byte output across hosts.
///
/// Locale: <c>ro-RO</c> for date and number formatting (e.g. <c>1.234,56</c>).
/// </summary>
public sealed class InvoicePdfDocument : IDocument
{
    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    private readonly Order _order;
    private readonly Invoice _invoice;
    private readonly SellerSettings _seller;

    public InvoicePdfDocument(Order order, Invoice invoice, SellerSettings seller)
    {
        _order = order;
        _invoice = invoice;
        _seller = seller;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("FACTURĂ").FontSize(20).Bold();
                col.Item().Text(_invoice.InvoiceNumber).FontSize(14).Bold();
                col.Item().Text($"Data emiterii: {_invoice.IssuedAt.ToString("dd MMMM yyyy", Ro)}");
            });
            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text(_seller.Name).Bold();
                col.Item().Text($"CUI: {_seller.Cui}");
                col.Item().Text($"Nr. ord. reg.: {_seller.RegistrationNumber}");
                col.Item().Text(_seller.Address.Line1);
                col.Item().Text($"{_seller.Address.PostalCode} {_seller.Address.City}, {_seller.Address.CountryCode}");
                if (!string.IsNullOrWhiteSpace(_seller.IbanRon))
                    col.Item().Text($"IBAN: {_seller.IbanRon}");
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(15).Column(col =>
        {
            col.Item().PaddingBottom(8).Element(ComposeBuyer);
            col.Item().PaddingBottom(8).Element(ComposeLines);
            col.Item().PaddingTop(10).Element(ComposeTotals);
        });
    }

    private void ComposeBuyer(IContainer container)
    {
        var isGuest = _order.UserId is null && _order.User is null;
        var buyerName = isGuest
            ? "Persoană fizică"
            : ($"{_order.User?.FirstName} {_order.User?.LastName}".Trim() is { Length: > 0 } full
                ? full
                : (_order.ShippingAddress?.RecipientName ?? "Persoană fizică"));

        var addr = _order.ShippingAddress;

        container.Border(0.5f).Padding(8).Column(col =>
        {
            col.Item().Text("Cumpărător").SemiBold();
            col.Item().Text(buyerName);
            if (addr is not null)
            {
                var line1 = string.Join(' ',
                    new[] { addr.Street, addr.Number, addr.Block }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                col.Item().Text(line1);
                col.Item().Text($"{addr.PostalCode} {addr.City}, {addr.County}");
            }
        });
    }

    private void ComposeLines(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(25);    // #
                c.RelativeColumn(4);     // description
                c.ConstantColumn(35);    // qty
                c.ConstantColumn(70);    // unit price
                c.ConstantColumn(70);    // line total
            });

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("#");
                h.Cell().Element(HeaderCell).Text("Descriere");
                h.Cell().Element(HeaderCell).AlignRight().Text("Cant.");
                h.Cell().Element(HeaderCell).AlignRight().Text("Preț unit.");
                h.Cell().Element(HeaderCell).AlignRight().Text("Total");
            });

            var idx = 1;
            foreach (var item in _order.Items)
            {
                AddLine(table,
                    idx++,
                    $"{item.ProductSnapshot.ProductName} ({item.ProductSnapshot.Size}, {item.ProductSnapshot.Finish})",
                    item.Quantity,
                    item.UnitPriceRon,
                    item.LineTotalRon);
            }

            if (_order.ShippingCostRon > 0)
                AddLine(table, idx, "Transport", 1, _order.ShippingCostRon, _order.ShippingCostRon);
        });

        static IContainer HeaderCell(IContainer c) => c.BorderBottom(1).PaddingVertical(3);
    }

    private static void AddLine(
        TableDescriptor table, int idx, string description, int qty,
        decimal unit, decimal total)
    {
        table.Cell().Element(BodyCell).Text(idx.ToString(Ro));
        table.Cell().Element(BodyCell).Text(description);
        table.Cell().Element(BodyCell).AlignRight().Text(qty.ToString(Ro));
        table.Cell().Element(BodyCell).AlignRight().Text(unit.ToString("N2", Ro));
        table.Cell().Element(BodyCell).AlignRight().Text(total.ToString("N2", Ro));

        static IContainer BodyCell(IContainer c) => c.PaddingVertical(3);
    }

    private void ComposeTotals(IContainer container)
    {
        container.AlignRight().Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().AlignRight().Text("Total net:");
                r.ConstantItem(80).AlignRight().Text($"{_invoice.NetTotalRon.ToString("N2", Ro)} RON");
            });
            col.Item().Row(r =>
            {
                r.RelativeItem().AlignRight().Text($"TVA ({(_order.VatRate * 100m).ToString("N0", Ro)}%):");
                r.ConstantItem(80).AlignRight().Text($"{_invoice.VatRon.ToString("N2", Ro)} RON");
            });
            col.Item().PaddingTop(3).Row(r =>
            {
                r.RelativeItem().AlignRight().Text("Total de plată:").Bold();
                r.ConstantItem(80).AlignRight().Text($"{_invoice.TotalRon.ToString("N2", Ro)} RON").Bold();
            });
            col.Item().PaddingTop(5).Text($"Plată: {_order.PaymentProcessor}").FontSize(9);
            if (!string.IsNullOrWhiteSpace(_order.AwbNumber))
                col.Item().Text($"AWB: {_order.AwbNumber}").FontSize(9);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text("Document generat electronic, valid fără semnătură.")
            .FontSize(8).Italic();
    }
}
