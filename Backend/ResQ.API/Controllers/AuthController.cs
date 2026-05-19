using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Auth;
using ResQ.API.Extensions;
using ResQ.API.Services.Auth;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Registers a new consumer account with personal details.
    /// Creates the user, assigns the Consumer role, and returns a ready-to-use token pair.
    /// </summary>
    /// <remarks>Returns 409 Conflict if the email address is already registered.</remarks>
    [HttpPost("register/consumer")]
    public async Task<ActionResult<AuthResponse>> RegisterConsumer(
        [FromBody] RegisterConsumerRequest request,
        CancellationToken ct)
        => (await authService.RegisterConsumerAsync(request, ct)).ToActionResult();

    /// <summary>
    /// Registers a new merchant account with business information.
    /// Creates the user, assigns the Merchant role, and returns a ready-to-use token pair.
    /// </summary>
    /// <remarks>Returns 409 Conflict if the email address is already registered.</remarks>
    [HttpPost("register/merchant")]
    public async Task<ActionResult<AuthResponse>> RegisterMerchant(
        [FromBody] RegisterMerchantRequest request,
        CancellationToken ct)
        => (await authService.RegisterMerchantAsync(request, ct)).ToActionResult();

    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// Returns a short-lived access token (15 min) and a long-lived refresh token (7 days).
    /// </summary>
    /// <remarks>Returns 401 Unauthorized if the credentials are invalid or the account is inactive.</remarks>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
        => (await authService.LoginAsync(request, ct)).ToActionResult();

    /// <summary>
    /// Issues a new access token using a valid, non-expired refresh token.
    /// Rotates the refresh token on each call — the previous token is immediately revoked.
    /// </summary>
    /// <remarks>Returns 401 Unauthorized if the refresh token is invalid, expired, or already revoked.</remarks>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
        => (await authService.RefreshTokenAsync(request.RefreshToken, ct)).ToActionResult();

    /// <summary>
    /// Revokes the provided refresh token, effectively ending the current session.
    /// This operation is idempotent — revoking an already-revoked or non-existent token still returns 204.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
        => (await authService.LogoutAsync(request.RefreshToken, ct)).ToActionResult();
}
