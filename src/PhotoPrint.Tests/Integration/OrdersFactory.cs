using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

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
    public async Task<Order> SeedOrderAsync(
        Guid userId,
        OrderStatus status = OrderStatus.Paid,
        DeliveryType deliveryType = DeliveryType.Easybox,
        int quantity = 2)
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
