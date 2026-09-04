namespace PhotoPrint.API.Exceptions;

public static class CouponErrorCodes
{
    public const string InvalidCoupon = "INVALID_COUPON";
    public const string MinSubtotalNotMet = "MIN_SUBTOTAL_NOT_MET";
    public const string CouponExhausted = "COUPON_EXHAUSTED";
    public const string EmptyCart = "EMPTY_CART";
    public const string OrderTotalBelowMinimum = "ORDER_TOTAL_BELOW_MINIMUM";
    public const string DuplicateCode = "DUPLICATE_CODE";
    public const string CouponAlreadyInactive = "COUPON_ALREADY_INACTIVE";
    public const string CodeImmutableAfterRedemption = "CODE_IMMUTABLE_AFTER_REDEMPTION";
    public const string NoDiscount = "NO_DISCOUNT";
}
