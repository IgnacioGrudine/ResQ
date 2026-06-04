namespace ResQ.API.Models.Settings;

public class MpSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string AdminAccessToken { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string NotificationUrl { get; set; } = string.Empty;
}
