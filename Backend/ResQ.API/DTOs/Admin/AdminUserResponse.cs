namespace ResQ.API.DTOs.Admin;

/// <summary>
/// Row in the admin user-management list, summarising one user account's identity,
/// role, status, and registration date. Covers both consumers and merchants.
/// </summary>
public class AdminUserListItemResponse
{
    /// <summary>User account ID.</summary>
    public int Id { get; set; }

    /// <summary>Login email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Primary role: "Consumer", "Merchant", or "Admin".</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Display name: full name for consumers, business name for merchants,
    /// or the email local part for admins.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Whether the account is active (can authenticate).</summary>
    public bool IsActive { get; set; }

    /// <summary>The profile ID (consumer or merchant) associated with the account, if any.</summary>
    public int? ProfileId { get; set; }

    /// <summary>When the account was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request body for activating or deactivating a user or merchant account.
/// Toggles the underlying <c>User.IsActive</c> flag (soft enable/disable, no deletion).
/// </summary>
public class UpdateStatusRequest
{
    /// <summary>The desired active state: <c>true</c> to enable, <c>false</c> to disable.</summary>
    public bool IsActive { get; set; }
}
