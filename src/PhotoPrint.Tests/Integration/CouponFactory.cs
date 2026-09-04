using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

public class CouponFactory : CartFactory
{
    public async Task<Coupon> SeedCouponAsync(
        string code = "VARA25",
        CouponType type = CouponType.Fixed,
        decimal value = 10.00m,
        decimal minSubtotalRon = 0m,
        int? maxRedemptions = null,
        int redemptionsCount = 0,
        bool isActive = true,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var coupon = TestCoupons.Make(
            code, type, value, minSubtotalRon, maxRedemptions, redemptionsCount,
            isActive, validFrom, validUntil);

        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();
        return coupon;
    }

    public async Task<Coupon?> ReadCouponAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        return await db.Coupons.FindAsync(id);
    }

    public async Task<int> CountCartCouponsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        return db.CartCoupons.Count();
    }

    public async Task<(Guid userId, string bearerToken)> SeedAdminWithJwtAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var admin = new User
        {
            Email = $"coupon-admin-{Guid.NewGuid():N}@example.com",
            NormalizedEmail = $"COUPON-ADMIN-{Guid.NewGuid():N}@EXAMPLE.COM",
            FirstName = "Coupon",
            LastName = "Admin",
            IsEmailConfirmed = true,
            GdprConsentAccepted = true,
            Role = UserRole.Admin,
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Test@1234!");
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        return (admin.Id, CouponAdminJwt.ForAdmin(admin.Id));
    }

    public async Task SeedRedemptionAsync(Guid couponId, Guid orderId, decimal discountRon = 10m)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = couponId,
            OrderId = orderId,
            DiscountRon = discountRon,
        });

        var coupon = await db.Coupons.FindAsync(couponId);
        if (coupon is not null) coupon.RedemptionsCount += 1;

        await db.SaveChangesAsync();
    }

    public async Task<Order> SeedBareOrderAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var order = TestOrders.Make(Guid.NewGuid());
        order.OrderNumber = $"FT-{Guid.NewGuid():N}"[..18];
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }
}

file static class CouponAdminJwt
{
    public static string ForAdmin(Guid userId)
    {
        var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(TestKeys.RsaPrivateKeyPem);

        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa),
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId.ToString()),
            new System.Security.Claims.Claim(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, "admin@test.com"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"),
            new System.Security.Claims.Claim(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "fototipar",
            audience: "fototipar-spa",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
