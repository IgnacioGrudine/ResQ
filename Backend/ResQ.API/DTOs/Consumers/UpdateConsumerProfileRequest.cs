namespace ResQ.API.DTOs.Consumers;

/// <summary>
/// Request payload for updating the authenticated consumer's profile.
/// Only the fields included in this DTO are editable by the consumer.
/// </summary>
public class UpdateConsumerProfileRequest
{
    /// <summary>
    /// Updated given name of the consumer.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Updated family name of the consumer.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Updated contact phone number. Null or omitted means no change.
    /// </summary>
    public string? PhoneNumber { get; set; }
}
