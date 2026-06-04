using System.Text.Json.Serialization;

namespace ResQ.API.DTOs.MercadoPago;

public class MpWebhookPayload
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("data")]
    public MpWebhookData? Data { get; set; }
}

public class MpWebhookData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
