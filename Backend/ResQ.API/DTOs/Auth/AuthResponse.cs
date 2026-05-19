namespace ResQ.API.DTOs.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? ProfileId { get; set; }
}
