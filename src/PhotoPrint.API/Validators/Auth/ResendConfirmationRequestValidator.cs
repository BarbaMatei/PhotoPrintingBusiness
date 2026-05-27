using FluentValidation;
using PhotoPrint.API.DTOs.Auth;

namespace PhotoPrint.API.Validators.Auth;

public class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
{
    public ResendConfirmationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Adresa de email este obligatorie.")
            .EmailAddress().WithMessage("Adresă de email invalidă.");
    }
}
