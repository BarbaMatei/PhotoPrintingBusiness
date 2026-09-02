namespace PhotoPrint.API.DTOs.Invoices;

/// <summary>
/// Query-string params for the admin invoice list endpoint.
/// Validated by <c>AdminInvoiceListQueryValidator</c> via FluentValidation —
/// no <c>[Required]</c> data annotations.
/// </summary>
public sealed class AdminInvoiceListQuery
{
    /// <summary>Optional status filter — matches
    /// <see cref="Models.InvoiceAnafStatus"/> by name (case-insensitive).</summary>
    public string? Status { get; set; }

    /// <summary>1-based page index. Default 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Page size. Default 20, capped at 100.</summary>
    public int Size { get; set; } = 20;
}
