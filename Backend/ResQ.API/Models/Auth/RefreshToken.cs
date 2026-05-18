using ResQ.API.Models.Common;

namespace ResQ.API.Models.Auth;

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }

    public User User { get; set; } = null!;
}
