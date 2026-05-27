using FluentValidation;
using PhotoPrint.API.DTOs.Admin;

namespace PhotoPrint.API.Validators.Admin;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Numele produsului este obligatoriu.")
            .MaximumLength(200).WithMessage("Numele nu poate depăși 200 de caractere.");

        RuleFor(x => x.ProductType)
            .NotEmpty().WithMessage("Tipul produsului este obligatoriu.")
            .MaximumLength(50).WithMessage("Tipul produsului nu poate depăși 50 de caractere.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("URL-ul imaginii nu poate depăși 500 de caractere.")
            .When(x => x.ImageUrl is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Ordinea de sortare trebuie să fie ≥ 0.");

        RuleFor(x => x.Sizes)
            .NotEmpty().WithMessage("Produsul trebuie să aibă cel puțin o dimensiune.");

        RuleForEach(x => x.Sizes).SetValidator(new CreateProductSizeRequestValidator());
    }
}
