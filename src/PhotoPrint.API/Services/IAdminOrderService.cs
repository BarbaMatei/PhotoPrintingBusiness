using Microsoft.AspNetCore.Http;
using PhotoPrint.API.DTOs.Admin;

namespace PhotoPrint.API.Services;

public interface IAdminOrderService
{
    Task<(IReadOnlyList<AdminOrderSummaryDto> Items, int Total)> GetOrdersAsync(
        int page, int pageSize, string? status, string? search, CancellationToken ct = default);

    Task<AdminOrderDetailDto> GetOrderDetailAsync(Guid orderId, CancellationToken ct = default);

    Task<AdminOrderDetailDto> UpdateStatusAsync(
        Guid orderId, string status, string? awbNumber, string? trackingUrl,
        Guid? adminUserId = null, CancellationToken ct = default);

    Task StreamZipAsync(Guid orderId, HttpResponse response, CancellationToken ct = default);

    Task<AdminOrderDetailDto> CancelOrderAsync(Guid orderId, string? reason, CancellationToken ct = default);

    Task<AdminOrderDetailDto> UpdateNotesAsync(
        Guid orderId, string? notes, CancellationToken ct = default);
}
