using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.DTOs.Products;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class AdminProductService(PhotoPrintDbContext db, PricingService pricingService) : IAdminProductService
{
    // ── Get All (admin) ───────────────────────────────────────────────────────

    public async Task<List<ProductDto>> GetAllProductsAsync(CancellationToken ct = default)
    {
        var products = await db.Products
            .Include(p => p.Sizes.OrderBy(s => s.Label))
                .ThenInclude(s => s.PricingTiers.OrderBy(t => t.MinQuantity))
            .Include(p => p.Finishes.OrderBy(f => f.Name))
            .OrderBy(p => p.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        return products.Select(p => new ProductDto
        {
            Id          = p.Id,
            Name        = p.Name,
            ProductType = p.ProductType,
            ImageUrl    = p.ImageUrl,
            SortOrder   = p.SortOrder,
            IsActive    = p.IsActive,
            Sizes       = p.Sizes.Select(s => new ProductSizeDto
            {
                Id           = s.Id,
                Label        = s.Label,
                WidthMm      = s.WidthMm,
                HeightMm     = s.HeightMm,
                IsActive     = s.IsActive,
                PricingTiers = s.PricingTiers.Select(t => new PricingTierDto
                {
                    MinQuantity = t.MinQuantity,
                    MaxQuantity = t.MaxQuantity,
                    UnitPrice   = t.UnitPrice,
                }).ToList(),
            }).ToList(),
            Finishes = p.Finishes.Select(f => f.Name).ToList(),
        }).ToList();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name        = request.Name,
            ProductType = request.ProductType,
            ImageUrl    = request.ImageUrl,
            SortOrder   = request.SortOrder,
            IsActive    = true,
        };

        foreach (var sizeReq in request.Sizes)
        {
            product.Sizes.Add(new ProductSize
            {
                Label    = sizeReq.Label,
                WidthMm  = sizeReq.WidthMm,
                HeightMm = sizeReq.HeightMm,
                IsActive = false,
            });
        }

        // Default finishes
        product.Finishes.Add(new ProductFinish { Name = "Lucioasă" });
        product.Finishes.Add(new ProductFinish { Name = "Mată" });

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return await LoadProductDtoAsync(product.Id, cancellationToken);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Produsul nu a fost găsit.");

        product.Name        = request.Name;
        product.ProductType = request.ProductType;
        product.ImageUrl    = request.ImageUrl;
        product.SortOrder   = request.SortOrder;

        await db.SaveChangesAsync(cancellationToken);
        return await LoadProductDtoAsync(id, cancellationToken);
    }

    // ── Status ────────────────────────────────────────────────────────────────

    public async Task SetProductStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Produsul nu a fost găsit.");

        product.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Delete (soft) ─────────────────────────────────────────────────────────

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Produsul nu a fost găsit.");

        product.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Sizes ─────────────────────────────────────────────────────────────────

    public async Task<ProductSizeDto> AddProductSizeAsync(Guid productId, CreateProductSizeRequest request, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FindAsync([productId], cancellationToken)
            ?? throw new NotFoundException("Produsul nu a fost găsit.");

        var labelExists = await db.ProductSizes
            .AnyAsync(s => s.ProductId == productId && s.Label == request.Label, cancellationToken);

        if (labelExists)
            throw new ConflictException($"Dimensiunea '{request.Label}' există deja pentru acest produs.");

        var size = new ProductSize
        {
            ProductId = product.Id,
            Label     = request.Label,
            WidthMm   = request.WidthMm,
            HeightMm  = request.HeightMm,
            IsActive  = false,
        };

        db.ProductSizes.Add(size);
        await db.SaveChangesAsync(cancellationToken);

        return new ProductSizeDto
        {
            Id           = size.Id,
            Label        = size.Label,
            WidthMm      = size.WidthMm,
            HeightMm     = size.HeightMm,
            PricingTiers = [],
        };
    }

    public async Task SetSizeStatusAsync(Guid productId, Guid sizeId, bool isActive, CancellationToken cancellationToken = default)
    {
        var size = await db.ProductSizes
            .Include(s => s.PricingTiers)
            .FirstOrDefaultAsync(s => s.Id == sizeId && s.ProductId == productId, cancellationToken)
            ?? throw new NotFoundException("Dimensiunea nu a fost găsită.");

        if (isActive && !size.PricingTiers.Any())
            throw new UnprocessableEntityException("Dimensiunea nu poate fi activată fără niveluri de prețuri.");

        size.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Pricing Tiers ─────────────────────────────────────────────────────────

    public async Task<ProductSizeDto> ReplacePricingTiersAsync(
        Guid productId, Guid sizeId, ReplacePricingTiersRequest request, CancellationToken cancellationToken = default)
    {
        var (isValid, error) = pricingService.ValidateTiers(request.Tiers);
        if (!isValid)
            throw new UnprocessableEntityException(error!);

        var size = await db.ProductSizes
            .Include(s => s.PricingTiers)
            .FirstOrDefaultAsync(s => s.Id == sizeId && s.ProductId == productId, cancellationToken)
            ?? throw new NotFoundException("Dimensiunea nu a fost găsită.");

        db.PricingTiers.RemoveRange(size.PricingTiers);

        var newTiers = request.Tiers.Select(t => new PricingTier
        {
            ProductSizeId = sizeId,
            MinQuantity   = t.MinQuantity,
            MaxQuantity   = t.MaxQuantity,
            UnitPrice     = t.UnitPrice,
        }).ToList();

        db.PricingTiers.AddRange(newTiers);
        await db.SaveChangesAsync(cancellationToken);

        return new ProductSizeDto
        {
            Id           = size.Id,
            Label        = size.Label,
            WidthMm      = size.WidthMm,
            HeightMm     = size.HeightMm,
            PricingTiers = newTiers.OrderBy(t => t.MinQuantity).Select(t => new PricingTierDto
            {
                MinQuantity = t.MinQuantity,
                MaxQuantity = t.MaxQuantity,
                UnitPrice   = t.UnitPrice,
            }).ToList(),
        };
    }

    // ── Finishes ──────────────────────────────────────────────────────────────

    public async Task ReplaceFinishesAsync(Guid productId, IReadOnlyList<string> names, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Finishes)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new NotFoundException("Produsul nu a fost găsit.");

        db.RemoveRange(product.Finishes);

        foreach (var name in names
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            product.Finishes.Add(new ProductFinish { Name = name });
        }

        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ProductDto> LoadProductDtoAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .Where(p => p.Id == productId)
            .Include(p => p.Sizes.OrderBy(s => s.Label))
                .ThenInclude(s => s.PricingTiers.OrderBy(t => t.MinQuantity))
            .Include(p => p.Finishes.OrderBy(f => f.Name))
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        return new ProductDto
        {
            Id          = product.Id,
            Name        = product.Name,
            ProductType = product.ProductType,
            ImageUrl    = product.ImageUrl,
            SortOrder   = product.SortOrder,
            IsActive    = product.IsActive,
            Sizes       = product.Sizes.Select(s => new ProductSizeDto
            {
                Id           = s.Id,
                Label        = s.Label,
                WidthMm      = s.WidthMm,
                HeightMm     = s.HeightMm,
                IsActive     = s.IsActive,
                PricingTiers = s.PricingTiers.Select(t => new PricingTierDto
                {
                    MinQuantity = t.MinQuantity,
                    MaxQuantity = t.MaxQuantity,
                    UnitPrice   = t.UnitPrice,
                }).ToList(),
            }).ToList(),
            Finishes = product.Finishes.Select(f => f.Name).ToList(),
        };
    }
}
