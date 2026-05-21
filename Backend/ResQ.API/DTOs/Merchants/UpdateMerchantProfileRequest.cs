namespace ResQ.API.DTOs.Merchants;

public class UpdateMerchantProfileRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public List<int> CategoryIds { get; set; } = [];
}
