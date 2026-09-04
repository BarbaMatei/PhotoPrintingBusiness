using System.Globalization;
using PhotoPrint.API.Exceptions;

namespace PhotoPrint.API.Services.Coupons;

public static class CouponMessages
{
    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    public static string For(string errorCode, decimal minSubtotalRon = 0m) => errorCode switch
    {
        CouponErrorCodes.InvalidCoupon =>
            "Codul introdus nu este valid sau a expirat.",
        CouponErrorCodes.MinSubtotalNotMet =>
            $"Codul se aplică doar la comenzi de cel puțin {minSubtotalRon.ToString("N2", Ro)} RON.",
        CouponErrorCodes.CouponExhausted =>
            "Codul a atins limita de utilizări.",
        CouponErrorCodes.EmptyCart =>
            "Coșul este gol.",
        CouponErrorCodes.OrderTotalBelowMinimum =>
            "După reducere, valoarea comenzii este prea mică pentru a fi plătită online. "
            + "Adaugă produse sau elimină codul.",
        CouponErrorCodes.NoDiscount =>
            "Codul nu produce nicio reducere pentru această comandă.",
        CouponErrorCodes.DuplicateCode =>
            "Există deja un cupon cu acest cod.",
        CouponErrorCodes.CouponAlreadyInactive =>
            "Cuponul este deja dezactivat.",
        CouponErrorCodes.CodeImmutableAfterRedemption =>
            "Codul nu mai poate fi modificat după prima utilizare.",
        _ => "Codul introdus nu poate fi folosit.",
    };
}
