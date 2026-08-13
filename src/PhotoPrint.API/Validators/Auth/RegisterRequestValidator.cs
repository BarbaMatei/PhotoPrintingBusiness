using FluentValidation;
using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Validators;

namespace PhotoPrint.API.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Prenumele este obligatoriu.")
            .MaximumLength(100).WithMessage("Prenumele nu poate depăși 100 de caractere.")
            .Must(TextValidation.HasNoXmlInvalidChars).WithMessage("Prenumele conține caractere nevalide.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Numele de familie este obligatoriu.")
            .MaximumLength(100).WithMessage("Numele nu poate depăși 100 de caractere.")
            .Must(TextValidation.HasNoXmlInvalidChars).WithMessage("Numele conține caractere nevalide.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Adresa de email este obligatorie.")
            .EmailAddress().WithMessage("Adresă de email invalidă.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola este obligatorie.")
            .MinimumLength(8).WithMessage("Parola trebuie să aibă cel puțin 8 caractere.")
            .Matches("[A-Z]").WithMessage("Parola trebuie să conțină cel puțin o literă mare.")
            .Matches("[0-9]").WithMessage("Parola trebuie să conțină cel puțin o cifră.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Parola trebuie să conțină cel puțin un caracter special.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Parolele nu se potrivesc.");

        RuleFor(x => x.Phone)
            .Matches(@"^07[0-9]{8}$").WithMessage("Număr de telefon invalid (ex: 0712345678).")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.GdprConsentAccepted)
            .Equal(true).WithMessage("Trebuie să acceptați prelucrarea datelor personale.");
    }
}
