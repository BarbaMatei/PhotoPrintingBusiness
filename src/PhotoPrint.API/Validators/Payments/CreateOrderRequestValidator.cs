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
        });

        When(x => x.DeliveryType == DeliveryType.Courier, () =>
        {
            RuleFor(x => x.ShippingAddress).NotNull()
                .WithMessage("Shipping address is required for courier delivery");

            When(x => x.ShippingAddress != null, () =>
            {
                RuleFor(x => x.ShippingAddress!.City).NotEmpty();
                RuleFor(x => x.ShippingAddress!.County).NotEmpty();
                RuleFor(x => x.ShippingAddress!.PostalCode).NotEmpty();
            });
        });
    }
}
