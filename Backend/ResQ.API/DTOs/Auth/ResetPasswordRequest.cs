namespace ResQ.API.DTOs.Auth;

/// <summary>
/// Request payload for the reset-password endpoint.
/// </summary>
/// <param name="Token">The opaque token from the reset link emailed to the user.</param>
/// <param name="NewPassword">The new plaintext password chosen by the user. Will be hashed before storage.</param>
public record ResetPasswordRequest(string Token, string NewPassword);
