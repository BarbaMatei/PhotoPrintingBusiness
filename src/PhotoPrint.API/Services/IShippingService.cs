using PhotoPrint.API.DTOs.Shipping;

namespace PhotoPrint.API.Services;

public interface IShippingService
{
    Task<IEnumerable<LockerDto>> GetLockersAsync(string city, CancellationToken ct = default);
    Task<ShippingCostDto> GetShippingCostAsync(string type, CancellationToken ct = default);
    Task<AwbResultDto> GenerateAwbAsync(Guid orderId, CancellationToken ct = default);
}
