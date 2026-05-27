using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.API.Models;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// Extends <see cref="CartFactory"/> with shipping-specific seed helpers.
/// Also injects shipping config overrides into the in-memory test environment.
/// </summary>
public class ShippingFactory : CartFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Shipping:EasyboxCostRon"] = "20.00",
                ["Shipping:CourierCostRon"] = "25.00",
            });
        });
    }

    /// <summary>Seeds Easybox lockers for integration tests.</summary>
    public async Task<List<EasyboxLocker>> SeedLockersAsync(string city = "București", int count = 3)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

        var lockers = Enumerable.Range(1, count).Select(i => new EasyboxLocker
        {
            SamedayId = $"SMD-{city.ToUpperInvariant()}-{i:D3}",
            Name = $"{city} Locker {i}",
            Address = $"Str. Test {i}",
            City = city,
            County = "Ilfov",
            Lat = 44.43 + i * 0.01,
            Lng = 26.00 + i * 0.01,
            IsActive = true,
        }).ToList();

        db.EasyboxLockers.AddRange(lockers);
        await db.SaveChangesAsync();
        return lockers;
    }
}
