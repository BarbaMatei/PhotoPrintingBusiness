using FluentValidation;
using PhotoPrint.API.DTOs.Cart;

namespace PhotoPrint.API.Validators.Cart;

public class CartRequestValidator : AbstractValidator<CartRequest>
{
    private const int MaxItems = 100;
    private const int MinQuantity = 1;
    private const int MaxQuantity = 100;

    public CartRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Produsul este obligatoriu.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Lista de articole este obligatorie.")
            .Must(items => items.Count <= MaxItems)
            .WithMessage($"Coșul nu poate conține mai mult de {MaxItems} articole.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.UploadId)
                .NotEmpty().WithMessage("ID-ul fotografiei este obligatoriu.");

            item.RuleFor(i => i.Quantity)
                .InclusiveBetween(MinQuantity, MaxQuantity)
                .WithMessage($"Cantitatea trebuie să fie între {MinQuantity} și {MaxQuantity}.");
        });

        RuleFor(x => x.Items)
            .Must(items => items == null || items.GroupBy(i => i.UploadId).All(g => g.Count() == 1))
            .WithMessage("Coșul conține fotografii duplicate.");
    }
}
