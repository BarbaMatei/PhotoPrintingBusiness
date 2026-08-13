using FluentValidation.TestHelper;
using PhotoPrint.API.DTOs.Invoices;
using PhotoPrint.API.Validators.Invoices;
using Xunit;

namespace PhotoPrint.Tests.Unit.Validators;

public class AdminInvoiceListQueryValidatorTests
{
    private readonly AdminInvoiceListQueryValidator _sut = new();

    [Fact]
    public void Defaults_Pass()
    {
        _sut.TestValidate(new AdminInvoiceListQuery()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PageNearIntMax_FailsValidation()
    {
        var query = new AdminInvoiceListQuery { Page = int.MaxValue, Size = 100 };

        _sut.TestValidate(query).ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Fact]
    public void PageAtUpperBound_Passes()
    {
        var query = new AdminInvoiceListQuery { Page = 1_000_000, Size = 100 };

        _sut.TestValidate(query).ShouldNotHaveValidationErrorFor(q => q.Page);
    }

    [Fact]
    public void PageZero_FailsValidation()
    {
        var query = new AdminInvoiceListQuery { Page = 0 };

        _sut.TestValidate(query).ShouldHaveValidationErrorFor(q => q.Page);
    }
}
