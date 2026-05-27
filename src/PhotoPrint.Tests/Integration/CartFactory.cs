using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// Extends <see cref="UploadFactory"/> with cart-specific seed helpers.
/// </summary>
public class CartFactory : UploadFactory
{
    /// <summary>Seeds an active Product with one size and one pricing tier. The returned
    /// Product has its <c>Sizes</c> collection populated so callers can read
    /// <c>product.Sizes.First().Id</c>.</summary>
    public async Task<Product> SeedProductAsync(decimal unitPrice = 1.50m)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var product = new Product
        {
            Name = $"Test Product {Guid.NewGuid():N}".Substring(0, 20),
            IsActive = true,
            SortOrder = 0,
        };
        var size = new ProductSize
        {
            ProductId = product.Id,
            Label = "10x15",
            WidthMm = 100,
            HeightMm = 150,
            IsActive = true,
        };
        var tier = new PricingTier
        {
            ProductSizeId = size.Id,
            MinQuantity = 1,
            MaxQuantity = null,
            UnitPrice = unitPrice,
        };
        var finish = new ProductFinish { ProductId = product.Id, Name = "Lucios" };

        product.Sizes = [size];
        size.PricingTiers = [tier];
        product.Finishes = [finish];

        db.Products.Add(product);
        db.ProductSizes.Add(size);
        db.PricingTiers.Add(tier);
        db.ProductFinishes.Add(finish);
        await db.SaveChangesAsync();

        return product;
    }

    /// <summary>Seeds a CartItem row directly, bypassing validation.</summary>
    public async Task<CartItem> SeedCartItemAsync(
        Guid productId,
        Guid uploadId,
        Guid sizeId,
        Guid? userId = null,
        Guid? guestSessionId = null,
        int quantity = 2)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var item = new CartItem
        {
            UserId = userId,
            GuestSessionId = guestSessionId,
            UploadId = uploadId,
            ProductId = productId,
            SizeId = sizeId,
            Quantity = quantity,
        };
        db.CartItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }
}
