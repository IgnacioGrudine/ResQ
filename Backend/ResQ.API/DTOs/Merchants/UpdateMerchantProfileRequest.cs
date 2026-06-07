namespace ResQ.API.DTOs.Merchants;

/// <summary>
/// Request payload for updating the authenticated merchant's profile.
/// Allows the merchant to change business information and reassign food categories.
/// </summary>
public class UpdateMerchantProfileRequest
{
    /// <summary>
    /// Updated public-facing business name of the merchant.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Updated physical street address of the merchant's establishment.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Updated geographic latitude of the merchant's location.
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Updated geographic longitude of the merchant's location.
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Updated public contact phone number of the merchant.
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Identifiers of the food categories to associate with this merchant.
    /// Replaces the existing category assignments.
    /// </summary>
    public List<int> CategoryIds { get; set; } = [];
}
