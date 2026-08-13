using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // ── GET /api/orders ───────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1)
            return BadRequest("page must be ≥ 1.");
        if (pageSize < 1 || pageSize > 50)
            return BadRequest("pageSize must be between 1 and 50.");

        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        var (items, total) = await _orderService.GetOrdersAsync(userId.Value, page, pageSize, ct);

        Response.Headers["X-Total-Count"] = total.ToString();

        return Ok(new { items, total, page, size = pageSize });
    }

    // ── GET /api/orders/{id} ──────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderDetail(
        Guid id,
        CancellationToken ct = default)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        var dto = await _orderService.GetOrderDetailAsync(id, userId.Value, ct);
        return Ok(dto);
    }

    // ── GET /api/orders/{id}/photos ────────────────────────────────

    [HttpGet("{id:guid}/photos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderPhotos(
        Guid id,
        CancellationToken ct = default)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        var dto = await _orderService.GetOrderPhotosAsync(id, userId.Value, ct);

        // The payload embeds per-user presigned URLs, so it must never sit in a shared cache
        //  — matches the preview endpoint's private-cache posture.
        Response.Headers.CacheControl = "private, no-store";
        return Ok(dto);
    }
}
