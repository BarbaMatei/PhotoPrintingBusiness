using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Coupons;

public sealed class CouponService : ICouponService
{
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<CouponService> _logger;

    public CouponService(PhotoPrintDbContext db, ILogger<CouponService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private bool UsesRelationalProvider
        => _db.Database.ProviderName != DbProviders.InMemory;

    public async Task<CouponResolution> ApplyToCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        string rawCode,
        decimal goodsGrossRon,
        CancellationToken ct = default)
    {
        RequireOwner(userId, guestSessionId);

        var code = CouponCode.Normalize(rawCode);

        if (goodsGrossRon <= 0m)
        {
            LogRejected(code, CouponErrorCodes.EmptyCart);
            throw new CouponRejectedException(
                CouponErrorCodes.EmptyCart, CouponMessages.For(CouponErrorCodes.EmptyCart));
        }

        if (!CouponCode.IsWellFormed(code))
        {
            LogRejected(code, CouponErrorCodes.InvalidCoupon);
            throw new CouponRejectedException(
                CouponErrorCodes.InvalidCoupon, CouponMessages.For(CouponErrorCodes.InvalidCoupon));
        }

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct);
        var failure = Validate(coupon, goodsGrossRon, DateTimeOffset.UtcNow);
        if (failure is not null)
        {
            LogRejected(code, failure);
            throw new CouponRejectedException(
                failure, CouponMessages.For(failure, coupon?.MinSubtotalRon ?? 0m));
        }

        var existing = await FindCartCouponAsync(userId, guestSessionId, ct);
        if (existing is not null)
            _db.CartCoupons.Remove(existing);

        _db.CartCoupons.Add(new CartCoupon
        {
            UserId = userId,
            GuestSessionId = guestSessionId,
            CouponId = coupon!.Id,
        });

        await _db.SaveChangesAsync(ct);

        var discount = CouponDiscountCalculator.Compute(coupon.Type, coupon.Value, goodsGrossRon, 0m);
        _logger.LogInformation(
            "coupon.applied code={Code} discount_ron={DiscountRon} owner={Owner}",
            coupon.Code, discount, userId.HasValue ? "user" : "guest");

        return new CouponResolution(coupon.Id, coupon.Code, coupon.Type, discount);
    }

    public async Task ClearCartCouponAsync(
        Guid? userId, Guid? guestSessionId, CancellationToken ct = default)
    {
        if (userId is null && guestSessionId is null) return;

        var existing = await FindCartCouponAsync(userId, guestSessionId, ct);
        if (existing is null) return;

        _db.CartCoupons.Remove(existing);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CartCouponView?> ResolveForCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        decimal goodsGrossRon,
        CancellationToken ct = default)
    {
        if (userId is null && guestSessionId is null) return null;

        var applied = await FindCartCouponAsync(userId, guestSessionId, ct);
        if (applied is null) return null;

        var coupon = await _db.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == applied.CouponId, ct);
        if (coupon is null) return null;

        var failure = Validate(coupon, goodsGrossRon, DateTimeOffset.UtcNow);
        if (failure is not null)
            return new CartCouponView(coupon.Id, coupon.Code, coupon.Type, 0m, true, failure);

        var discount = CouponDiscountCalculator.Compute(coupon.Type, coupon.Value, goodsGrossRon, 0m);
        return new CartCouponView(coupon.Id, coupon.Code, coupon.Type, discount, false, null);
    }

    public async Task<CouponResolution?> ResolveForOrderAsync(
        Guid? userId,
        Guid? guestSessionId,
        decimal goodsGrossRon,
        decimal shippingGrossRon,
        Guid? heldCouponId = null,
        CancellationToken ct = default)
    {
        if (userId is null && guestSessionId is null) return null;

        var applied = await FindCartCouponAsync(userId, guestSessionId, ct);
        if (applied is null) return null;

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == applied.CouponId, ct);
        var heldSlotCredit = coupon is not null && coupon.Id == heldCouponId ? 1 : 0;
        var failure = Validate(coupon, goodsGrossRon, DateTimeOffset.UtcNow, heldSlotCredit);
        if (failure is not null)
        {
            LogRejected(coupon?.Code ?? "unknown", failure);
            throw new CouponConflictException(
                failure, CouponMessages.For(failure, coupon?.MinSubtotalRon ?? 0m));
        }

        var discount = CouponDiscountCalculator.Compute(
            coupon!.Type, coupon.Value, goodsGrossRon, shippingGrossRon);

        if (discount <= 0m)
        {
            LogRejected(coupon.Code, CouponErrorCodes.NoDiscount);
            return null;
        }

        return new CouponResolution(coupon.Id, coupon.Code, coupon.Type, discount);
    }

    public async Task ConsumeOrThrowAsync(Guid couponId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        int affected;

        if (UsesRelationalProvider)
        {
            affected = await _db.Coupons
                .Where(c => c.Id == couponId
                    && c.IsActive
                    && c.ValidFrom <= now
                    && c.ValidUntil > now
                    && (c.MaxRedemptions == null || c.RedemptionsCount < c.MaxRedemptions))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(c => c.RedemptionsCount, c => c.RedemptionsCount + 1), ct);
        }
        else
        {
            var tracked = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == couponId, ct);
            if (tracked is not null && IsRedeemable(tracked, now))
            {
                tracked.RedemptionsCount += 1;
                await _db.SaveChangesAsync(ct);
                affected = 1;
            }
            else
            {
                affected = 0;
            }
        }

        if (affected == 1)
        {
            _logger.LogInformation("coupon.redeemed coupon_id={CouponId}", couponId);
            return;
        }

        var reason = await ClassifyRefusalAsync(couponId, now, ct);
        if (reason == CouponErrorCodes.CouponExhausted)
            _logger.LogWarning("coupon.exhausted coupon_id={CouponId}", couponId);
        else
            LogRejected(couponId.ToString(), reason);

        throw new CouponConflictException(reason, CouponMessages.For(reason));
    }

    public async Task ReleaseForOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var redemption = await _db.CouponRedemptions
            .FirstOrDefaultAsync(r => r.OrderId == orderId, ct);
        if (redemption is null) return;

        var ownsTransaction = UsesRelationalProvider && _db.Database.CurrentTransaction is null;
        var tx = ownsTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        await using var _ = tx;

        _db.CouponRedemptions.Remove(redemption);
        await _db.SaveChangesAsync(ct);

        if (UsesRelationalProvider)
        {
            await _db.Coupons
                .Where(c => c.Id == redemption.CouponId && c.RedemptionsCount > 0)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(c => c.RedemptionsCount, c => c.RedemptionsCount - 1), ct);
        }
        else
        {
            var tracked = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == redemption.CouponId, ct);
            if (tracked is { RedemptionsCount: > 0 })
            {
                tracked.RedemptionsCount -= 1;
                await _db.SaveChangesAsync(ct);
            }
        }

        if (tx is not null) await tx.CommitAsync(ct);

        _logger.LogInformation(
            "coupon.released coupon_id={CouponId} order_id={OrderId} discount_ron={DiscountRon}",
            redemption.CouponId, orderId, redemption.DiscountRon);
    }

    public async Task TransferGuestCouponAsync(
        Guid userId, Guid guestSessionId, CancellationToken ct = default)
    {
        var guestApplied = await _db.CartCoupons
            .FirstOrDefaultAsync(cc => cc.GuestSessionId == guestSessionId, ct);
        if (guestApplied is null) return;

        var userApplied = await _db.CartCoupons
            .FirstOrDefaultAsync(cc => cc.UserId == userId, ct);

        if (userApplied is not null)
        {
            _db.CartCoupons.Remove(guestApplied);
        }
        else
        {
            guestApplied.UserId = userId;
            guestApplied.GuestSessionId = null;
        }

        await _db.SaveChangesAsync(ct);
    }

    private Task<CartCoupon?> FindCartCouponAsync(
        Guid? userId, Guid? guestSessionId, CancellationToken ct)
        => _db.CartCoupons.FirstOrDefaultAsync(
            cc => userId.HasValue ? cc.UserId == userId : cc.GuestSessionId == guestSessionId, ct);

    private async Task<string> ClassifyRefusalAsync(
        Guid couponId, DateTimeOffset now, CancellationToken ct)
    {
        var coupon = await _db.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == couponId, ct);

        if (coupon is null || !coupon.IsActive) return CouponErrorCodes.InvalidCoupon;
        if (coupon.ValidFrom > now || coupon.ValidUntil <= now) return CouponErrorCodes.InvalidCoupon;
        return coupon.MaxRedemptions is null
            ? CouponErrorCodes.InvalidCoupon
            : CouponErrorCodes.CouponExhausted;
    }

    private static string? Validate(
        Coupon? coupon, decimal goodsGrossRon, DateTimeOffset now, int heldSlotCredit = 0)
    {
        if (coupon is null || !coupon.IsActive) return CouponErrorCodes.InvalidCoupon;
        if (coupon.ValidFrom > now || coupon.ValidUntil <= now) return CouponErrorCodes.InvalidCoupon;
        if (coupon.MaxRedemptions is { } cap && coupon.RedemptionsCount - heldSlotCredit >= cap)
            return CouponErrorCodes.CouponExhausted;
        if (goodsGrossRon < coupon.MinSubtotalRon) return CouponErrorCodes.MinSubtotalNotMet;
        return null;
    }

    private static bool IsRedeemable(Coupon coupon, DateTimeOffset now)
        => coupon.IsActive
            && coupon.ValidFrom <= now
            && coupon.ValidUntil > now
            && (coupon.MaxRedemptions is not { } cap || coupon.RedemptionsCount < cap);

    private static void RequireOwner(Guid? userId, Guid? guestSessionId)
    {
        if (userId is null && guestSessionId is null)
            throw new InvalidOperationException(
                "A coupon operation requires an authenticated user or guest session identity.");
    }

    private void LogRejected(string code, string reason)
        => _logger.LogInformation("coupon.rejected code={Code} reason={Reason}", code, reason);
}
