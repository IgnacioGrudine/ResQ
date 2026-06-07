namespace ResQ.API.DTOs.Reviews;

/// <summary>
/// Response representing a single consumer review submitted for a merchant,
/// returned in merchant detail and profile endpoints.
/// </summary>
public class ReviewResponse
{
    /// <summary>
    /// Unique identifier of the review.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Star rating given by the consumer, typically in the range 1–5.
    /// </summary>
    public byte Rating { get; set; }

    /// <summary>
    /// Optional written comment accompanying the numeric rating.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// UTC date and time when the review was submitted.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
