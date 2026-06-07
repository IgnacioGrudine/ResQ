using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;

namespace ResQ.API.Models.Auth;

/// <summary>
/// Join entity that associates a <see cref="User"/> with a <see cref="Role"/>.
/// A user may hold multiple roles simultaneously (e.g. both Consumer and Merchant).
/// </summary>
public class UserRole : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="User"/> that holds this role assignment.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The role granted to the user, controlling which features and endpoints they can access.
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Navigation property to the <see cref="User"/> that owns this role assignment.
    /// </summary>
    public User User { get; set; } = null!;
}
