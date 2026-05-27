using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoPrint.API.DTOs.Auth;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISocialAuthService _socialAuthService;
    private readonly IGuestSessionService _guestSessionService;

    public AuthController(
        IAuthService authService,
        ISocialAuthService socialAuthService,
        IGuestSessionService guestSessionService)
    {
        _authService = authService;
        _socialAuthService = socialAuthService;
        _guestSessionService = guestSessionService;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [EnableRateLimiting(AuthExtensions.RegisterRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [EnableRateLimiting(SecurityExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var response = await _authService.LoginAsync(request, ip, Response, cancellationToken);
        return Ok(response);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(rawToken))
        {
            return Unauthorized(new { message = "Token de reîmprospătare lipsă." });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var response = await _authService.RefreshTokenAsync(rawToken, ip, Response, cancellationToken);
        return Ok(response);
    }

    // POST /api/auth/logout — no [Authorize]: expired tokens can still invalidate the cookie
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies["refresh_token"];

        if (!string.IsNullOrEmpty(rawToken))
        {
            await _authService.RevokeRefreshTokenAsync(rawToken, cancellationToken);
        }

        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/api/auth",
        });

        return NoContent();
    }

    // GET /api/auth/confirm-email
    [HttpGet("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmEmailAsync(
        [FromQuery] Guid userId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        await _authService.ConfirmEmailAsync(userId, token, cancellationToken);
        return NoContent();
    }

    // POST /api/auth/resend-confirmation
    [HttpPost("resend-confirmation")]
    [EnableRateLimiting(AuthExtensions.ResendConfirmationRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResendConfirmationAsync(
        [FromBody] ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResendConfirmationAsync(request.Email, cancellationToken);
        return NoContent();
    }

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    [EnableRateLimiting(AuthExtensions.ForgotPasswordRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return NoContent();
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, cancellationToken);
        return NoContent();
    }

    // POST /api/auth/google
    [HttpPost("google")]
    [EnableRateLimiting(SecurityExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType(typeof(GoogleLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GoogleSignInAsync(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var response = await _socialAuthService.GoogleSignInAsync(request.IdToken, ip, Response, cancellationToken);
        return Ok(response);
    }

    // POST /api/auth/guest
    [HttpPost("guest")]
    [ProducesResponseType(typeof(CreateGuestSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateGuestSessionAsync(
        [FromBody] CreateGuestSessionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _guestSessionService.CreateAsync(request, cancellationToken);
        return Ok(response);
    }

    // POST /api/auth/guest/init — anonymous pre-session; no contact info required.
    // Called automatically by the frontend when an unauthenticated user starts uploading.
    [HttpPost("guest/init")]
    [ProducesResponseType(typeof(CreateGuestSessionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitGuestSessionAsync(CancellationToken cancellationToken)
    {
        var response = await _guestSessionService.InitAsync(cancellationToken);
        return Ok(response);
    }

    // PATCH /api/auth/guest/contact — fills in contact info on an existing guest session.
    // Called from the checkout form when the user already has an anonymous pre-session.
    [HttpPatch("guest/contact")]
    [Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateGuestContactAsync(
        [FromBody] UpdateGuestContactRequest request,
        CancellationToken cancellationToken)
    {
        var sessionId = User.GetGuestSessionIdOrNull()
            ?? throw new UnauthorizedAccessException("Not a guest session.");
        await _guestSessionService.UpdateContactAsync(sessionId, request, cancellationToken);
        return NoContent();
    }

    // POST /api/auth/guest/claim
    [HttpPost("guest/claim")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ClaimGuestSessionAsync(
        [FromBody] ClaimGuestSessionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _guestSessionService.ClaimAsync(request.GuestToken, userId, cancellationToken);
        return Ok();
    }
}
