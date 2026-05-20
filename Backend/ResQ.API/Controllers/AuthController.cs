using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ResQ.API.DTOs.Auth;
using ResQ.API.Extensions;
using ResQ.API.Models.Settings;
using ResQ.API.Services.Auth;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IOptions<JwtSettings> jwtOptions,
    IWebHostEnvironment env) : ControllerBase
{
    private const string RefreshCookieName = "resq_refresh_token";
    private readonly JwtSettings _jwt = jwtOptions.Value;

    /// <summary>
    /// Registers a new consumer account.
    /// Returns 201 Created with an access token. The refresh token is set as an HttpOnly cookie.
    /// </summary>
    /// <remarks>Returns 409 Conflict if the email is already registered.</remarks>
    [HttpPost("register/consumer")]
    public async Task<ActionResult<ClientAuthResponse>> RegisterConsumer(
        [FromBody] RegisterConsumerRequest request,
        CancellationToken ct)
        => (await authService.RegisterConsumerAsync(request, ct))
            .ToCreatedResult(ToClientResponse, r => SetRefreshCookie(r.RefreshToken));

    /// <summary>
    /// Registers a new merchant account.
    /// Returns 201 Created with an access token. The refresh token is set as an HttpOnly cookie.
    /// </summary>
    /// <remarks>Returns 409 Conflict if the email is already registered.</remarks>
    [HttpPost("register/merchant")]
    public async Task<ActionResult<ClientAuthResponse>> RegisterMerchant(
        [FromBody] RegisterMerchantRequest request,
        CancellationToken ct)
        => (await authService.RegisterMerchantAsync(request, ct))
            .ToCreatedResult(ToClientResponse, r => SetRefreshCookie(r.RefreshToken));

    /// <summary>
    /// Authenticates a user with email and password.
    /// Returns 200 OK with an access token. The refresh token is set as an HttpOnly cookie.
    /// </summary>
    /// <remarks>Returns 401 Unauthorized if credentials are invalid or the account is inactive.</remarks>
    [HttpPost("login")]
    public async Task<ActionResult<ClientAuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
        => (await authService.LoginAsync(request, ct))
            .ToActionResult(ToClientResponse, r => SetRefreshCookie(r.RefreshToken));

    /// <summary>
    /// Issues a new access token using the HttpOnly refresh token cookie.
    /// Rotates the refresh token on each call.
    /// </summary>
    /// <remarks>Returns 401 Unauthorized if the cookie is missing, expired, or already revoked.</remarks>
    [HttpPost("refresh")]
    public async Task<ActionResult<ClientAuthResponse>> Refresh(CancellationToken ct)
        => (await authService.RefreshTokenAsync(
                Request.Cookies[RefreshCookieName] ?? string.Empty, ct))
            .ToActionResult(ToClientResponse, r => SetRefreshCookie(r.RefreshToken));

    /// <summary>
    /// Revokes the refresh token cookie, ending the current session.
    /// Idempotent — calling without a cookie still returns 204.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
        => (await authService.LogoutAsync(
                Request.Cookies[RefreshCookieName] ?? string.Empty, ct))
            .ToActionResult(ClearRefreshCookie);

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private void SetRefreshCookie(string token) =>
        Response.Cookies.Append(RefreshCookieName, token, BuildCookieOptions());

    private void ClearRefreshCookie() =>
        Response.Cookies.Append(RefreshCookieName, string.Empty, BuildCookieOptions(clear: true));

    private CookieOptions BuildCookieOptions(bool clear = false) => new()
    {
        HttpOnly = true,
        Secure   = !env.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Expires  = clear
            ? DateTimeOffset.UtcNow.AddDays(-1)
            : DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
        Path     = "/api/auth"
    };

    private static ClientAuthResponse ToClientResponse(AuthResponse r) => new()
    {
        AccessToken          = r.AccessToken,
        AccessTokenExpiresAt = r.AccessTokenExpiresAt,
        Role                 = r.Role,
        ProfileId            = r.ProfileId
    };
}
