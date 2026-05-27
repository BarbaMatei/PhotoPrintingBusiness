using FluentValidation;
using PhotoPrint.API.DTOs.Cart;

namespace PhotoPrint.API.Validators.Cart;

public class CartMergeRequestValidator : AbstractValidator<CartMergeRequest>
{
    public CartMergeRequestValidator()
    {
        RuleFor(x => x.GuestSessionId)
            .NotEmpty().WithMessage("ID-ul sesiunii de oaspete este obligatoriu.");
    }
}
