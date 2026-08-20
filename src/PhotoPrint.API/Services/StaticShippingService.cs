using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.API.Exceptions;

namespace PhotoPrint.API.Services;

public class StaticShippingService : IShippingService
{
    private readonly PhotoPrintDbContext _db;
    private readonly IConfiguration _config;

    public StaticShippingService(PhotoPrintDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<IEnumerable<LockerDto>> GetLockersAsync(
        string city,
        CancellationToken ct = default)
    {
        var query = _db.EasyboxLockers.Where(l => l.IsActive);

        if (!string.IsNullOrWhiteSpace(city))
        {
            // ILike is PostgreSQL-specific; fall back to ToLower().Contains for InMemory.
            if (_db.Database.ProviderName == DbProviders.Postgres)
                query = query.Where(l => EF.Functions.ILike(l.City, $"%{city}%"));
            else
                query = query.Where(l => l.City.ToLower().Contains(city.ToLower()));
        }

        return await query
            .OrderBy(l => l.City)
            .ThenBy(l => l.Name)
            .Select(l => new LockerDto(l.Id, l.SamedayId, l.Name, l.Address, l.City, l.Lat, l.Lng))
            .ToListAsync(ct);
    }

    public Task<ShippingCostDto> GetShippingCostAsync(
        string type,
        CancellationToken ct = default)
    {
        var costRon = type switch
        {
            "Easybox" => _config.GetValue<decimal>("Shipping:EasyboxCostRon"),
            "Courier"  => _config.GetValue<decimal>("Shipping:CourierCostRon"),
            _ => throw new BadRequestException($"Tipul de livrare '{type}' nu este valid. Valorile acceptate: Easybox, Courier."),
        };

        return Task.FromResult(new ShippingCostDto(costRon));
    }

    public Task<AwbResultDto> GenerateAwbAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        return Task.FromResult(new AwbResultDto(
            Manual: true,
            Message: "AWB se generează manual în portalul Sameday"));
    }
}
