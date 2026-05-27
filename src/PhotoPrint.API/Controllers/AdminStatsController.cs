using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs.Admin;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/admin/stats")]
[Authorize(Roles = "Admin")]
public class AdminStatsController(IAdminStatsService adminStatsService) : ControllerBase
{
    // GET /api/admin/stats/summary
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AdminStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var stats = await adminStatsService.GetSummaryAsync(cancellationToken);
        return Ok(stats);
    }

    // GET /api/admin/stats/revenue?days=30
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(IReadOnlyList<RevenueDataPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueChartAsync(
        [FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 365);
        var data = await adminStatsService.GetRevenueChartAsync(days, cancellationToken);
        return Ok(data);
    }

    // GET /api/admin/stats/products
    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductStatsAsync(CancellationToken cancellationToken = default)
    {
        var data = await adminStatsService.GetProductStatsAsync(cancellationToken);
        return Ok(data);
    }

    // GET /api/admin/stats/orders-by-status
    [HttpGet("orders-by-status")]
    [ProducesResponseType(typeof(IReadOnlyList<OrdersByStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersByStatusAsync(CancellationToken cancellationToken = default)
    {
        var data = await adminStatsService.GetOrdersByStatusAsync(cancellationToken);
        return Ok(data);
    }
}
