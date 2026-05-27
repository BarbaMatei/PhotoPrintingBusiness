using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class AdminStatsService : IAdminStatsService
{
    private readonly PhotoPrintDbContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public AdminStatsService(PhotoPrintDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    public async Task<AdminStatsDto> GetSummaryAsync(CancellationToken ct = default)
    {
        return (await _cache.GetOrCreateAsync("admin_stats_summary", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var now = DateTimeOffset.UtcNow;
            var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            var tomorrowStart = todayStart.AddDays(1);
            var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

            var todayOrders = await _db.Orders
                .CountAsync(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart
                    && o.Status != OrderStatus.Cancelled
                    && o.Status != OrderStatus.PaymentFailed
                    && o.Status != OrderStatus.AwaitingPayment, ct);

            var todayRevenue = (await _db.Orders
                .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart
                    && o.Status != OrderStatus.Cancelled
                    && o.Status != OrderStatus.PaymentFailed
                    && o.Status != OrderStatus.AwaitingPayment)
                .Select(o => o.TotalRon)
                .ToListAsync(ct)).Sum();

            var monthOrders = await _db.Orders
                .CountAsync(o => o.CreatedAt >= monthStart
                    && o.Status != OrderStatus.Cancelled
                    && o.Status != OrderStatus.PaymentFailed
                    && o.Status != OrderStatus.AwaitingPayment, ct);

            var monthRevenue = (await _db.Orders
                .Where(o => o.CreatedAt >= monthStart
                    && o.Status != OrderStatus.Cancelled
                    && o.Status != OrderStatus.PaymentFailed
                    && o.Status != OrderStatus.AwaitingPayment)
                .Select(o => o.TotalRon)
                .ToListAsync(ct)).Sum();

            return new AdminStatsDto(todayOrders, todayRevenue, monthOrders, monthRevenue);
        }))!;
    }

    // ── Revenue chart ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RevenueDataPointDto>> GetRevenueChartAsync(
        int days, CancellationToken ct = default)
    {
        return (await _cache.GetOrCreateAsync($"admin_stats_revenue_{days}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var from = DateTimeOffset.UtcNow.AddDays(-days);

            var rows = await _db.Orders
                .Where(o => o.PaidAt.HasValue && o.PaidAt >= from && o.Status != OrderStatus.Cancelled)
                .Select(o => new { o.PaidAt, o.TotalRon })
                .ToListAsync(ct);

            IReadOnlyList<RevenueDataPointDto> result = rows
                .GroupBy(o => o.PaidAt!.Value.Date)
                .Select(g => new RevenueDataPointDto(
                    g.Key.ToString("yyyy-MM-dd"),
                    g.Sum(o => o.TotalRon)))
                .OrderBy(d => d.Date)
                .ToList();

            return result;
        }))!;
    }

    // ── Product stats ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ProductStatsDto>> GetProductStatsAsync(CancellationToken ct = default)
    {
        return (await _cache.GetOrCreateAsync("admin_stats_products", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var items = await _db.OrderItems
                .Where(i => i.Order.Status != OrderStatus.Cancelled
                    && i.Order.Status != OrderStatus.PaymentFailed
                    && i.Order.Status != OrderStatus.AwaitingPayment)
                .Select(i => new { i.ProductSnapshot.ProductName, i.Quantity })
                .ToListAsync(ct);

            IReadOnlyList<ProductStatsDto> result = items
                .GroupBy(i => i.ProductName)
                .Select(g => new ProductStatsDto(g.Key, g.Sum(i => i.Quantity), g.Count()))
                .OrderByDescending(s => s.TotalQuantity)
                .Take(10)
                .ToList();

            return result;
        }))!;
    }

    // ── Orders by status ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<OrdersByStatusDto>> GetOrdersByStatusAsync(CancellationToken ct = default)
    {
        return (await _cache.GetOrCreateAsync("admin_stats_orders_by_status", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            IReadOnlyList<OrdersByStatusDto> result = await _db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new OrdersByStatusDto(g.Key.ToString(), g.Count()))
                .ToListAsync(ct);

            return result;
        }))!;
    }
}
