using FluentValidation;
using PhotoPrint.API.DTOs.Account;

namespace PhotoPrint.API.Validators.Account;

public class UpdateAccountValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Prenumele este obligatoriu.")
            .MaximumLength(100).WithMessage("Prenumele nu poate depăși 100 de caractere.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Numele de familie este obligatoriu.")
            .MaximumLength(100).WithMessage("Numele nu poate depăși 100 de caractere.");

        RuleFor(x => x.Phone)
            .Matches(@"^07[0-9]{8}$").WithMessage("Număr de telefon invalid (ex: 0712345678).")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}
