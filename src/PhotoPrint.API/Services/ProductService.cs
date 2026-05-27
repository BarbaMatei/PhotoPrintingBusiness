using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Products;

namespace PhotoPrint.API.Services;

public class ProductService(PhotoPrintDbContext db, PricingService pricingService) : IProductService
{
    public async Task<List<ProductDto>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var products = await db.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Include(p => p.Sizes.Where(s => s.IsActive).OrderBy(s => s.Label))
                .ThenInclude(s => s.PricingTiers.OrderBy(t => t.MinQuantity))
            .Include(p => p.Finishes.OrderBy(f => f.Name))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return products.Select(MapToDto).ToList();
    }

    public async Task<ProductDto?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await db.Products
            .Where(p => p.Id == id && p.IsActive)
            .Include(p => p.Sizes.Where(s => s.IsActive).OrderBy(s => s.Label))
                .ThenInclude(s => s.PricingTiers.OrderBy(t => t.MinQuantity))
            .Include(p => p.Finishes.OrderBy(f => f.Name))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? null : MapToDto(product);
    }

    public async Task<PriceCalculationResponse?> CalculatePriceAsync(
        Guid productId, Guid sizeId, int quantity, CancellationToken cancellationToken = default)
    {
        var size = await db.ProductSizes
            .Where(s => s.Id == sizeId && s.ProductId == productId && s.IsActive)
            .Include(s => s.PricingTiers.OrderBy(t => t.MinQuantity))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (size is null)
            return null;

        var tier = pricingService.GetApplicableTier(size.PricingTiers, quantity);
        var (unitPrice, totalPrice, tierLabel) = pricingService.Calculate(tier, quantity);

        return new PriceCalculationResponse
        {
            SizeId     = size.Id,
            SizeLabel  = size.Label,
            Quantity   = quantity,
            UnitPrice  = unitPrice,
            TotalPrice = totalPrice,
            TierLabel  = tierLabel,
            Currency   = "RON",
        };
    }

    // ── Mapping ──────────────────────────────────────────────────────────────
    private static ProductDto MapToDto(Models.Product p) => new()
    {
        Id          = p.Id,
        Name        = p.Name,
        ProductType = p.ProductType,
        ImageUrl    = p.ImageUrl,
        SortOrder   = p.SortOrder,
        Sizes       = p.Sizes.Select(s => new ProductSizeDto
        {
            Id           = s.Id,
            Label        = s.Label,
            WidthMm      = s.WidthMm,
            HeightMm     = s.HeightMm,
            PricingTiers = s.PricingTiers.Select(t => new PricingTierDto
            {
                MinQuantity = t.MinQuantity,
                MaxQuantity = t.MaxQuantity,
                UnitPrice   = t.UnitPrice,
            }).ToList(),
        }).ToList(),
        Finishes = p.Finishes.Select(f => f.Name).ToList(),
    };
}
