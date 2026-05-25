using ResQ.API.DTOs.Shared;
using ResQ.API.Models.Enums;

namespace ResQ.API.DTOs.Merchants;

public class MerchantProfileResponse
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public MpConnectionStatus MpConnectionStatus { get; set; }
    public List<CategoryResponse> Categories { get; set; } = [];
}
