using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

/// <summary>
/// EF Core implementation of <see cref="IConsumerProfileRepository"/>.
/// Provides data access operations for the <see cref="ConsumerProfile"/> entity,
/// extending the generic CRUD capabilities with consumer-specific queries.
/// </summary>
public class ConsumerProfileRepository(ResQDbContext db) : GenericRepository<ConsumerProfile>(db), IConsumerProfileRepository
{
    /// <summary>
    /// Retrieves the consumer profile associated with the given user account identifier.
    /// </summary>
    /// <param name="userId">The identifier of the <c>User</c> whose profile is requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="ConsumerProfile"/>, or <c>null</c> if no profile exists
    /// for the specified user.
    /// </returns>
    public async Task<ConsumerProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(cp => cp.UserId == userId, ct);

    /// <summary>
    /// Retrieves a consumer profile by its own identifier, eagerly loading the associated
    /// <c>User</c> navigation property. The query runs as no-tracking for read-only scenarios.
    /// </summary>
    /// <param name="profileId">The identifier of the consumer profile.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="ConsumerProfile"/> with its <c>User</c> loaded, or <c>null</c>
    /// if no profile with the given identifier exists.
    /// </returns>
    public async Task<ConsumerProfile?> GetByIdWithUserAsync(int profileId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.Id == profileId, ct);
}
