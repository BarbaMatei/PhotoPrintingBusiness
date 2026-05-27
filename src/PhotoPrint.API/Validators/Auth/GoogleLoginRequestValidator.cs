using FluentValidation;
using PhotoPrint.API.DTOs.Auth;

namespace PhotoPrint.API.Validators.Auth;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage("Token-ul Google este obligatoriu.");
    }
}
