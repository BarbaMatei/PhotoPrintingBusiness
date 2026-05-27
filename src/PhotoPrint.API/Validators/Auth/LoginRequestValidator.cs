using FluentValidation;
using PhotoPrint.API.DTOs.Auth;

namespace PhotoPrint.API.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Adresa de email este obligatorie.")
            .EmailAddress().WithMessage("Adresă de email invalidă.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola este obligatorie.");
    }
}
