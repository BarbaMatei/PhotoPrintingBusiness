using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests;

/// <summary>
/// The canonical cart entity graph (Product + active ProductSize + PricingTier +
/// ProductFinish + Upload + CartItem) for one user or guest. Hoisted here because OrderServiceTests, OrderServiceIdempotencyConcurrencyTests and
/// PaymentFactory each hand-rolled the identical graph and had already drifted — one omitted
/// <c>CartItem.SizeId</c> while the relational fixtures set it. <see cref="Build"/> returns the
/// entities so each fixture adds them to ITS OWN context (InMemory / PostgreSQL / WAF
/// scope); <see cref="CartGraph.AddTo"/> is the shared insert.
/// </summary>
internal static class TestCartSeed
{
    public static CartGraph Build(
        Guid? userId = null,
        Guid? guestSessionId = null,
        decimal unitPrice = 2.00m,
        int quantity = 3)
    {
        var product = new Product { Name = "Foto 10x15", IsActive = true };
        var size = new ProductSize
        {
            ProductId = product.Id, Label = "10x15", WidthMm = 100, HeightMm = 150, IsActive = true,
        };
        var tier = new PricingTier
        {
            ProductSizeId = size.Id, MinQuantity = 1, MaxQuantity = null, UnitPrice = unitPrice,
        };
        var finish = new ProductFinish { ProductId = product.Id, Name = "Lucios" };
        var upload = new Upload
        {
            UserId = userId,
            GuestSessionId = guestSessionId,
            OriginalFileName = "photo.jpg",
            FilePath = "/uploads/photo.jpg",
            ContentType = "image/jpeg",
            WidthPx = 1800,
            HeightPx = 1200,
        };
        var cartItem = new CartItem
        {
            UserId = userId,
            GuestSessionId = guestSessionId,
            UploadId = upload.Id,
            ProductId = product.Id,
            SizeId = size.Id, // set consistently — required under real FK enforcement
            Quantity = quantity,
        };

        return new CartGraph(product, size, tier, finish, upload, cartItem);
    }

    public sealed record CartGraph(
        Product Product,
        ProductSize Size,
        PricingTier Tier,
        ProductFinish Finish,
        Upload Upload,
        CartItem CartItem)
    {
        /// <summary>Adds the whole graph to <paramref name="db"/> (does not save).</summary>
        public void AddTo(DbContext db)
        {
            db.Add(Product);
            db.Add(Size);
            db.Add(Tier);
            db.Add(Finish);
            db.Add(Upload);
            db.Add(CartItem);
        }
    }
}
