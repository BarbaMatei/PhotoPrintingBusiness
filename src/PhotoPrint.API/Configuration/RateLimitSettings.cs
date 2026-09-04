namespace PhotoPrint.API.Configuration;

public sealed class RateLimitSettings
{
    public int WindowSeconds { get; init; } = 60;
    public RateLimitWindow Public { get; init; } = new() { PermitLimit = 100 };
    public RateLimitWindow Auth { get; init; } = new() { PermitLimit = 10 };
    public RateLimitWindow Coupon { get; init; } = new() { PermitLimit = 15 };
}

public sealed class RateLimitWindow
{
    public int PermitLimit { get; init; }
}
