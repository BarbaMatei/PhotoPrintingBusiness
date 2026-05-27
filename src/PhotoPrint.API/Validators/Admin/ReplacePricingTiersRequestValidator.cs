using FluentValidation;
using PhotoPrint.API.DTOs.Admin;

namespace PhotoPrint.API.Validators.Admin;

public class ReplacePricingTiersRequestValidator : AbstractValidator<ReplacePricingTiersRequest>
{
    public ReplacePricingTiersRequestValidator()
    {
        RuleFor(x => x.Tiers)
            .NotEmpty().WithMessage("Lista de niveluri de prețuri nu poate fi goală.");

        RuleForEach(x => x.Tiers).ChildRules(tier =>
        {
            tier.RuleFor(t => t.MinQuantity)
                .GreaterThanOrEqualTo(1).WithMessage("Cantitatea minimă trebuie să fie ≥ 1.");

            tier.RuleFor(t => t.MaxQuantity)
                .GreaterThanOrEqualTo(t => t.MinQuantity)
                .WithMessage("Cantitatea maximă trebuie să fie ≥ cantitatea minimă.")
                .When(t => t.MaxQuantity.HasValue);

            tier.RuleFor(t => t.UnitPrice)
                .GreaterThan(0).WithMessage("Prețul unitar trebuie să fie > 0.")
                .PrecisionScale(10, 2, false).WithMessage("Prețul unitar poate avea cel mult 2 zecimale.");
        });
    }
}
