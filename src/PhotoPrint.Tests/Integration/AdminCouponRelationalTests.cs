using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoPrint.API.DTOs.Coupons;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Coupons;
using PhotoPrint.Tests.Helpers;
using Xunit;

namespace PhotoPrint.Tests.Integration;

public class AdminCouponRelationalTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;
    private readonly Guid _adminId = Guid.NewGuid();

    public AdminCouponRelationalTests(PostgresTestDatabase database)
    {
        _database = database;
        _database.ResetForTest();
    }

    private static AdminCouponService BuildService(PhotoPrint.API.Data.PhotoPrintDbContext db)
        => new(db, NullLogger<AdminCouponService>.Instance);

    private static CouponUpdateRequest RenameTo(string code) => new(
        Code: code,
        Type: nameof(CouponType.Fixed),
        Value: 30.00m,
        MinSubtotalRon: 0m,
        ValidFrom: DateTimeOffset.UtcNow.AddDays(-1),
        ValidUntil: DateTimeOffset.UtcNow.AddDays(30),
        MaxRedemptions: 10,
        IsActive: true);

    private async Task<Guid> SeedCouponAsync(string code)
    {
        using var seed = _database.NewContext();
        var coupon = TestCoupons.Make(code: code, type: CouponType.Fixed, value: 30.00m);
        seed.Coupons.Add(coupon);
        await seed.SaveChangesAsync();
        return coupon.Id;
    }

    [Fact]
    public async Task RenameRacingARedemption_Fails_AndLeavesTheCodeIntact()
    {
        var couponId = await SeedCouponAsync("VARA30");

        using var db = _database.NewContext();
        var tracked = await db.Coupons.FirstAsync(c => c.Id == couponId);
        tracked.RedemptionsCount.Should().Be(0);

        using var redeem = _database.NewContext();
        redeem.Coupons.Single(c => c.Id == couponId).RedemptionsCount = 1;
        await redeem.SaveChangesAsync();

        var act = () => BuildService(db).UpdateAsync(couponId, RenameTo("IARNA30"), _adminId);

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.CodeImmutableAfterRedemption);

        using var verify = _database.NewContext();
        var stored = verify.Coupons.Single(c => c.Id == couponId);
        stored.Code.Should().Be("VARA30");
        stored.RedemptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task RenameOfAnUnredeemedCoupon_PersistsTheNewCode()
    {
        var couponId = await SeedCouponAsync("VARA30");

        using var db = _database.NewContext();
        var updated = await BuildService(db).UpdateAsync(couponId, RenameTo("IARNA30"), _adminId);

        updated.Code.Should().Be("IARNA30");

        using var verify = _database.NewContext();
        verify.Coupons.Single(c => c.Id == couponId).Code.Should().Be("IARNA30");
    }

    [Fact]
    public async Task RenameToAnExistingCode_Returns409DuplicateCode_AndChangesNothing()
    {
        var couponId = await SeedCouponAsync("VARA30");
        await SeedCouponAsync("IARNA30");

        using var db = _database.NewContext();
        var act = () => BuildService(db).UpdateAsync(couponId, RenameTo("iarna30"), _adminId);

        (await act.Should().ThrowAsync<CouponConflictException>())
            .Which.ErrorCode.Should().Be(CouponErrorCodes.DuplicateCode);

        using var verify = _database.NewContext();
        verify.Coupons.Single(c => c.Id == couponId).Code.Should().Be("VARA30");
        verify.Coupons.Count(c => c.Code == "IARNA30").Should().Be(1);
    }
}
