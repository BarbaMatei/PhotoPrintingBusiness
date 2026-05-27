using PhotoPrint.API.DTOs.Admin;

namespace PhotoPrint.API.Services;

public interface IAdminStatsService
{
    Task<AdminStatsDto> GetSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RevenueDataPointDto>> GetRevenueChartAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<ProductStatsDto>> GetProductStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<OrdersByStatusDto>> GetOrdersByStatusAsync(CancellationToken ct = default);
}
