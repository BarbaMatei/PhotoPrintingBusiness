using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Data;
using PhotoPrint.API.Services;
using PhotoPrint.API.Services.Sameday;

namespace PhotoPrint.Tests.Unit.Services.Sameday;

/// <summary>
/// <see cref="SamedayShippingService"/> is mostly a delegating shell in bolt 036:
///   - lockers + costs are produced by the wrapped <see cref="StaticShippingService"/>,
///   - AWB creation returns the manual-fallback DTO until bolt 037 lands.
/// These tests pin that behaviour so the swap-in does not silently break the
/// pre-bolt UX.
/// </summary>
public class SamedayShippingServiceTests : IDisposable
{
    private readonly PhotoPrintDbContext _db;
    private readonly IConfiguration _config;
    private readonly SamedayShippingService _sut;

    public SamedayShippingServiceTests()
    {
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"SamedayShipping_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(opts);

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Shipping:EasyboxCostRon"] = "20.00",
                ["Shipping:CourierCostRon"] = "25.00",
            })
            .Build();

        _sut = new SamedayShippingService(
            Mock.Of<ISamedayClient>(),
            new StaticShippingService(_db, _config),
            new LoggerFactory().CreateLogger<SamedayShippingService>());
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetShippingCostAsync_delegates_to_static_service_for_easybox()
    {
        var cost = await _sut.GetShippingCostAsync("Easybox");
        cost.CostRon.Should().Be(20.00m);
    }

    [Fact]
    public async Task GetShippingCostAsync_delegates_to_static_service_for_courier()
    {
        var cost = await _sut.GetShippingCostAsync("Courier");
        cost.CostRon.Should().Be(25.00m);
    }

    [Fact]
    public async Task GetLockersAsync_returns_empty_when_no_lockers_seeded()
    {
        var lockers = await _sut.GetLockersAsync(string.Empty);
        lockers.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAwbAsync_reports_automatic_creation_not_manual()
    {
        // AWB creation is event-driven (background jobs) — the synchronous endpoint must not
        // tell the admin to create one manually, which would double-book alongside the job.
        var result = await _sut.GenerateAwbAsync(Guid.NewGuid());
        result.Manual.Should().BeFalse();
        result.Message.Should().Contain("automat");
    }
}
