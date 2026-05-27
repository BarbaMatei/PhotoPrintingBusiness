using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs.Shipping;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;

    public ShippingController(IShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    /// <summary>Returns Easybox lockers, optionally filtered by city (case-insensitive partial match).</summary>
    [HttpGet("lockers")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LockerDto>>> GetLockers(
        [FromQuery] string? city,
        CancellationToken ct)
    {
        var lockers = await _shippingService.GetLockersAsync(city ?? string.Empty, ct);
        return Ok(lockers);
    }

    /// <summary>Returns the shipping cost for the given delivery type (Easybox or Courier).</summary>
    [HttpGet("cost")]
    [AllowAnonymous]
    public async Task<ActionResult<ShippingCostDto>> GetCost(
        [FromQuery] string type,
        CancellationToken ct)
    {
        var cost = await _shippingService.GetShippingCostAsync(type, ct);
        return Ok(cost);
    }

    /// <summary>Generates an AWB for the given order. Admin-only (Phase 1: manual stub).</summary>
    [HttpPost("awb")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AwbResultDto>> GenerateAwb(
        [FromBody] AwbRequest request,
        CancellationToken ct)
    {
        var result = await _shippingService.GenerateAwbAsync(request.OrderId, ct);
        return Ok(result);
    }
}
