using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Reviews;

public interface IReviewRepository : IGenericRepository<Review>
{
    /// <summary>
    /// Returns all consumer reviews submitted for products belonging to the given merchant,
    /// ordered from most recent to oldest.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant profile whose reviews are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Review"/> entities for the specified merchant;
    /// an empty collection if the merchant has received no reviews.
    /// </returns>
    Task<IEnumerable<Review>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);
}
