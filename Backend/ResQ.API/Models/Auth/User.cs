using ResQ.API.Models.Common;

namespace ResQ.API.Models.Auth;

/// <summary>
/// Core identity record for every account on the platform.
/// Holds credentials and links to role assignments and profile data.
/// A user may have a <see cref="ConsumerProfile"/>, a <see cref="MerchantProfile"/>, or both,
/// depending on their assigned roles.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Unique email address used as the login identifier for this account.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Bcrypt hash of the user's password. Null for accounts created via Google OAuth.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Google account subject identifier. Null for email/password accounts.
    /// </summary>
    public string? GoogleId { get; set; }

    /// <summary>
    /// Indicates whether the account is active and permitted to authenticate.
    /// Soft-disable a user by setting this to <c>false</c> without deleting the record.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The roles assigned to this user (e.g. Consumer, Merchant, Admin).
    /// Determines which parts of the platform the user can access.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// All refresh tokens that have been issued to this user, including revoked and expired ones.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    /// <summary>
    /// The consumer profile associated with this user. Null if the user has not registered as a consumer.
    /// </summary>
    public ConsumerProfile? ConsumerProfile { get; set; }

    /// <summary>
    /// The merchant profile associated with this user. Null if the user has not registered as a merchant.
    /// </summary>
    public MerchantProfile? MerchantProfile { get; set; }
}
