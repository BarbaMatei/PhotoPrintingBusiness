using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class ShippingServiceTests : IDisposable
{
    private readonly PhotoPrintDbContext _db;
    private readonly StaticShippingService _service;

    public ShippingServiceTests()
    {
        var opts = new DbContextOptionsBuilder<PhotoPrintDbContext>()
            .UseInMemoryDatabase($"ShippingTests_{Guid.NewGuid():N}")
            .Options;
        _db = new PhotoPrintDbContext(opts);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Shipping:EasyboxCostRon"] = "20.00",
                ["Shipping:CourierCostRon"] = "25.00",
            })
            .Build();

        _service = new StaticShippingService(_db, config);
    }

    public void Dispose() => _db.Dispose();

    // ── GetLockersAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetLockers_EmptyDb_ReturnsEmptyList()
    {
        var result = await _service.GetLockersAsync("");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLockers_WithoutCityFilter_ReturnsAllActive()
    {
        await SeedLockersAsync();
        var result = await _service.GetLockersAsync("");
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetLockers_WithCityFilter_ReturnsCityMatch()
    {
        await SeedLockersAsync();
        var result = await _service.GetLockersAsync("Cluj");
        Assert.Single(result);
        Assert.All(result, l => Assert.Contains("Cluj", l.City));
    }

    [Fact]
    public async Task GetLockers_CityFilter_IsCaseInsensitive()
    {
        await SeedLockersAsync();
        var lower = await _service.GetLockersAsync("bucurești");
        var upper = await _service.GetLockersAsync("BUCUREȘTI");
        Assert.Equal(lower.Count(), upper.Count());
    }

    [Fact]
    public async Task GetLockers_InactiveLockers_AreExcluded()
    {
        await SeedLockersAsync();
        _db.EasyboxLockers.Add(new EasyboxLocker
        {
            SamedayId = "SMD-INACTIVE",
            Name = "Inactive",
            Address = "Str. Test 1",
            City = "București",
            County = "Ilfov",
            IsActive = false,
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetLockersAsync("București");
        Assert.All(result, l => Assert.True(l.Name != "Inactive"));
    }

    [Fact]
    public async Task GetLockers_EmptyCityFilter_ReturnsAll()
    {
        await SeedLockersAsync();
        var result = await _service.GetLockersAsync(string.Empty);
        Assert.Equal(3, result.Count());
    }

    // ── GetShippingCostAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCost_Easybox_Returns20()
    {
        var result = await _service.GetShippingCostAsync("Easybox");
        Assert.Equal(20.00m, result.CostRon);
    }

    [Fact]
    public async Task GetCost_Courier_Returns25()
    {
        var result = await _service.GetShippingCostAsync("Courier");
        Assert.Equal(25.00m, result.CostRon);
    }

    [Fact]
    public async Task GetCost_InvalidType_ThrowsBadRequestException()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.GetShippingCostAsync("DroneDrop"));
    }

    // ── GenerateAwbAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAwb_ReturnsManualTrue()
    {
        var result = await _service.GenerateAwbAsync(Guid.NewGuid());
        Assert.True(result.Manual);
        Assert.NotNull(result.Message);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SeedLockersAsync()
    {
        _db.EasyboxLockers.AddRange(
            new EasyboxLocker { SamedayId = "B-001", Name = "Kaufland Militari", Address = "Bd. Iuliu Maniu 1", City = "București", County = "Ilfov", Lat = 44.43, Lng = 26.00 },
            new EasyboxLocker { SamedayId = "B-002", Name = "Mega Image Unirii", Address = "Bd. Unirii 11",    City = "București", County = "Ilfov", Lat = 44.42, Lng = 26.10 },
            new EasyboxLocker { SamedayId = "CJ-001", Name = "Iulius Mall Cluj", Address = "Str. Vaida 53",   City = "Cluj-Napoca", County = "Cluj",  Lat = 46.75, Lng = 23.59 }
        );
        await _db.SaveChangesAsync();
    }
}
