namespace ResQ.API.DTOs.Orders;

/// <summary>
/// Request payload for the pickup confirmation endpoint. The merchant submits the
/// alphanumeric code shown by the consumer to validate that the pack has been collected.
/// </summary>
public class ConfirmPickupRequest
{
    /// <summary>
    /// The alphanumeric pickup code presented by the consumer at the point of collection.
    /// </summary>
    public string PickupCode { get; set; } = string.Empty;
}
