using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs.Coupons;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Services.Coupons;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/admin/coupons")]
[Authorize(Roles = "Admin")]
public class AdminCouponsController(IAdminCouponService adminCouponService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await adminCouponService.ListAsync(status, page, size, cancellationToken);
        return Ok(new { items, total, page = Math.Max(page, 1), size = Math.Clamp(size, 1, AdminCouponService.MaxPageSize) });
    }

    [HttpPost]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CouponCreateRequest request,
        CancellationToken cancellationToken)
    {
        var coupon = await adminCouponService.CreateAsync(request, RequireAdminId(), cancellationToken);
        return Created($"/api/admin/coupons/{coupon.Id}", coupon);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] CouponUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var coupon = await adminCouponService.UpdateAsync(id, request, RequireAdminId(), cancellationToken);
        return Ok(coupon);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        await adminCouponService.DeactivateAsync(id, RequireAdminId(), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/redemptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListRedemptionsAsync(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await adminCouponService.ListRedemptionsAsync(id, page, size, cancellationToken);
        return Ok(new { items, total, page = Math.Max(page, 1), size = Math.Clamp(size, 1, AdminCouponService.MaxPageSize) });
    }

    private Guid RequireAdminId()
        => User.GetUserIdOrNull()
            ?? throw new UnauthorizedException("Sesiunea de administrator nu este validă.");
}
