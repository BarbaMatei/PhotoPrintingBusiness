namespace PhotoPrint.API.Models;

public class CartCoupon
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }

    public Guid? GuestSessionId { get; set; }

    public Guid CouponId { get; set; }

    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }

    public Coupon Coupon { get; set; } = null!;
}
