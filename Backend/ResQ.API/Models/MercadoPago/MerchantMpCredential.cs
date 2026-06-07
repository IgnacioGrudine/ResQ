using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;

namespace ResQ.API.Models.MercadoPago;

/// <summary>
/// Stores the Mercado Pago OAuth 2.0 credentials obtained after a merchant completes
/// the platform's OAuth authorisation flow. Both <see cref="AccessToken"/> and
/// <see cref="RefreshToken"/> are stored AES-256-CBC encrypted at rest; the encryption
/// key is held in Azure Key Vault and never persisted to the database.
/// One-to-one with <see cref="MerchantProfile"/>.
/// </summary>
public class MerchantMpCredential : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="MerchantProfile"/> that owns these credentials.
    /// </summary>
    public int MerchantId { get; set; }

    /// <summary>
    /// The merchant's Mercado Pago user identifier, returned by the OAuth token exchange.
    /// Used to identify the merchant's MP account when making API calls on their behalf.
    /// </summary>
    public long MpUserId { get; set; }

    /// <summary>
    /// AES-256-CBC encrypted Mercado Pago access token used to authenticate
    /// API requests (e.g. creating payment preferences) on behalf of the merchant.
    /// Decrypted in memory only at the point of use; never logged or exposed in responses.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// AES-256-CBC encrypted Mercado Pago refresh token used by the Hangfire daily job
    /// to obtain a new access token before the current one expires.
    /// Rotated on each successful refresh.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp at which the current <see cref="AccessToken"/> expires.
    /// The Hangfire renewal job uses this to determine which tokens need refreshing.
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// Space-separated list of OAuth scopes granted by the merchant during authorisation
    /// (e.g. <c>read write offline_access</c>). Null if the scope was not returned by the token endpoint.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Indicates whether these credentials are current and should be used for payment processing.
    /// Set to <c>false</c> when a merchant disconnects their MP account or revokes access.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the merchant that owns these Mercado Pago credentials.
    /// </summary>
    public MerchantProfile Merchant { get; set; } = null!;
}
