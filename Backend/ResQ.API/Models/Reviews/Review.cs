using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;
using ResQ.API.Models.Orders;

namespace ResQ.API.Models.Reviews;

/// <summary>
/// Represents a consumer's post-pickup review of a merchant.
/// Reviews are tied to a specific completed <see cref="Order"/> to ensure that only
/// consumers who successfully picked up a Surprise Pack can leave feedback,
/// and to prevent duplicate reviews per transaction.
/// </summary>
public class Review : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="Order"/> for which this review was submitted.
    /// Carries a UNIQUE constraint to prevent more than one review per order.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Foreign key referencing the <see cref="MerchantProfile"/> being reviewed.
    /// Denormalised here for efficient aggregation of a merchant's average rating
    /// without requiring a join through <see cref="Order"/>.
    /// </summary>
    public int MerchantId { get; set; }

    /// <summary>
    /// Numeric rating given by the consumer, on a scale defined by the business rules
    /// (e.g. 1 to 5). Stored as a single byte for compactness.
    /// </summary>
    public byte Rating { get; set; }

    /// <summary>
    /// Optional free-text comment left by the consumer to describe their experience.
    /// Null if the consumer submitted only a star rating without written feedback.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Navigation property to the order associated with this review.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property to the merchant who was reviewed.
    /// </summary>
    public MerchantProfile Merchant { get; set; } = null!;
}
