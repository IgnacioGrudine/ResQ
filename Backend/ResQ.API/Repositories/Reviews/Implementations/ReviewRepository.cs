using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Reviews;

/// <summary>
/// EF Core implementation of <see cref="IReviewRepository"/>.
/// Provides data access operations for the <see cref="Review"/> entity,
/// extending generic CRUD with a merchant-scoped query used to populate
/// reputation summaries and review lists.
/// </summary>
public class ReviewRepository(ResQDbContext db) : GenericRepository<Review>(db), IReviewRepository
{
    /// <summary>
    /// Retrieves all reviews submitted for the specified merchant, ordered with the
    /// most recently created review first. The query runs as no-tracking.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose reviews are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Review"/> records for the merchant,
    /// sorted by creation date descending. Returns an empty collection if the
    /// merchant has no reviews.
    /// </returns>
    public async Task<IEnumerable<Review>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Where(r => r.MerchantId == merchantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Returns <c>true</c> if any review record references the given order ID.
    /// Mirrors the UNIQUE constraint on <c>OrderId</c> at the application layer to
    /// produce a clear domain error before hitting the database constraint.
    /// </summary>
    public Task<bool> ExistsForOrderAsync(int orderId, CancellationToken ct = default)
        => _set.AnyAsync(r => r.OrderId == orderId, ct);
}
