using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;
using QuestPDF.Fluent;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Thin façade over <see cref="InvoicePdfDocument"/>. Exists so consumers
/// (worker, integration tests) depend on the interface, not the document.
/// </summary>
public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    public byte[] Render(Order order, Invoice invoice, SellerSettings seller)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(seller);

        // Not inside the document tree: QuestPDF wraps anything thrown in compose, and the worker keys parking on this type.
        InvoiceAddressFormatter.EnsureBuyerAddressUsable(order);

        var doc = new InvoicePdfDocument(order, invoice, seller);
        return doc.GeneratePdf();
    }
}
