namespace ResQ.API.DTOs.Auth;

/// <summary>
/// Request payload for the consumer registration endpoint. Contains the credentials
/// and personal data required to create a new consumer account and its associated profile.
/// </summary>
public class RegisterConsumerRequest
{
    /// <summary>
    /// Email address that will serve as the consumer's login identifier. Must be unique.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Plaintext password chosen by the consumer. Will be hashed before storage.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Consumer's given name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Consumer's family name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Optional contact phone number for the consumer.
    /// </summary>
    public string? PhoneNumber { get; set; }
}
