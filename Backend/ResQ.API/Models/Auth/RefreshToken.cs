using ResQ.API.Models.Common;

namespace ResQ.API.Models.Auth;

/// <summary>
/// Represents a long-lived refresh token issued to a user upon login.
/// Used to obtain new access tokens without requiring re-authentication.
/// Implements a rotation scheme: each use invalidates the current token and issues a replacement.
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="User"/> that this token belongs to.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The opaque token string sent by the client in refresh requests.
    /// Stored as a cryptographically random value; never re-used after rotation.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp after which this token is no longer valid.
    /// Controlled by the <c>RefreshTokenExpirationDays</c> setting in <c>JwtSettings</c>.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether this token has been explicitly revoked (e.g. via logout or token rotation).
    /// Revoked tokens must be rejected even if they have not yet expired.
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// The token string that replaced this one during rotation. Populated when a new token
    /// is issued in exchange for this one, enabling detection of token reuse attacks.
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Navigation property to the <see cref="User"/> that owns this refresh token.
    /// </summary>
    public User User { get; set; } = null!;
}
