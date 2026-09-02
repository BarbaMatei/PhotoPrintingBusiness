using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Renders a customer-facing invoice PDF from <c>(Order, Invoice, Seller)</c>.
/// Implementation uses QuestPDF (no Chromium dependency).
/// </summary>
public interface IInvoicePdfRenderer
{
    /// <summary>
    /// Returns the PDF as a byte array. Locale fixed at <c>ro-RO</c> for
    /// number and date formatting. A4, single document (auto-overflow for
    /// many-line orders).
    /// </summary>
    byte[] Render(Order order, Invoice invoice, SellerSettings seller);
}
