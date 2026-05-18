using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;

namespace ResQ.API.Models.MercadoPago;

public class MpWebhookEvent : BaseEntity
{
    public long MpNotificationId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public long MpResourceId { get; set; }
    public string? RawPayload { get; set; }
    public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Pending;
    public DateTime? ProcessedAt { get; set; }
    public byte AttemptCount { get; set; } = 0;
    public string? LastErrorMessage { get; set; }
}
