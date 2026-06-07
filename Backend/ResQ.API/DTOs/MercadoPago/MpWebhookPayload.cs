using System.Text.Json.Serialization;

namespace ResQ.API.DTOs.MercadoPago;

/// <summary>
/// Root payload delivered by Mercado Pago to the webhook endpoint when a payment
/// notification is received. Maps the JSON fields sent by MP's notification service.
/// </summary>
public class MpWebhookPayload
{
    /// <summary>
    /// Type of the Mercado Pago notification (e.g., "payment").
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Object containing the resource identifier associated with this notification.
    /// </summary>
    [JsonPropertyName("data")]
    public MpWebhookData? Data { get; set; }
}

/// <summary>
/// Nested data object within a Mercado Pago webhook payload,
/// carrying the identifier of the resource that triggered the notification.
/// </summary>
public class MpWebhookData
{
    /// <summary>
    /// Mercado Pago resource identifier (e.g., payment ID) that triggered this notification.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
