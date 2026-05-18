using ResQ.API.Models.Common;
using ResQ.API.Models.Orders;

namespace ResQ.API.Models.Auth;

public class ConsumerProfile : BaseEntity
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = [];
}
