using FluentValidation;
using PhotoPrint.API.DTOs.Auth;

namespace PhotoPrint.API.Validators.Auth;

public class CreateGuestSessionRequestValidator : AbstractValidator<CreateGuestSessionRequest>
{
    public CreateGuestSessionRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Prenumele este obligatoriu.")
            .MaximumLength(100).WithMessage("Prenumele nu poate depăși 100 de caractere.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Numele de familie este obligatoriu.")
            .MaximumLength(100).WithMessage("Numele de familie nu poate depăși 100 de caractere.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Adresa de email este obligatorie.")
            .EmailAddress().WithMessage("Adresa de email nu este validă.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Numărul de telefon este obligatoriu.")
            .Matches(@"^07[0-9]{8}$")
            .WithMessage("Numărul de telefon trebuie să fie în format românesc (07XXXXXXXX).");
    }
}
