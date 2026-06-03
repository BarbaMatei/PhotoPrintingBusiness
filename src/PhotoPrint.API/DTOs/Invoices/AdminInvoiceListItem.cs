namespace PhotoPrint.API.DTOs.Invoices;

/// <summary>
/// Row shape for the admin invoice list endpoint (story 004).
/// One row per <see cref="Models.Invoice"/>; the join onto <c>Orders</c>
/// pulls in the human-readable order number for at-a-glance scanning.
/// </summary>
public sealed record AdminInvoiceListItem(
    Guid InvoiceId,
    Guid OrderId,
    string OrderNumber,
    string InvoiceNumber,
    DateTimeOffset IssuedAt,
    string AnafStatus,
    string? AnafUploadId,
    string? LastError);
