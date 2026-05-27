using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class AdminStatsServiceTests
{
    private readonly PhotoPrintDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly AdminStatsService _sut;

    public AdminStatsServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"AdminStatsSvc_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new AdminStatsService(_db, _cache);
    }

    private async Task SeedOrderAsync(OrderStatus status, decimal total, DateTimeOffset? paidAt = null)
    {
        var order = new Order
        {
            OrderNumber = $"FT-{Guid.NewGuid():N}",
            Status = status,
            PaymentProcessor = PaymentProcessor.Stripe,
            DeliveryType = DeliveryType.Courier,
            ShippingAddress = new ShippingAddressSnapshot
            {
                RecipientName = "Test",
                Street = "St",
                Number = "1",
                City = "B",
                County = "IF",
                PostalCode = "010000",
                Phone = "07",
            },
            SubtotalRon = total,
            ShippingCostRon = 0m,
            TotalRon = total,
            PaidAt = paidAt,
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
    }

    // ── GetSummaryAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_CountsOnlyRevenueStatuses()
    {
        await SeedOrderAsync(OrderStatus.Paid, 100m);
        await SeedOrderAsync(OrderStatus.Printing, 50m);
        await SeedOrderAsync(OrderStatus.Cancelled, 200m);          // excluded
        await SeedOrderAsync(OrderStatus.AwaitingPayment, 30m);     // excluded
        await SeedOrderAsync(OrderStatus.PaymentFailed, 20m);       // excluded

        var summary = await _sut.GetSummaryAsync();

        // Month stats: only Paid + Printing = 2 orders
        summary.MonthOrders.Should().Be(2);
        summary.MonthRevenue.Should().Be(150m);
    }

    [Fact]
    public async Task GetSummaryAsync_CachesResult()
    {
        await SeedOrderAsync(OrderStatus.Paid, 100m);

        var first = await _sut.GetSummaryAsync();

        // Add another order — cache should serve stale data
        await SeedOrderAsync(OrderStatus.Paid, 200m);

        var second = await _sut.GetSummaryAsync();

        // Second call hits cache: same as first
        second.MonthOrders.Should().Be(first.MonthOrders);
        second.MonthRevenue.Should().Be(first.MonthRevenue);
    }

    // ── GetRevenueChartAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRevenueChartAsync_GroupsByDay()
    {
        var now = DateTimeOffset.UtcNow;
        await SeedOrderAsync(OrderStatus.Paid, 100m, now.AddDays(-1));
        await SeedOrderAsync(OrderStatus.Paid, 50m, now.AddDays(-1));
        await SeedOrderAsync(OrderStatus.Paid, 200m, now.AddDays(-2));

        var chart = await _sut.GetRevenueChartAsync(30);

        chart.Should().HaveCount(2);
        var yesterday = chart.First(d => d.Date == now.AddDays(-1).Date.ToString("yyyy-MM-dd"));
        yesterday.Revenue.Should().Be(150m);
    }

    [Fact]
    public async Task GetRevenueChartAsync_ExcludesCancelledOrders()
    {
        var now = DateTimeOffset.UtcNow;
        await SeedOrderAsync(OrderStatus.Paid, 100m, now.AddDays(-1));
        await SeedOrderAsync(OrderStatus.Cancelled, 999m, now.AddDays(-1));

        var chart = await _sut.GetRevenueChartAsync(30);

        var yesterday = chart.SingleOrDefault(d => d.Date == now.AddDays(-1).Date.ToString("yyyy-MM-dd"));
        yesterday.Should().NotBeNull();
        yesterday!.Revenue.Should().Be(100m);
    }

    // ── GetOrdersByStatusAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersByStatusAsync_ReturnsGroupedCounts()
    {
        await SeedOrderAsync(OrderStatus.Paid, 50m);
        await SeedOrderAsync(OrderStatus.Paid, 50m);
        await SeedOrderAsync(OrderStatus.Printing, 80m);

        var result = await _sut.GetOrdersByStatusAsync();

        var paidGroup = result.Single(r => r.Status == "Paid");
        paidGroup.Count.Should().Be(2);

        var printingGroup = result.Single(r => r.Status == "Printing");
        printingGroup.Count.Should().Be(1);
    }
}
