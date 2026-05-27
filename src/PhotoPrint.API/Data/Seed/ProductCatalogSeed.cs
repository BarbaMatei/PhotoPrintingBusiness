using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Data.Seed;

/// <summary>
/// Seeds the initial photo print product catalog:
/// 1 product, 6 sizes, 2 finishes, 18 pricing tiers (3 tiers × 6 sizes).
/// All GUIDs are deterministic so this operation is idempotent (skips if already seeded).
/// </summary>
public static class ProductCatalogSeed
{
    // ── IDs ──────────────────────────────────────────────────────────────────
    public static readonly Guid ProductId = new("a1000000-0000-0000-0000-000000000001");

    private static readonly Guid Size10x15Id   = new("b1000000-0000-0000-0000-000000000001");
    private static readonly Guid Size13x18Id   = new("b1000000-0000-0000-0000-000000000002");
    private static readonly Guid Size15x21Id   = new("b1000000-0000-0000-0000-000000000003");
    private static readonly Guid Size20x30Id   = new("b1000000-0000-0000-0000-000000000004");
    private static readonly Guid SizeA4Id      = new("b1000000-0000-0000-0000-000000000005");
    private static readonly Guid SizeA3Id      = new("b1000000-0000-0000-0000-000000000006");

    private static readonly Guid FinishGlossyId = new("c1000000-0000-0000-0000-000000000001");
    private static readonly Guid FinishMatteId  = new("c1000000-0000-0000-0000-000000000002");

    /// <summary>
    /// Applies seed data directly to the database. Idempotent — skips if the product already exists.
    /// Call explicitly (e.g. via <c>dotnet run --seed</c>) rather than from OnModelCreating.
    /// </summary>
    private static readonly Guid AdminUserId = new("e1000000-0000-0000-0000-000000000001");
    private const string AdminEmail = "mateibarba@yahoo.com";
    private const string AdminPassword = "Admin1234!";

    public static async Task ApplyAsync(PhotoPrintDbContext db, CancellationToken ct = default)
    {
        var alreadySeeded = await db.Products.AnyAsync(p => p.Id == ProductId, ct)
                         && await db.Users.AnyAsync(u => u.Id == AdminUserId, ct);
        if (alreadySeeded)
        {
            Console.WriteLine("Seed already applied — skipping.");
            return;
        }

        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        if (!await db.Products.AnyAsync(p => p.Id == ProductId, ct))
        {
        db.Products.Add(new Product
        {
            Id          = ProductId,
            Name        = "Poze foto",
            ProductType = "PhotoPrint",
            ImageUrl    = null,
            SortOrder   = 0,
            IsActive    = true,
            CreatedAt   = createdAt,
        });

        db.ProductSizes.AddRange(
            Size(Size10x15Id, "10×15", 100, 150),
            Size(Size13x18Id, "13×18", 130, 180),
            Size(Size15x21Id, "15×21", 150, 210),
            Size(Size20x30Id, "20×30", 200, 300),
            Size(SizeA4Id,    "A4",    210, 297),
            Size(SizeA3Id,    "A3",    297, 420)
        );

        db.ProductFinishes.AddRange(
            new ProductFinish { Id = FinishGlossyId, ProductId = ProductId, Name = "Lucioasă" },
            new ProductFinish { Id = FinishMatteId,  ProductId = ProductId, Name = "Mată" }
        );

        var tierOffset = 1;
        foreach (var (sizeId, t1, t2, t3) in SizePrices())
        {
            db.PricingTiers.AddRange(
                Tier(new Guid($"d1000000-0000-0000-0000-{tierOffset++:D12}"), sizeId,  1,  9,    t1),
                Tier(new Guid($"d1000000-0000-0000-0000-{tierOffset++:D12}"), sizeId, 10, 49,    t2),
                Tier(new Guid($"d1000000-0000-0000-0000-{tierOffset++:D12}"), sizeId, 50, null,  t3)
            );
        }
        }

        if (!await db.Users.AnyAsync(u => u.Id == AdminUserId, ct))
        {
            var admin = new User
            {
                Id                  = AdminUserId,
                Email               = AdminEmail,
                NormalizedEmail     = AdminEmail.ToUpperInvariant(),
                FirstName           = "Matei",
                LastName            = "Barba",
                Role                = UserRole.Admin,
                IsEmailConfirmed    = true,
                GdprConsentAccepted = true,
                CreatedAt           = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            };
            admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, AdminPassword);
            db.Users.Add(admin);
        }

        await db.SaveChangesAsync(ct);
        Console.WriteLine("Seed applied successfully.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static ProductSize Size(Guid id, string label, int w, int h) =>
        new() { Id = id, ProductId = ProductId, Label = label, WidthMm = w, HeightMm = h, IsActive = true };

    private static PricingTier Tier(Guid id, Guid sizeId, int min, int? max, decimal price) =>
        new() { Id = id, ProductSizeId = sizeId, MinQuantity = min, MaxQuantity = max, UnitPrice = price };

    /// <summary>Returns (sizeId, tier1Price, tier2Price, tier3Price) per size, prices in RON.</summary>
    private static IEnumerable<(Guid SizeId, decimal T1, decimal T2, decimal T3)> SizePrices() =>
    [
        (Size10x15Id, 1.20m, 0.90m, 0.70m),
        (Size13x18Id, 1.80m, 1.40m, 1.10m),
        (Size15x21Id, 2.20m, 1.70m, 1.30m),
        (Size20x30Id, 3.50m, 2.80m, 2.20m),
        (SizeA4Id,    3.00m, 2.40m, 1.90m),
        (SizeA3Id,    5.50m, 4.40m, 3.50m),
    ];
}

