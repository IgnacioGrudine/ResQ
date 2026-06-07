namespace ResQ.API.DTOs.Auth;

/// <summary>
/// Request payload for the login endpoint. Contains the user's credentials
/// for email/password authentication.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
/// <param name="Password">The user's plaintext password (transmitted over HTTPS and verified against the stored hash).</param>
public record LoginRequest(string Email, string Password);
