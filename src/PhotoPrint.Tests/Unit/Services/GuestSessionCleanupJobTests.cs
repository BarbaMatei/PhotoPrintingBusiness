using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

public class GuestSessionCleanupJobTests
{
    private static PhotoPrintDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IServiceScopeFactory BuildScopeFactory(PhotoPrintDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton<PhotoPrintDbContext>(_ => db);
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static GuestSession MakeSession(DateTimeOffset expiresAt, Guid? claimedBy = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = "guest@example.com",
        FirstName = "Guest",
        LastName = "User",
        Phone = "0700000000",
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        ExpiresAt = expiresAt,
        ClaimedByUserId = claimedBy,
    };

    private static async Task InvokeCleanupAsync(GuestSessionCleanupJob job, CancellationToken ct)
    {
        var method = typeof(GuestSessionCleanupJob).GetMethod(
            "CleanupAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task<int>)method!.Invoke(job, new object[] { ct })!;
    }

    [Fact]
    public async Task ExpiredUnclaimed_Session_IsDeleted()
    {
        var db = CreateDb();
        var expired = MakeSession(DateTimeOffset.UtcNow.AddHours(-1));
        await db.GuestSessions.AddAsync(expired);
        await db.SaveChangesAsync();

        var job = new GuestSessionCleanupJob(BuildScopeFactory(db),
            Mock.Of<ILogger<GuestSessionCleanupJob>>());

        await InvokeCleanupAsync(job, CancellationToken.None);

        var remaining = await db.GuestSessions.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task ExpiredSession_WithAnAppliedCoupon_TakesTheCartCouponWithIt()
    {
        var db = CreateDb();
        var expired = MakeSession(DateTimeOffset.UtcNow.AddHours(-1));
        var active = MakeSession(DateTimeOffset.UtcNow.AddDays(1));
        var coupon = Helpers.TestCoupons.Make(code: "VARA25");
        await db.GuestSessions.AddRangeAsync(expired, active);
        await db.Coupons.AddAsync(coupon);
        await db.CartCoupons.AddRangeAsync(
            new CartCoupon { GuestSessionId = expired.Id, CouponId = coupon.Id },
            new CartCoupon { GuestSessionId = active.Id, CouponId = coupon.Id });
        await db.SaveChangesAsync();

        var job = new GuestSessionCleanupJob(BuildScopeFactory(db),
            Mock.Of<ILogger<GuestSessionCleanupJob>>());

        await InvokeCleanupAsync(job, CancellationToken.None);

        var remaining = await db.CartCoupons.ToListAsync();
        remaining.Should().ContainSingle();
        remaining[0].GuestSessionId.Should().Be(active.Id);
    }

    [Fact]
    public async Task ActiveSession_IsNotDeleted()
    {
        var db = CreateDb();
        var active = MakeSession(DateTimeOffset.UtcNow.AddDays(1));
        await db.GuestSessions.AddAsync(active);
        await db.SaveChangesAsync();

        var job = new GuestSessionCleanupJob(BuildScopeFactory(db),
            Mock.Of<ILogger<GuestSessionCleanupJob>>());

        await InvokeCleanupAsync(job, CancellationToken.None);

        var remaining = await db.GuestSessions.CountAsync();
        remaining.Should().Be(1);
    }

    [Fact]
    public async Task ExpiredButClaimed_Session_IsNotDeleted()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        // Add user so FK is satisfied
        await db.Users.AddAsync(new User { Id = userId, Email = "u@u.com", NormalizedEmail = "U@U.COM" });
        var claimed = MakeSession(DateTimeOffset.UtcNow.AddHours(-2), claimedBy: userId);
        await db.GuestSessions.AddAsync(claimed);
        await db.SaveChangesAsync();

        var job = new GuestSessionCleanupJob(BuildScopeFactory(db),
            Mock.Of<ILogger<GuestSessionCleanupJob>>());

        await InvokeCleanupAsync(job, CancellationToken.None);

        var remaining = await db.GuestSessions.CountAsync();
        remaining.Should().Be(1);
    }

    [Fact]
    public async Task Mixed_Sessions_OnlyExpiredUnclaimedAreDeleted()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        await db.Users.AddAsync(new User { Id = userId, Email = "u@u.com", NormalizedEmail = "U@U.COM" });

        await db.GuestSessions.AddRangeAsync(
            MakeSession(DateTimeOffset.UtcNow.AddHours(-1)),          // expired unclaimed → DELETE
            MakeSession(DateTimeOffset.UtcNow.AddDays(1)),             // active → keep
            MakeSession(DateTimeOffset.UtcNow.AddHours(-1), userId)    // expired claimed → keep
        );
        await db.SaveChangesAsync();

        var job = new GuestSessionCleanupJob(BuildScopeFactory(db),
            Mock.Of<ILogger<GuestSessionCleanupJob>>());

        await InvokeCleanupAsync(job, CancellationToken.None);

        var remaining = await db.GuestSessions.CountAsync();
        remaining.Should().Be(2);
    }
}
