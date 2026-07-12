namespace ResQ.API.DTOs.Auth;

/// <summary>
/// Request payload for the forgot-password endpoint.
/// </summary>
/// <param name="Email">The email address of the account requesting a password reset.</param>
public record ForgotPasswordRequest(string Email);
