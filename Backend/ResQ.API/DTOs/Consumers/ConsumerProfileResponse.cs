namespace ResQ.API.DTOs.Consumers;

public class ConsumerProfileResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSaved { get; set; }
    public decimal Co2SavedKg { get; set; }
}
