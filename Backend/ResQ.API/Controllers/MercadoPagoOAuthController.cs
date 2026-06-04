using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Extensions;
using ResQ.API.Services.MercadoPago;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api")]
public class MercadoPagoOAuthController(IMercadoPagoOAuthService oauthService) : ControllerBase
{
    /// <summary>
    /// Returns the Mercado Pago authorization URL for the authenticated merchant.
    /// The merchant must visit this URL to grant ResQ access to their MP account.
    /// </summary>
    [HttpGet("merchants/mp/auth-url")]
    [Authorize(Roles = "Merchant")]
    public IActionResult GetAuthUrl()
    {
        var url = oauthService.BuildAuthorizationUrl(User.GetProfileId());
        return Ok(new MpAuthUrlResponse { AuthUrl = url });
    }

    /// <summary>
    /// OAuth callback from Mercado Pago. Exchanges the authorization code for tokens,
    /// encrypts and persists them, then redirects the merchant back to the frontend.
    /// This endpoint is public — MP calls it directly after the merchant authorizes.
    /// </summary>
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
    /// Marks credentials as inactive and disables all their active products.
    /// </summary>
    [HttpDelete("merchants/mp/disconnect")]
    [Authorize(Roles = "Merchant")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
        => (await oauthService.DisconnectAsync(User.GetProfileId(), ct)).ToActionResult();
}
