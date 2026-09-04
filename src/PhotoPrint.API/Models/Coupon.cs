namespace PhotoPrint.API.Models;

public class Coupon
{
    public const int MaxCodeLength = 20;
    public const int MinCodeLength = 4;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public CouponType Type { get; set; }

    public decimal Value { get; set; }

    public decimal MinSubtotalRon { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset ValidUntil { get; set; }

    public int? MaxRedemptions { get; set; }

    public int RedemptionsCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
