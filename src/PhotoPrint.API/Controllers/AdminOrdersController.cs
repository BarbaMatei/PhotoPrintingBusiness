using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(IAdminOrderService adminOrderService) : ControllerBase
{
    // GET /api/admin/orders?page=1&pageSize=20&status=Paid&search=FT-001
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await adminOrderService.GetOrdersAsync(
            page, pageSize, status, search, cancellationToken);

        return Ok(new { items, total, page, pageSize });
    }

    // GET /api/admin/orders/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderDetailAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var detail = await adminOrderService.GetOrderDetailAsync(id, cancellationToken);
        return Ok(detail);
    }

    // PATCH /api/admin/orders/{id}/status
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(AdminOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatusAsync(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var detail = await adminOrderService.UpdateStatusAsync(
            id, request.Status, request.AwbNumber, request.TrackingUrl, cancellationToken);
        return Ok(detail);
    }

    // GET /api/admin/orders/{id}/download-zip
    [HttpGet("{id:guid}/download-zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadZipAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        await adminOrderService.StreamZipAsync(id, Response, cancellationToken);
        return new EmptyResult();
    }

    // POST /api/admin/orders/{id}/cancel
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(AdminOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrderAsync(
        Guid id,
        [FromBody] CancelOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var detail = await adminOrderService.CancelOrderAsync(id, request?.Reason, cancellationToken);
        return Ok(detail);
    }

    // PATCH /api/admin/orders/{id}/notes
    [HttpPatch("{id:guid}/notes")]
    [ProducesResponseType(typeof(AdminOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotesAsync(
        Guid id,
        [FromBody] UpdateOrderNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        var detail = await adminOrderService.UpdateNotesAsync(id, request.Notes, cancellationToken);
        return Ok(detail);
    }
}
