using FluentValidation;
using PhotoPrint.API.DTOs.Coupons;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Coupons;

namespace PhotoPrint.API.Validators.Coupons;

public class CouponWriteRequestValidator : AbstractValidator<ICouponWriteRequest>
{
    public const decimal MaxPercentValue = 99.99m;

    public CouponWriteRequestValidator()
    {
        RuleFor(x => x.Code)
            .Must(CouponCode.IsWellFormed)
            .WithMessage(
                $"Codul trebuie să conțină între {Coupon.MinCodeLength} și {Coupon.MaxCodeLength} "
                + "litere mari fără diacritice sau cifre.");

        RuleFor(x => x.Type)
            .Must(BeAKnownType)
            .WithMessage("Tipul cuponului trebuie să fie Percent, Fixed sau FreeShipping.");

        RuleFor(x => x.Value)
            .GreaterThan(0m)
            .WithMessage("Valoarea cuponului trebuie să fie mai mare decât 0.")
            .When(x => !IsType(x.Type, CouponType.FreeShipping));

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(MaxPercentValue)
            .WithMessage(
                "Un cupon procentual nu poate acoperi întreaga valoare a comenzii; "
                + $"valoarea maximă este {MaxPercentValue}%.")
            .When(x => IsType(x.Type, CouponType.Percent));

        RuleFor(x => x.MinSubtotalRon)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Valoarea minimă a comenzii nu poate fi negativă.");

        RuleFor(x => x.ValidUntil)
            .GreaterThan(x => x.ValidFrom)
            .WithMessage("Data de sfârșit trebuie să fie după data de început.");

        RuleFor(x => x.MaxRedemptions)
            .Must(m => m is null or > 0)
            .WithMessage("Numărul maxim de utilizări trebuie să fie mai mare decât 0.");
    }

    private static bool BeAKnownType(string type)
        => Enum.TryParse<CouponType>(type, ignoreCase: true, out _);

    private static bool IsType(string type, CouponType expected)
        => Enum.TryParse<CouponType>(type, ignoreCase: true, out var parsed) && parsed == expected;
}

public class CouponCreateRequestValidator : AbstractValidator<CouponCreateRequest>
{
    public CouponCreateRequestValidator() => Include(new CouponWriteRequestValidator());
}

public class CouponUpdateRequestValidator : AbstractValidator<CouponUpdateRequest>
{
    public CouponUpdateRequestValidator() => Include(new CouponWriteRequestValidator());
}
