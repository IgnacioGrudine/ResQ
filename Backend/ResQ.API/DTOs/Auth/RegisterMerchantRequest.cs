namespace ResQ.API.DTOs.Auth;

/// <summary>
/// Request payload for the merchant registration endpoint. Contains the credentials
/// and business data required to create a new merchant account and its associated profile.
/// </summary>
public class RegisterMerchantRequest
{
    /// <summary>
    /// Email address that will serve as the merchant's login identifier. Must be unique.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Plaintext password chosen by the merchant. Will be hashed before storage.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Public-facing name of the merchant's business.
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
    /// Geographic latitude of the merchant's location, used for proximity searches.
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Geographic longitude of the merchant's location, used for proximity searches.
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Public contact phone number for the merchant's establishment.
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;
}
