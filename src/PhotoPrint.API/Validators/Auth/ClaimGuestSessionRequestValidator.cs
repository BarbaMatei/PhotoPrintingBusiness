using FluentValidation;
using PhotoPrint.API.DTOs.Auth;

namespace PhotoPrint.API.Validators.Auth;

public class ClaimGuestSessionRequestValidator : AbstractValidator<ClaimGuestSessionRequest>
{
    public ClaimGuestSessionRequestValidator()
    {
        RuleFor(x => x.GuestToken)
            .NotEqual(Guid.Empty)
            .WithMessage("Token-ul sesiunii de oaspete este obligatoriu.");
    }
}
