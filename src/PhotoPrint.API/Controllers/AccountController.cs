using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.DTOs.Account;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    // ── GET /api/account ──────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccount(CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        var dto = await _accountService.GetAccountAsync(userId.Value, ct);
        return Ok(dto);
    }

    // ── PATCH /api/account ────────────────────────────────────────────────────

    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAccount(
        [FromBody] UpdateAccountRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        await _accountService.UpdateAccountAsync(userId.Value, request, ct);
        return Ok();
    }

    // ── POST /api/account/change-password ─────────────────────────────────────

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        await _accountService.ChangePasswordAsync(userId.Value, request, ct);
        return Ok();
    }

    // ── DELETE /api/account ───────────────────────────────────────────────────

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestDeletion(CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        await _accountService.RequestDeletionAsync(userId.Value, ct);
        return Ok(new { message = "Contul va fi șters în 30 de zile. Conectează-te pentru a anula." });
    }

    // ── GET /api/account/addresses ────────────────────────────────────────────

    [HttpGet("addresses")]
    [ProducesResponseType(typeof(List<SavedAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAddresses(CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        var addresses = await _accountService.GetAddressesAsync(userId.Value, ct);
        return Ok(addresses);
    }

    // ── POST /api/account/addresses ───────────────────────────────────────────

    [HttpPost("addresses")]
    [ProducesResponseType(typeof(SavedAddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddAddress(
        [FromBody] SavedAddressRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        var dto = await _accountService.AddAddressAsync(userId.Value, request, ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    // ── PUT /api/account/addresses/{id} ──────────────────────────────────────

    [HttpPut("addresses/{id:guid}")]
    [ProducesResponseType(typeof(SavedAddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAddress(
        Guid id,
        [FromBody] SavedAddressRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        var dto = await _accountService.UpdateAddressAsync(userId.Value, id, request, ct);
        return Ok(dto);
    }

    // ── DELETE /api/account/addresses/{id} ───────────────────────────────────

    [HttpDelete("addresses/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserIdOrNull();
        if (userId is null)
            return Unauthorized();

        await _accountService.DeleteAddressAsync(userId.Value, id, ct);
        return NoContent();
    }
}
