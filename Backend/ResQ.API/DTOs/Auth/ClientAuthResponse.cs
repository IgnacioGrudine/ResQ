namespace ResQ.API.DTOs.Auth;

/// <summary>
/// Slimmed-down authentication response sent to the client after a successful login.
/// Omits the refresh token (which is stored in an HTTP-only cookie) and exposes only
/// the data the frontend needs to bootstrap the session.
/// </summary>
public class ClientAuthResponse
{
    /// <summary>
    /// Short-lived JWT access token used to authorize API requests.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// UTC date and time at which the access token expires.
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// Role assigned to the authenticated user (e.g., Consumer, Merchant, Admin).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the user's domain profile (ConsumerProfile or MerchantProfile).
    /// Null if the user has no associated profile yet.
    /// </summary>
    public int? ProfileId { get; set; }
}
