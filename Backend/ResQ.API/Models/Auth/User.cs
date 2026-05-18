using ResQ.API.Models.Common;

namespace ResQ.API.Models.Auth;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ConsumerProfile? ConsumerProfile { get; set; }
    public MerchantProfile? MerchantProfile { get; set; }
}
