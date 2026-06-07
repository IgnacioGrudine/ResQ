namespace ResQ.API.Models.Settings;

/// <summary>
/// Strongly-typed configuration for JSON Web Token generation and validation.
/// Bound from the <c>JwtSettings</c> section in <c>appsettings.json</c>
/// (or the corresponding environment variable overrides in production).
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// HMAC-SHA256 signing key used to sign and verify access tokens.
    /// Must be at least 32 characters long. Sourced from <c>JwtSettings:SecretKey</c>
    /// in configuration — never hard-code this value in source control.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The <c>iss</c> (issuer) claim embedded in every token, identifying the ResQ API
    /// as the authority that issued it. Sourced from <c>JwtSettings:Issuer</c>.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The <c>aud</c> (audience) claim embedded in every token, identifying the intended
    /// recipient (typically the ResQ frontend client). Sourced from <c>JwtSettings:Audience</c>.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Lifetime of the short-lived access token in minutes.
    /// Defaults to 15 minutes. Sourced from <c>JwtSettings:AccessTokenExpirationMinutes</c>.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Lifetime of the long-lived refresh token in days.
    /// Defaults to 7 days. Sourced from <c>JwtSettings:RefreshTokenExpirationDays</c>.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
