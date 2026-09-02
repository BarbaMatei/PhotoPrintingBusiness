using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Caller-supplied storage key policy for invoice PDFs.
/// The adapter (<c>IStorageService</c>) persists bytes at this exact key;
/// naming lives in the application layer.
/// </summary>
public static class InvoiceStorageKeys
{
    /// <summary>
    /// Returns the storage key for a rendered invoice PDF.
    /// Format: <c>invoices/yyyy/MM/{InvoiceNumber}.pdf</c>, derived from
    /// <see cref="Invoice.IssuedAt"/>. Year-month partitioning aids object
    /// lifecycle policies in S3/R2 and makes ad-hoc inspection scannable.
    /// </summary>
    public static string ForPdf(Invoice invoice)
    {
        var issued = invoice.IssuedAt.UtcDateTime;
        return $"invoices/{issued:yyyy}/{issued:MM}/{invoice.InvoiceNumber}.pdf";
    }
}
