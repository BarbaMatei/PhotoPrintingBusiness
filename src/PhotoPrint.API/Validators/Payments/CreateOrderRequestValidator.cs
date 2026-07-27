using FluentValidation;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Validators.Payments;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.PaymentProcessor).IsInEnum();
        RuleFor(x => x.DeliveryType).IsInEnum();

        When(x => x.DeliveryType == DeliveryType.Easybox, () =>
        {
            RuleFor(x => x.EasyboxLockerId).NotNull()
                .WithMessage("Locker ID is required for Easybox delivery");

            // The locker supplies the address, but Sameday still needs a person +
            // phone to notify — captured on the Easybox checkout step.
            RuleFor(x => x.ShippingAddress).NotNull()
                .WithMessage("Recipient name and phone are required for Easybox delivery");

            When(x => x.ShippingAddress != null, () =>
            {
                RuleFor(x => x.ShippingAddress!.RecipientName).NotEmpty().MaximumLength(255);
                RuleFor(x => x.ShippingAddress!.Phone).NotEmpty().MaximumLength(20)
                    .Matches(@"^[0-9+\s().\-]{6,20}$").WithMessage("Invalid phone number.");
            });
        });

        When(x => x.DeliveryType == DeliveryType.Courier, () =>
        {
            RuleFor(x => x.ShippingAddress).NotNull()
                .WithMessage("Shipping address is required for courier delivery");

            When(x => x.ShippingAddress != null, () =>
            {
                RuleFor(x => x.ShippingAddress!.RecipientName).NotEmpty().MaximumLength(255);
                RuleFor(x => x.ShippingAddress!.Phone).NotEmpty().MaximumLength(20)
                    .Matches(@"^[0-9+\s().\-]{6,20}$").WithMessage("Invalid phone number.");
                RuleFor(x => x.ShippingAddress!.Street).NotEmpty().MaximumLength(255);
                RuleFor(x => x.ShippingAddress!.Number).NotEmpty().MaximumLength(50);
                RuleFor(x => x.ShippingAddress!.City).NotEmpty().MaximumLength(100);
                RuleFor(x => x.ShippingAddress!.County).NotEmpty().MaximumLength(100);
                RuleFor(x => x.ShippingAddress!.PostalCode).NotEmpty().MaximumLength(20);
            });
        });
    }
}
