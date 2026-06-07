using ResQ.API.Models.Common;
using ResQ.API.Models.Orders;

namespace ResQ.API.Models.Auth;

/// <summary>
/// Stores the personal information of a consumer — the end-user role that browses
/// merchants, places orders, and picks up Surprise Packs. One-to-one with <see cref="User"/>.
/// </summary>
public class ConsumerProfile : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="User"/> account that owns this profile.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Consumer's given name, used for personalisation and display purposes.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Consumer's family name, used together with <see cref="FirstName"/> for full identification.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Optional contact phone number for the consumer. May be used for order-related notifications.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Navigation property to the <see cref="User"/> account associated with this consumer profile.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// All orders placed by this consumer across all merchants on the platform.
    /// </summary>
    public ICollection<Order> Orders { get; set; } = [];
}
