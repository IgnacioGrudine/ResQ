using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Extensions;
using ResQ.API.Services.MercadoPago;

namespace ResQ.API.Controllers;

/// <summary>
/// Manages the Mercado Pago OAuth2 authorization-code flow for merchants.
/// Provides endpoints to initiate the connection, handle the MP callback, and disconnect.
/// </summary>
[ApiController]
[Route("api")]
public class MercadoPagoOAuthController(IMercadoPagoOAuthService oauthService) : ControllerBase
{
    /// <summary>
    /// Returns the Mercado Pago authorization URL for the authenticated merchant.
    /// The merchant must visit this URL in their browser to grant ResQ marketplace
    /// access to their Mercado Pago account.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <remarks>
    /// The merchant's profile ID is embedded in the OAuth <c>state</c> parameter of the
    /// generated URL so that the callback endpoint can identify the merchant without
    /// requiring authentication at that point.
    /// </remarks>
    /// <returns>
    /// 200 OK with a <see cref="MpAuthUrlResponse"/> containing the authorization URL.
    /// </returns>
    [HttpGet("merchants/mp/auth-url")]
    [Authorize(Roles = "Merchant")]
    public IActionResult GetAuthUrl()
    {
        var url = oauthService.BuildAuthorizationUrl(User.GetProfileId());
        return Ok(new MpAuthUrlResponse { AuthUrl = url });
    }

    /// <summary>
    /// OAuth callback endpoint called directly by Mercado Pago after the merchant approves
    /// (or denies) the authorization request. Exchanges the authorization code for access
    /// and refresh tokens, encrypts and persists them, then redirects the merchant back
    /// to the frontend with a status indicator.
    /// </summary>
    /// <remarks>
    /// This endpoint is intentionally <see cref="AllowAnonymousAttribute"/> because it is
    /// called by the Mercado Pago authorization server — not by the merchant's browser with
    /// a JWT. The merchant identity is recovered from the <paramref name="state"/> parameter.
    /// On any error (MP error, missing params, invalid state, token exchange failure) the
    /// redirect target includes a <c>status=error</c> query parameter so the frontend can
    /// display an appropriate message.
    /// </remarks>
    /// <param name="code">The authorization code issued by Mercado Pago. Null if the merchant denied access.</param>
    /// <param name="state">The merchant profile ID encoded as a string in the original auth URL.</param>
    /// <param name="error">Error code sent by Mercado Pago if the merchant denied authorization.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A redirect to <c>/mp/callback?status=success</c> on success, or
    /// <c>/mp/callback?status=error&amp;message=...</c> on any failure.
    /// </returns>
    [HttpGet("auth/mp/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (error is not null)
            return Redirect($"/mp/callback?status=error&message={Uri.EscapeDataString(error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect("/mp/callback?status=error&message=missing_params");

        if (!int.TryParse(state, out var merchantProfileId))
            return Redirect("/mp/callback?status=error&message=invalid_state");

        var result = await oauthService.HandleCallbackAsync(code, merchantProfileId, ct);

        return result.IsSuccess
            ? Redirect("/mp/callback?status=success")
            : Redirect($"/mp/callback?status=error&message={Uri.EscapeDataString(result.Errors[0].Message)}");
    }

    /// <summary>
    /// Disconnects the authenticated merchant from Mercado Pago.
    /// Marks the stored credentials as inactive, sets the merchant's connection status
    /// to <c>Disconnected</c>, and deactivates all of their active packs.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <remarks>
    /// Packs are deactivated automatically because they cannot accept payments without
    /// an active MP connection. The merchant must reconnect and re-enable their packs manually.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 204 No Content on success; 404 Not Found if the merchant profile does not exist.
    /// </returns>
    [HttpDelete("merchants/mp/disconnect")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
        => (await oauthService.DisconnectAsync(User.GetProfileId(), ct)).ToActionResult();
}
