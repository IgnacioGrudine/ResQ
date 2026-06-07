namespace ResQ.API.DTOs.Auth;

/// <summary>
/// Response returned by the authentication service after a successful login or token refresh,
/// containing both tokens and the user's role and profile identifier.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Short-lived JWT access token used to authorize API requests.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Long-lived opaque token used to obtain a new access token without re-authenticating.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

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
