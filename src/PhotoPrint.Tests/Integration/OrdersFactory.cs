using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// Extends <see cref="PaymentFactory"/> with order-seeding helpers for
/// <see cref="OrdersControllerIntegrationTests"/>.
/// </summary>
public class OrdersFactory : PaymentFactory
{
    /// <summary>
    /// Seeds a completed <see cref="Order"/> directly for the given user.
    /// </summary>
    public async Task<(Guid userId, string bearerToken)> SeedAdminWithJwtAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        var hasher = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<User>>();

        var admin = new User
        {
            Email               = $"admin-{Guid.NewGuid():N}@example.com",
            NormalizedEmail     = $"ADMIN-{Guid.NewGuid():N}@EXAMPLE.COM",
            FirstName           = "Admin",
            LastName            = "Tester",
            IsEmailConfirmed    = true,
            GdprConsentAccepted = true,
            Role                = UserRole.Admin,
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Test@1234!");
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        return (admin.Id, AdminJwt.ForAdmin(admin.Id));
    }

    public async Task<Order> SeedOrderAsync(
        Guid? userId,
        OrderStatus status = OrderStatus.Paid,
        DeliveryType deliveryType = DeliveryType.Easybox,
        int quantity = 2,
        Guid? guestSessionId = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var upload = new Upload
        {
            UserId = userId,
            FilePath = $"uploads/{Guid.NewGuid():N}.jpg",
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 1200,
            HeightPx = 800,
            FileSizeBytes = 100_000,
        };
        db.Uploads.Add(upload);

        var product = new Product { Name = "10x15 Test", IsActive = true, SortOrder = 0 };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var shippingAddress = deliveryType == DeliveryType.Courier
            ? new ShippingAddressSnapshot
            {
                RecipientName = "Ion Popescu",
                Street = "Str. Libertății",
                Number = "1",
                City = "Cluj-Napoca",
                County = "Cluj",
                PostalCode = "400001",
                Phone = "0700000001",
            }
            : new ShippingAddressSnapshot
            {
                RecipientName = "Locker Recipient",
                Street = "Str. Locker",
                Number = "2",
                City = "Cluj-Napoca",
                County = "Cluj",
                PostalCode = "400002",
                Phone = "0700000002",
            };

        EasyboxLocker? locker = null;
        if (deliveryType == DeliveryType.Easybox)
        {
            locker = new EasyboxLocker
            {
                SamedayId = "SD001",
                Name = "Easybox Iulius Mall",
                Address = "Str. Alexandru Vaida Voevod 53",
                City = "Cluj-Napoca",
                County = "Cluj",
                Lat = 46.7712,
                Lng = 23.6236,
            };
            db.EasyboxLockers.Add(locker);
            await db.SaveChangesAsync();
        }

        var orderItem = new OrderItem
        {
            UploadId = upload.Id,
            ProductId = product.Id,
            Quantity = quantity,
            UnitPriceRon = 1.50m,
            LineTotalRon = 1.50m * quantity,
            ProductSnapshot = new ProductSnapshot
            {
                ProductName = "10x15 Test",
                Size = "10x15",
                Finish = "Lucios",
            },
        };

        var subtotal = orderItem.LineTotalRon;
        var shippingCost = deliveryType == DeliveryType.Easybox ? 20m : 25m;

        var order = new Order
        {
            OrderNumber = $"FT-TEST-{Guid.NewGuid():N}".Substring(0, 14),
            UserId = userId,
            GuestSessionId = guestSessionId,
            Status = status,
            DeliveryType = deliveryType,
            EasyboxLockerId = locker?.Id,
            ShippingAddress = shippingAddress,
            ShippingCostRon = shippingCost,
            SubtotalRon = subtotal,
            TotalRon = subtotal + shippingCost,
            PaidAt = status == OrderStatus.Paid ? DateTimeOffset.UtcNow : null,
            Items = new List<OrderItem> { orderItem },
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order;
    }
}

// The shared generator in UploadFactory is file-scoped and always stamps the User role.
file static class AdminJwt
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
