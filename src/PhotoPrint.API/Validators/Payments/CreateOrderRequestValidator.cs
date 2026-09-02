using System.Linq;
using FluentValidation;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Invoicing;

namespace PhotoPrint.API.Validators.Payments;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    private const string PhoneCharset = @"^[0-9+\s().\-]{6,20}$";

    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.DeliveryType).IsInEnum();

        When(x => x.DeliveryType == DeliveryType.Easybox, () =>
        {
            RuleFor(x => x.EasyboxLockerId).NotNull()
                .WithMessage("Locker ID is required for Easybox delivery");

            RuleFor(x => x.ShippingAddress).NotNull()
                .WithMessage("Shipping address is required for Easybox delivery");

            When(x => x.ShippingAddress != null, AddAddressRules);
        });

        When(x => x.DeliveryType == DeliveryType.Courier, () =>
        {
            RuleFor(x => x.ShippingAddress).NotNull()
                .WithMessage("Shipping address is required for courier delivery");

            When(x => x.ShippingAddress != null, AddAddressRules);
        });
    }

    // One rule set both ways: the invoice embeds this as the buyer's address, so a laxer set for lockers cannot be invoiced.
    private void AddAddressRules()
    {
        AddRecipientRules();
        RuleFor(x => x.ShippingAddress!.Street).NotEmpty().MaximumLength(255).Must(TextValidation.HasNoXmlInvalidChars);
        RuleFor(x => x.ShippingAddress!.Number).NotEmpty().MaximumLength(50).Must(TextValidation.HasNoXmlInvalidChars);
        RuleFor(x => x.ShippingAddress!.Block).MaximumLength(100).Must(TextValidation.HasNoXmlInvalidChars);
        RuleFor(x => x.ShippingAddress!.City).NotEmpty().MaximumLength(InvoiceAddressFormatter.CityNameMaxLength).Must(TextValidation.HasNoXmlInvalidChars);
        RuleFor(x => x.ShippingAddress!.County).NotEmpty().MaximumLength(100).Must(TextValidation.HasNoXmlInvalidChars);
        RuleFor(x => x.ShippingAddress!.PostalCode).NotEmpty().MaximumLength(20).Must(TextValidation.HasNoXmlInvalidChars);
        AddCombinedStreetNameRule();
    }

    private void AddCombinedStreetNameRule() =>
        RuleFor(x => x.ShippingAddress)
            .Must(a => InvoiceAddressFormatter
                .FormatStreetName(a!.Street, a.Number, a.Block).Length <= InvoiceAddressFormatter.StreetNameMaxLength)
            .WithMessage($"Strada, numărul și blocul combinate nu pot depăși {InvoiceAddressFormatter.StreetNameMaxLength} de caractere.")
            .OverridePropertyName("ShippingAddress.Street");

    private void AddRecipientRules()
    {
        RuleFor(x => x.ShippingAddress!.RecipientName).NotEmpty().MaximumLength(InvoiceAddressFormatter.PartyNameMaxLength).Must(TextValidation.HasNoXmlInvalidChars);
        RuleFor(x => x.ShippingAddress!.Phone).NotEmpty().MaximumLength(20)
            .Matches(PhoneCharset).WithMessage("Invalid phone number.")
            .Must(HasEnoughDigits).WithMessage("Invalid phone number.");
    }

    // Charset+length alone accepts digit-poor junk ("1-2-3-4") that Sameday later rejects; require
    // a realistic Romanian phone digit count.
    private static bool HasEnoughDigits(string? phone) =>
        phone is not null && phone.Count(char.IsDigit) is >= 9 and <= 15;
}
