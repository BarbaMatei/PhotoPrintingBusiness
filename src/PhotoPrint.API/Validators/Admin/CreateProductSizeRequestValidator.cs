using FluentValidation;
using PhotoPrint.API.DTOs.Admin;

namespace PhotoPrint.API.Validators.Admin;

public class CreateProductSizeRequestValidator : AbstractValidator<CreateProductSizeRequest>
{
    public CreateProductSizeRequestValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Eticheta dimensiunii este obligatorie.")
            .MaximumLength(50).WithMessage("Eticheta nu poate depăși 50 de caractere.");

        RuleFor(x => x.WidthMm)
            .GreaterThanOrEqualTo(1).WithMessage("Lățimea trebuie să fie ≥ 1 mm.");

        RuleFor(x => x.HeightMm)
            .GreaterThanOrEqualTo(1).WithMessage("Înălțimea trebuie să fie ≥ 1 mm.");
    }
}
