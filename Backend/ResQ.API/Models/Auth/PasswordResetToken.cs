using ResQ.API.Models.Common;

namespace ResQ.API.Models.Auth;

/// <summary>
/// Represents a single-use token issued to a user who requested a password reset.
/// The plaintext token is emailed to the user as part of a reset link; only its
/// SHA-256 hash is persisted, so a leaked database dump cannot be used to reset accounts.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="User"/> requesting the password reset.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// SHA-256 hash (hex-encoded) of the opaque token string emailed to the user.
    /// The raw token itself is never persisted.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp after which this token is no longer valid. Set to one hour
    /// after issuance.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether this token has already been consumed by a successful
    /// password reset. Used tokens must be rejected even if not yet expired.
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Navigation property to the <see cref="User"/> that requested the reset.
    /// </summary>
    public User User { get; set; } = null!;
}
