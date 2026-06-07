namespace ResQ.API.DTOs.Consumers;

/// <summary>
/// Response returned by the consumer profile endpoint, containing personal information
/// and aggregated impact statistics for the authenticated consumer.
/// </summary>
public class ConsumerProfileResponse
{
    /// <summary>
    /// Unique identifier of the consumer profile.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Consumer's given name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Consumer's family name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address associated with the consumer's account.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional contact phone number registered by the consumer.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Total number of orders placed by the consumer across all time.
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Total amount of money saved by the consumer through ResQ discounts, in Argentine pesos.
    /// </summary>
    public decimal TotalSaved { get; set; }

    /// <summary>
    /// Estimated kilograms of CO₂ emissions avoided by the consumer through food rescue.
    /// </summary>
    public decimal Co2SavedKg { get; set; }
}
