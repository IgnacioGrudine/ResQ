using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ResQ.API.DTOs.Auth;
using ResQ.API.Extensions;
using ResQ.API.Models.Settings;
using ResQ.API.Services.Auth;

namespace ResQ.API.Controllers;

/// <summary>
/// Handles authentication for both consumers and merchants: registration, login,
/// token refresh, and logout. Access tokens are returned in the response body;
/// refresh tokens are managed exclusively via an HttpOnly cookie named
/// <c>resq_refresh_token</c> to prevent JavaScript access.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IGoogleAuthService googleAuthService,
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

    /// <summary>
    /// Starts the password-reset flow for the given email address.
    /// Always returns 204 No Content, whether or not the email is registered,
    /// to avoid leaking account existence.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct)
        => (await authService.ForgotPasswordAsync(request.Email, ct)).ToActionResult();

    /// <summary>
    /// Completes the password-reset flow using the token emailed to the user.
    /// </summary>
    /// <remarks>Returns 400 Bad Request if the token is invalid, expired, or already used.</remarks>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken ct)
        => (await authService.ResetPasswordAsync(request, ct)).ToActionResult();

    /// <summary>
    /// Authenticates (or registers) a user via Google Sign-In.
    /// Verifies the Google id_token server-side, then finds or creates a consumer account.
    /// </summary>
    /// <remarks>Returns 401 if the token is invalid or expired.</remarks>
    [HttpPost("google")]
    public async Task<ActionResult<ClientAuthResponse>> LoginWithGoogle(
        [FromBody] GoogleLoginRequest request,
        CancellationToken ct)
        => (await googleAuthService.LoginWithGoogleAsync(request.IdToken, ct))
            .ToActionResult(ToClientResponse, r => SetRefreshCookie(r.RefreshToken));

    // ─── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends the refresh token as an HttpOnly cookie on the current HTTP response,
    /// with expiry aligned to the configured <see cref="JwtSettings.RefreshTokenExpirationDays"/>.
    /// </summary>
    /// <param name="token">The plaintext refresh token to store in the cookie.</param>
    private void SetRefreshCookie(string token) =>
        Response.Cookies.Append(RefreshCookieName, token, BuildCookieOptions());

    /// <summary>
    /// Overwrites the refresh token cookie with an empty value and an expiry in the past,
    /// effectively deleting it from the browser.
    /// </summary>
    private void ClearRefreshCookie() =>
        Response.Cookies.Append(RefreshCookieName, string.Empty, BuildCookieOptions(clear: true));

    /// <summary>
    /// Builds the <see cref="CookieOptions"/> used for the refresh token cookie.
    /// In non-development environments the <c>Secure</c> flag is set so the cookie is
    /// only sent over HTTPS.
    /// </summary>
    /// <param name="clear">
    /// When <see langword="true"/>, sets the expiry to one day in the past to delete the cookie.
    /// When <see langword="false"/> (default), sets the expiry to the configured refresh token lifetime.
    /// </param>
    /// <returns>A configured <see cref="CookieOptions"/> instance.</returns>
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

    /// <summary>
    /// Maps the internal <see cref="AuthResponse"/> (which includes the raw refresh token)
    /// to the <see cref="ClientAuthResponse"/> DTO that is safe to return to the client.
    /// The refresh token is intentionally excluded from the client-facing DTO.
    /// </summary>
    /// <param name="r">The internal auth response produced by <see cref="IAuthService"/>.</param>
    /// <returns>A <see cref="ClientAuthResponse"/> containing only client-safe fields.</returns>
    private static ClientAuthResponse ToClientResponse(AuthResponse r) => new()
    {
        AccessToken          = r.AccessToken,
        AccessTokenExpiresAt = r.AccessTokenExpiresAt,
        Role                 = r.Role,
        ProfileId            = r.ProfileId
    };
}
