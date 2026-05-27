using FluentValidation;
using PhotoPrint.API.DTOs.Account;

namespace PhotoPrint.API.Validators.Account;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Parola curentă este obligatorie.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Parola nouă este obligatorie.")
            .MinimumLength(8).WithMessage("Parola trebuie să aibă cel puțin 8 caractere.")
            .Matches("[A-Z]").WithMessage("Parola trebuie să conțină cel puțin o literă mare.")
            .Matches("[0-9]").WithMessage("Parola trebuie să conțină cel puțin o cifră.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Parola trebuie să conțină cel puțin un caracter special.");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Parolele nu se potrivesc.");
    }
}
