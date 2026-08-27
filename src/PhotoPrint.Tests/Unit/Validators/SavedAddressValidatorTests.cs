using FluentValidation.TestHelper;
using PhotoPrint.API.DTOs.Account;
using PhotoPrint.API.Services.Invoicing;
using PhotoPrint.API.Validators.Account;
using Xunit;

namespace PhotoPrint.Tests.Unit.Validators;

public class SavedAddressValidatorTests
{
    private readonly SavedAddressValidator _sut = new();

    private static SavedAddressRequest WithCity(string city) => new(
        Label: "Acasă",
        FullName: "Ana Pop",
        Phone: "0712345678",
        AddressLine: "Str. Buyer 10",
        City: city,
        County: "Cluj",
        PostalCode: "400100",
        IsDefault: true);

    [Fact]
    public void City_longer_than_the_invoice_accepts_is_rejected_here_too()
    {
        var request = WithCity(new string('C', InvoiceAddressFormatter.CityNameMaxLength + 1));

        _sut.TestValidate(request).ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void City_at_the_invoice_limit_is_accepted()
    {
        var request = WithCity(new string('C', InvoiceAddressFormatter.CityNameMaxLength));

        _sut.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.City);
    }
}
