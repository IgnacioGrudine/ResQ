namespace ResQ.API.DTOs.Auth;

/// <summary>
/// Request payload for the token-refresh endpoint. Carries the opaque refresh token
/// previously issued during login so that a new access token can be generated.
/// </summary>
/// <param name="RefreshToken">The valid, non-expired refresh token to exchange for a new access token.</param>
public record RefreshTokenRequest(string RefreshToken);
