using FluentValidation.TestHelper;
using PhotoPrint.API.DTOs.Account;
using PhotoPrint.API.Validators.Account;
using Xunit;

namespace PhotoPrint.Tests.Unit.Validators;

public class UpdateAccountValidatorTests
{
    private readonly UpdateAccountValidator _sut = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new UpdateAccountRequest(FirstName: "Ana", LastName: "Pop", Phone: null);

        _sut.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FirstNameContainsXmlInvalidControlChar_FailsValidation()
    {
        var controlChar = ((char)1).ToString();
        var request = new UpdateAccountRequest(FirstName: "Ana" + controlChar, LastName: "Pop", Phone: null);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void LastNameContainsXmlInvalidControlChar_FailsValidation()
    {
        var controlChar = ((char)1).ToString();
        var request = new UpdateAccountRequest(FirstName: "Ana", LastName: "Pop" + controlChar, Phone: null);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor(x => x.LastName);
    }
}
