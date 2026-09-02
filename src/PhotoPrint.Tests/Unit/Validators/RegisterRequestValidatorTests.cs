using FluentValidation.TestHelper;
using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Validators.Auth;
using Xunit;

namespace PhotoPrint.Tests.Unit.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    private static RegisterRequest ValidRequest(string firstName = "Ana", string lastName = "Pop") => new(
        FirstName: firstName,
        LastName: lastName,
        Email: "ana@example.com",
        Password: "Passw0rd!",
        ConfirmPassword: "Passw0rd!",
        Phone: null,
        GdprConsentAccepted: true);

    [Fact]
    public void ValidRequest_Passes()
    {
        _sut.TestValidate(ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FirstNameContainsXmlInvalidControlChar_FailsValidation()
    {
        var controlChar = ((char)1).ToString();

        _sut.TestValidate(ValidRequest(firstName: "Ana" + controlChar))
            .ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void LastNameContainsXmlInvalidControlChar_FailsValidation()
    {
        var controlChar = ((char)1).ToString();

        _sut.TestValidate(ValidRequest(lastName: "Pop" + controlChar))
            .ShouldHaveValidationErrorFor(x => x.LastName);
    }
}
