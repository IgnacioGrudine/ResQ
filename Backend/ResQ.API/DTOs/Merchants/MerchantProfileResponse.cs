using ResQ.API.DTOs.Shared;
using ResQ.API.Models.Enums;

namespace ResQ.API.DTOs.Merchants;

/// <summary>
/// Response returned by the authenticated merchant's profile endpoint,
/// providing full account details including business information,
/// Mercado Pago connection status, and associated categories.
/// </summary>
public class MerchantProfileResponse
{
    /// <summary>
    /// Unique identifier of the merchant profile.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Public-facing business name of the merchant.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Argentine tax identification number (CUIT) in the format XX-XXXXXXXX-X.
    /// </summary>
    public string Cuit { get; set; } = string.Empty;

    /// <summary>
    /// Physical street address of the merchant's establishment.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Geographic latitude of the merchant's location.
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Geographic longitude of the merchant's location.
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Public contact phone number of the merchant.
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL of the merchant's profile photo.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Current status of the merchant's Mercado Pago OAuth connection.
    /// </summary>
    public MpConnectionStatus MpConnectionStatus { get; set; }

    /// <summary>
    /// List of food categories associated with this merchant.
    /// </summary>
    public List<CategoryResponse> Categories { get; set; } = [];
}
