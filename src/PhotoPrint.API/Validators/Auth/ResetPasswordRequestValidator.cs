using FluentValidation;
using PhotoPrint.API.DTOs.Auth;

namespace PhotoPrint.API.Validators.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID utilizator invalid.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token-ul de resetare este obligatoriu.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Parola nouă este obligatorie.")
            .MinimumLength(8).WithMessage("Parola trebuie să aibă cel puțin 8 caractere.")
            .Matches("[A-Z]").WithMessage("Parola trebuie să conțină cel puțin o literă mare.")
            .Matches("[0-9]").WithMessage("Parola trebuie să conțină cel puțin o cifră.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Parola trebuie să conțină cel puțin un caracter special.");
    }
}
