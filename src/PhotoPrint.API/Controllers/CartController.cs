using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.DTOs.Cart;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // GET /api/cart
    [HttpGet]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCartAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var result = await _cartService.GetCartAsync(userId, guestSessionId, cancellationToken);
        return Ok(result);
    }

    // POST /api/cart
    [HttpPost]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCartAsync(
        [FromBody] CartRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var result = await _cartService.SetCartAsync(userId, guestSessionId, request, cancellationToken);
        return Ok(result);
    }

    // DELETE /api/cart
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCartAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        await _cartService.ClearCartAsync(userId, guestSessionId, cancellationToken);
        return NoContent();
    }

    // POST /api/cart/merge  (JWT required — guests cannot merge carts)
    [HttpPost("merge")]
    [Authorize]  // Overrides class-level DualAuth — JWT only, no guest token
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MergeCartAsync(
        [FromBody] CartMergeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        if (userId == null)
            return Unauthorized();

        var result = await _cartService.MergeCartsAsync(userId.Value, request.GuestSessionId, cancellationToken);
        return Ok(result);
    }
}
