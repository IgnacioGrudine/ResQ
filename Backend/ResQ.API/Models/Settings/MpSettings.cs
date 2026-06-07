namespace ResQ.API.Models.Settings;

/// <summary>
/// Strongly-typed configuration for the ResQ platform's Mercado Pago Marketplace application.
/// These are the <em>platform-level</em> credentials — not per-merchant OAuth tokens.
/// Bound from the <c>MpSettings</c> section in <c>appsettings.json</c>
/// (or the corresponding environment variable overrides in production).
/// </summary>
public class MpSettings
{
    /// <summary>
    /// The Mercado Pago application client ID for the ResQ marketplace app.
    /// Used as the <c>client_id</c> parameter in the OAuth authorisation URL
    /// shown to merchants when connecting their MP account.
    /// Sourced from <c>MpSettings:ClientId</c>.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Mercado Pago application client secret used to exchange the OAuth authorisation
    /// code for access and refresh tokens on behalf of a merchant.
    /// Sourced from <c>MpSettings:ClientSecret</c>. Never expose this value in client-side code.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The Mercado Pago public key for the ResQ marketplace application.
    /// Passed to the MP frontend SDK to initialise the payment brick on the consumer checkout page.
    /// Sourced from <c>MpSettings:PublicKey</c>.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// The platform administrator's Mercado Pago access token, used for operations that
    /// require platform-level authority (e.g. querying payment details or applying marketplace fees).
    /// Sourced from <c>MpSettings:AdminAccessToken</c>. Treat as a secret.
    /// </summary>
    public string AdminAccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth callback URL registered in the Mercado Pago developer portal.
    /// After a merchant authorises the connection, MP redirects to this URI with the
    /// authorisation code. Must match exactly the URI configured in the MP app settings.
    /// Sourced from <c>MpSettings:RedirectUri</c>.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// The publicly reachable URL to which Mercado Pago sends webhook (IPN) notifications
    /// for payment events. Registered as the <c>notification_url</c> when creating payment preferences.
    /// Must be accessible from the internet; typically a tunnel URL in local development.
    /// Sourced from <c>MpSettings:NotificationUrl</c>.
    /// </summary>
    public string NotificationUrl { get; set; } = string.Empty;
}
