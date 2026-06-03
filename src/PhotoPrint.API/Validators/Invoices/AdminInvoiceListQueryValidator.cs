using FluentValidation;
using PhotoPrint.API.DTOs.Invoices;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Validators.Invoices;

public sealed class AdminInvoiceListQueryValidator : AbstractValidator<AdminInvoiceListQuery>
{
    public AdminInvoiceListQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("page must be ≥ 1.");

        RuleFor(q => q.Size)
            .InclusiveBetween(1, 100)
            .WithMessage("size must be between 1 and 100.");

        RuleFor(q => q.Status)
            .Must(s => s is null || Enum.TryParse<InvoiceAnafStatus>(s, ignoreCase: true, out _))
            .WithMessage("status must be one of: Pending, Submitted, Accepted, Rejected, Failed.");
    }
}
