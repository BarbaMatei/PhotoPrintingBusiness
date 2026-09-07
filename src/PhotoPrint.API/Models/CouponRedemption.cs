namespace PhotoPrint.API.Models;

public class CouponRedemption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CouponId { get; set; }

    public Guid OrderId { get; set; }

    public Guid? UserId { get; set; }

    public decimal DiscountRon { get; set; }

    public DateTimeOffset RedeemedAt { get; set; } = DateTimeOffset.UtcNow;

    public Coupon Coupon { get; set; } = null!;

    public Order Order { get; set; } = null!;
}
