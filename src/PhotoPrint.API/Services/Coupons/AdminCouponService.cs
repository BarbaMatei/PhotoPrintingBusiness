using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Coupons;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Coupons;

public sealed class AdminCouponService : IAdminCouponService
{
    public const int MaxPageSize = 100;

    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<AdminCouponService> _logger;

    public AdminCouponService(PhotoPrintDbContext db, ILogger<AdminCouponService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private bool UsesRelationalProvider
        => _db.Database.ProviderName != DbProviders.InMemory;

    public async Task<(IReadOnlyList<CouponDto> Items, int Total)> ListAsync(
        string? status, int page, int size, CancellationToken ct = default)
    {
        (page, size) = ClampPaging(page, size);

        var now = DateTimeOffset.UtcNow;
        var query = _db.Coupons.AsNoTracking();

        query = status?.ToLowerInvariant() switch
        {
            "active" => query.Where(c => c.IsActive && c.ValidUntil > now),
            "inactive" => query.Where(c => !c.IsActive),
            "expired" => query.Where(c => c.ValidUntil <= now),
            _ => query,
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<CouponDto> CreateAsync(
        CouponCreateRequest request, Guid adminUserId, CancellationToken ct = default)
    {
        var code = CouponCode.Normalize(request.Code);

        if (await _db.Coupons.AnyAsync(c => c.Code == code, ct))
            throw DuplicateCode(code, adminUserId);

        var coupon = new Coupon
        {
            Code = code,
            Type = ParseType(request.Type),
            Value = request.Value,
            MinSubtotalRon = request.MinSubtotalRon,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            MaxRedemptions = request.MaxRedemptions,
            IsActive = true,
        };

        _db.Coupons.Add(coupon);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsCodeViolation(ex))
        {
            _db.Entry(coupon).State = EntityState.Detached;
            throw DuplicateCode(code, adminUserId);
        }

        _logger.LogInformation(
            "admin.coupon.created coupon_id={CouponId} code={Code} admin_user_id={AdminUserId}",
            coupon.Id, coupon.Code, adminUserId);

        return ToDto(coupon);
    }

    public async Task<CouponDto> UpdateAsync(
        Guid id, CouponUpdateRequest request, Guid adminUserId, CancellationToken ct = default)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"Cuponul {id} nu a fost găsit.");

        var newCode = CouponCode.Normalize(request.Code);

        if (newCode != coupon.Code)
        {
            if (await _db.Coupons.AnyAsync(c => c.Code == newCode && c.Id != id, ct))
                throw DuplicateCode(newCode, adminUserId);

            await RenameOrThrowAsync(id, newCode, adminUserId, ct);
            coupon.Code = newCode;
        }

        coupon.Type = ParseType(request.Type);
        coupon.Value = request.Value;
        coupon.MinSubtotalRon = request.MinSubtotalRon;
        coupon.ValidFrom = request.ValidFrom;
        coupon.ValidUntil = request.ValidUntil;
        coupon.MaxRedemptions = request.MaxRedemptions;
        coupon.IsActive = request.IsActive;
        coupon.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsCodeViolation(ex))
        {
            throw DuplicateCode(newCode, adminUserId);
        }

        _logger.LogInformation(
            "admin.coupon.updated coupon_id={CouponId} code={Code} admin_user_id={AdminUserId}",
            coupon.Id, coupon.Code, adminUserId);

        return ToDto(coupon);
    }

    public async Task DeactivateAsync(Guid id, Guid adminUserId, CancellationToken ct = default)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"Cuponul {id} nu a fost găsit.");

        if (!coupon.IsActive)
        {
            _logger.LogInformation(
                "admin.coupon.deactivate-rejected coupon_id={CouponId} reason={Reason} admin_user_id={AdminUserId}",
                id, CouponErrorCodes.CouponAlreadyInactive, adminUserId);
            throw new CouponConflictException(
                CouponErrorCodes.CouponAlreadyInactive,
                CouponMessages.For(CouponErrorCodes.CouponAlreadyInactive));
        }

        coupon.IsActive = false;
        coupon.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "admin.coupon.deactivated coupon_id={CouponId} code={Code} admin_user_id={AdminUserId}",
            coupon.Id, coupon.Code, adminUserId);
    }

    public async Task<(IReadOnlyList<CouponRedemptionDto> Items, int Total)> ListRedemptionsAsync(
        Guid couponId, int page, int size, CancellationToken ct = default)
    {
        (page, size) = ClampPaging(page, size);

        if (!await _db.Coupons.AnyAsync(c => c.Id == couponId, ct))
            throw new NotFoundException($"Cuponul {couponId} nu a fost găsit.");

        var query = _db.CouponRedemptions
            .AsNoTracking()
            .Where(r => r.CouponId == couponId);

        var total = await query.CountAsync(ct);

        var items = await query
            .Join(_db.Orders.AsNoTracking(),
                r => r.OrderId,
                o => o.Id,
                (r, o) => new { r.Id, r.OrderId, o.OrderNumber, r.UserId, r.DiscountRon, r.RedeemedAt })
            .OrderByDescending(x => x.RedeemedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new CouponRedemptionDto(
                x.Id, x.OrderId, x.OrderNumber, x.UserId, x.DiscountRon, x.RedeemedAt))
            .ToListAsync(ct);

        return (items, total);
    }

    private async Task RenameOrThrowAsync(
        Guid id, string newCode, Guid adminUserId, CancellationToken ct)
    {
        int affected;

        if (UsesRelationalProvider)
        {
            try
            {
                affected = await _db.Coupons
                    .Where(c => c.Id == id && c.RedemptionsCount == 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Code, newCode), ct);
            }
            catch (Npgsql.PostgresException pg) when (IsCodeViolation(pg))
            {
                throw DuplicateCode(newCode, adminUserId);
            }
        }
        else
        {
            var current = await _db.Coupons
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            affected = current is { RedemptionsCount: 0 } ? 1 : 0;
        }

        if (affected == 1) return;

        _logger.LogInformation(
            "admin.coupon.update-rejected coupon_id={CouponId} reason={Reason} admin_user_id={AdminUserId}",
            id, CouponErrorCodes.CodeImmutableAfterRedemption, adminUserId);

        throw new CouponConflictException(
            CouponErrorCodes.CodeImmutableAfterRedemption,
            CouponMessages.For(CouponErrorCodes.CodeImmutableAfterRedemption));
    }

    private CouponConflictException DuplicateCode(string code, Guid adminUserId)
    {
        _logger.LogInformation(
            "admin.coupon.create-rejected code={Code} reason={Reason} admin_user_id={AdminUserId}",
            code, CouponErrorCodes.DuplicateCode, adminUserId);

        return new CouponConflictException(
            CouponErrorCodes.DuplicateCode, CouponMessages.For(CouponErrorCodes.DuplicateCode));
    }

    private static bool IsCodeViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException pg && IsCodeViolation(pg);

    private static bool IsCodeViolation(Npgsql.PostgresException pg)
        => pg.SqlState == "23505"
            && pg.ConstraintName == PhotoPrintDbContext.CouponCodeIndexName;

    private static (int Page, int Size) ClampPaging(int page, int size)
        => (Math.Max(page, 1), Math.Clamp(size, 1, MaxPageSize));

    private static CouponType ParseType(string raw)
        => Enum.TryParse<CouponType>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : throw new UnprocessableEntityException(
                "Tipul cuponului trebuie să fie Percent, Fixed sau FreeShipping.");

    private static CouponDto ToDto(Coupon c)
        => new(c.Id, c.Code, c.Type.ToString(), c.Value, c.MinSubtotalRon, c.ValidFrom,
            c.ValidUntil, c.MaxRedemptions, c.RedemptionsCount, c.IsActive, c.CreatedAt, c.UpdatedAt);
}
