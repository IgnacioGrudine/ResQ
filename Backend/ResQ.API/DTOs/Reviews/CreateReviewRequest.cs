namespace ResQ.API.DTOs.Reviews;

/// <summary>
/// Request payload sent by a consumer to leave a review for a completed order.
/// </summary>
public class CreateReviewRequest
{
    /// <summary>
    /// Star rating given to the merchant, on a scale from 1 (worst) to 5 (best).
    /// </summary>
    public byte Rating { get; set; }

    /// <summary>
    /// Optional written comment describing the consumer's experience.
    /// Maximum 1 000 characters. Null if only a numeric rating is submitted.
    /// </summary>
    public string? Comment { get; set; }
}
