using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;

namespace ResQ.API.Models.Auth;

public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public Role Role { get; set; }

    public User User { get; set; } = null!;
}
