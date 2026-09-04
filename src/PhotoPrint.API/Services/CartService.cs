using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Cart;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services.Coupons;

namespace PhotoPrint.API.Services;

public class CartService : ICartService
{
    private readonly PhotoPrintDbContext _db;
    private readonly ICouponService _coupons;
    private readonly VatSettings _vatSettings;

    public CartService(
        PhotoPrintDbContext db,
        ICouponService coupons,
        IOptions<VatSettings> vatSettings)
    {
        _db = db;
        _coupons = coupons;
        _vatSettings = vatSettings.Value;
    }

    // ── GetCart ──────────────────────────────────────────────────────────────────

    public async Task<CartResponseDto> GetCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default)
    {
        var items = await LoadCartItemsAsync(userId, guestSessionId, ct);

        if (items.Count == 0)
            return EmptyCart();

        return await BuildResponseAsync(items, userId, guestSessionId, ct);
    }

    public async Task<CartResponseDto> ApplyCouponAsync(
        Guid? userId,
        Guid? guestSessionId,
        string code,
        CancellationToken ct = default)
    {
        var items = await LoadCartItemsAsync(userId, guestSessionId, ct);
        var subtotal = SubtotalOf(items);

        await _coupons.ApplyToCartAsync(userId, guestSessionId, code, subtotal, ct);

        return await BuildResponseAsync(items, userId, guestSessionId, ct);
    }

    public async Task<CartResponseDto> ClearCouponAsync(
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default)
    {
        await _coupons.ClearCartCouponAsync(userId, guestSessionId, ct);

        var items = await LoadCartItemsAsync(userId, guestSessionId, ct);
        if (items.Count == 0)
            return EmptyCart();

        return await BuildResponseAsync(items, userId, guestSessionId, ct);
    }

    // ── SetCart ──────────────────────────────────────────────────────────────────

    public async Task<CartResponseDto> SetCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        CartRequest request,
        CancellationToken ct = default)
    {
        // Validate product is active
        var product = await _db.Products
            .Include(p => p.Sizes).ThenInclude(s => s.PricingTiers)
            .Include(p => p.Finishes)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

        if (product == null || !product.IsActive)
            throw new BadRequestException("Produsul selectat nu există sau nu este disponibil.");

        // Validate size belongs to this product and is active
        var size = product.Sizes.FirstOrDefault(s => s.Id == request.SizeId && s.IsActive);
        if (size == null)
            throw new BadRequestException("Dimensiunea selectată nu este disponibilă.");

        // Validate upload ownership for every item
        var uploadIds = request.Items.Select(i => i.UploadId).ToList();
        var uploads = await _db.Uploads
            .Where(u => uploadIds.Contains(u.Id) && u.DeletedAt == null)
            .ToListAsync(ct);

        // Ensure every requested uploadId was found
        var foundIds = uploads.Select(u => u.Id).ToHashSet();
        var missing = uploadIds.FirstOrDefault(id => !foundIds.Contains(id));
        if (missing != default)
            throw new NotFoundException($"Fotografia {missing} nu a fost găsită.");

        // Ensure each upload belongs to the calling user/session
        foreach (var upload in uploads)
        {
            var owned = userId.HasValue
                ? upload.UserId == userId
                : upload.GuestSessionId == guestSessionId;

            if (!owned)
                throw new ForbiddenException("Nu aveți acces la una sau mai multe fotografii selectate.");
        }

        // Replace strategy: delete only items for this product+size+finish, keep other groups intact.
        var useTransaction = _db.Database.ProviderName != DbProviders.InMemory;
        IDbContextTransaction? tx = useTransaction
            ? await _db.Database.BeginTransactionAsync(ct)
            : null;
        await using var _ = tx;

        var existing = await _db.CartItems
            .Where(ci => (userId.HasValue ? ci.UserId == userId : ci.GuestSessionId == guestSessionId)
                && ci.ProductId == request.ProductId
                && ci.SizeId == request.SizeId
                && ci.FinishName == request.FinishName)
            .ToListAsync(ct);

        _db.CartItems.RemoveRange(existing);

        var uploadMap = uploads.ToDictionary(u => u.Id);

        foreach (var item in request.Items)
        {
            _db.CartItems.Add(new CartItem
            {
                UserId = userId,
                GuestSessionId = guestSessionId,
                UploadId = item.UploadId,
                ProductId = request.ProductId,
                SizeId = request.SizeId,
                FinishName = request.FinishName,
                Quantity = item.Quantity,
                AddedAt = DateTimeOffset.UtcNow,
            });
        }

        await _db.SaveChangesAsync(ct);
        if (tx != null) await tx.CommitAsync(ct);

        var newItems = await LoadCartItemsAsync(userId, guestSessionId, ct);
        return await BuildResponseAsync(newItems, userId, guestSessionId, ct);
    }

    // ── ClearCart ────────────────────────────────────────────────────────────────

    public async Task ClearCartAsync(
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default)
    {
        var items = await _db.CartItems
            .Where(ci => userId.HasValue
                ? ci.UserId == userId
                : ci.GuestSessionId == guestSessionId)
            .ToListAsync(ct);

        if (items.Count > 0)
        {
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync(ct);
        }

        await _coupons.ClearCartCouponAsync(userId, guestSessionId, ct);
    }

    // ── MergeCart ────────────────────────────────────────────────────────────────

    public async Task<CartResponseDto> MergeCartsAsync(
        Guid userId,
        Guid guestSessionId,
        CancellationToken ct = default)
    {
        var useTransaction = _db.Database.ProviderName != DbProviders.InMemory;
        IDbContextTransaction? tx = useTransaction
            ? await _db.Database.BeginTransactionAsync(ct)
            : null;
        await using var _ = tx;
        var guestItems = await _db.CartItems
            .Include(ci => ci.Upload)
            .Where(ci => ci.GuestSessionId == guestSessionId)
            .ToListAsync(ct);

        if (guestItems.Count == 0)
        {
            await _coupons.TransferGuestCouponAsync(userId, guestSessionId, ct);
            if (tx != null) await tx.CommitAsync(ct);
            return await GetCartAsync(userId, null, ct);
        }

        // Load set of uploadIds already in user's cart (for conflict detection)
        var userUploadIds = (await _db.CartItems
            .Where(ci => ci.UserId == userId)
            .Select(ci => ci.UploadId)
            .ToListAsync(ct))
            .ToHashSet();

        // Transfer upload ownership and insert non-conflicting items
        foreach (var guestItem in guestItems)
        {
            // Transfer the upload to the authenticated user
            var upload = guestItem.Upload;
            if (upload != null && upload.UserId == null)
            {
                upload.UserId = userId;
                upload.GuestSessionId = null;
            }

            // Only add to user cart if there's no existing item for this upload
            if (!userUploadIds.Contains(guestItem.UploadId))
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    GuestSessionId = null,
                    UploadId = guestItem.UploadId,
                    ProductId = guestItem.ProductId,
                    SizeId = guestItem.SizeId,
                    FinishName = guestItem.FinishName,
                    Quantity = guestItem.Quantity,
                    AddedAt = guestItem.AddedAt,
                });
            }
        }

        // Remove all guest cart items
        _db.CartItems.RemoveRange(guestItems);

        await _db.SaveChangesAsync(ct);
        await _coupons.TransferGuestCouponAsync(userId, guestSessionId, ct);
        if (tx != null) await tx.CommitAsync(ct);

        return await GetCartAsync(userId, null, ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    private async Task<List<CartItem>> LoadCartItemsAsync(
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct)
    {
        return await _db.CartItems
            .Where(ci => userId.HasValue
                ? ci.UserId == userId
                : ci.GuestSessionId == guestSessionId)
            .Include(ci => ci.Upload)
            .Include(ci => ci.Product)
                .ThenInclude(p => p.Finishes)
            .Include(ci => ci.Size)
                .ThenInclude(s => s.PricingTiers)
            .Where(ci => ci.Upload.DeletedAt == null)
            .OrderBy(ci => ci.AddedAt)
            .ToListAsync(ct);
    }

    private async Task<CartResponseDto> BuildResponseAsync(
        List<CartItem> items,
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct)
    {
        var cart = BuildGroups(items);
        if (cart.Groups.Count == 0)
            return cart;

        var applied = await _coupons.ResolveForCartAsync(
            userId, guestSessionId, cart.Subtotal, ct);

        var discount = applied is { IsStale: false } ? applied.DiscountRon : 0m;
        var total = cart.Subtotal - discount;
        var vat = VatCalculator.ExtractBreakdown(total, _vatSettings.Rate);

        return cart with
        {
            CouponCode = applied?.Code,
            CouponType = applied?.Type.ToString(),
            CouponStatus = applied is null
                ? null
                : applied.IsStale ? CouponCartStatus.Stale : CouponCartStatus.Valid,
            CouponReason = applied?.ReasonCode,
            DiscountRon = discount,
            TotalRon = total,
            NetTotalRon = vat.NetTotalRon,
            VatRon = vat.VatRon,
            VatRate = vat.VatRate,
        };
    }

    private CartResponseDto EmptyCart()
        => CartResponseDto.Empty with { VatRate = _vatSettings.Rate };

    private static decimal SubtotalOf(List<CartItem> items)
        => BuildGroups(items).Subtotal;

    private static CartResponseDto BuildGroups(List<CartItem> items)
    {
        if (items.Count == 0)
            return CartResponseDto.Empty;

        // Group by product + size + finish so each combination gets its own card and correct tier pricing.
        var groups = items
            .GroupBy(ci => (ci.ProductId, ci.SizeId, ci.FinishName))
            .OrderBy(g => g.Key.ProductId)
            .ThenBy(g => g.Key.SizeId)
            .ThenBy(g => g.Key.FinishName)
            .Select(g =>
            {
                var product = g.First().Product;
                var size = g.First().Size;
                var finishName = g.Key.FinishName;

                // Tier price is based on TOTAL copies across all photos in this group.
                var totalCopies = g.Sum(ci => ci.Quantity);
                var unitPrice = ResolveUnitPrice(size, totalCopies);

                var dtoItems = g.Select(ci => new CartItemDto(
                    UploadId: ci.UploadId,
                    Quantity: ci.Quantity,
                    PreviewUrl: $"/api/uploads/{ci.UploadId}/preview",
                    UnitPrice: unitPrice,
                    LineTotal: unitPrice * ci.Quantity,
                    WidthPx: ci.Upload.WidthPx,
                    HeightPx: ci.Upload.HeightPx)).ToList();

                return new CartGroupDto(
                    ProductId: product.Id,
                    ProductName: product.Name,
                    SizeId: size.Id,
                    SizeName: size.Label,
                    FinishName: finishName,
                    Items: dtoItems,
                    TotalCopies: totalCopies,
                    UnitPrice: unitPrice,
                    Subtotal: dtoItems.Sum(i => i.LineTotal));
            })
            .ToList();

        return new CartResponseDto(
            Groups: groups,
            Subtotal: groups.Sum(g => g.Subtotal),
            ItemCount: groups.Sum(g => g.Items.Count),
            CouponCode: null,
            CouponType: null,
            CouponStatus: null,
            CouponReason: null,
            DiscountRon: 0m,
            TotalRon: groups.Sum(g => g.Subtotal),
            NetTotalRon: 0m,
            VatRon: 0m,
            VatRate: 0m);
    }

    /// <summary>
    /// Resolves unit price from the specific size's PricingTiers using the total copy count.
    /// Falls back to the highest-minQuantity tier if no tier brackets the quantity.
    /// Returns 0 if no tiers are defined. The bracket rule is shared with OrderService via
    /// <see cref="PricingTierResolver"/>; the tier source (this size's
    /// tiers) and quantity (per-group total copies) are this call site's own semantics.
    /// </summary>
    private static decimal ResolveUnitPrice(ProductSize size, int totalQuantity)
        => PricingTierResolver.Resolve(size.PricingTiers, totalQuantity);
}
