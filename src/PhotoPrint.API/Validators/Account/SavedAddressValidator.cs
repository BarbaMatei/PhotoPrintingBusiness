using FluentValidation;
using PhotoPrint.API.DTOs.Account;

namespace PhotoPrint.API.Validators.Account;

public class SavedAddressValidator : AbstractValidator<SavedAddressRequest>
{
    public SavedAddressValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Eticheta adresei este obligatorie.")
            .MaximumLength(100).WithMessage("Eticheta nu poate depăși 100 de caractere.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Numele complet este obligatoriu.")
            .MaximumLength(200).WithMessage("Numele nu poate depăși 200 de caractere.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Numărul de telefon este obligatoriu.")
            .Matches(@"^07[0-9]{8}$").WithMessage("Număr de telefon invalid (ex: 0712345678).");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Adresa este obligatorie.")
            .MaximumLength(400).WithMessage("Adresa nu poate depăși 400 de caractere.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Localitatea este obligatorie.")
            .MaximumLength(100).WithMessage("Localitatea nu poate depăși 100 de caractere.");

        RuleFor(x => x.County)
            .NotEmpty().WithMessage("Județul este obligatoriu.")
            .MaximumLength(100).WithMessage("Județul nu poate depăși 100 de caractere.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Codul poștal este obligatoriu.")
            .Matches(@"^\d{6}$").WithMessage("Codul poștal trebuie să aibă 6 cifre.");
    }
}
