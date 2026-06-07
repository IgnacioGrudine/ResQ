using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IConsumerProfileRepository : IGenericRepository<ConsumerProfile>
{
    /// <summary>
    /// Returns the consumer profile associated with the given user account,
    /// or <c>null</c> if no profile has been created for that user.
    /// </summary>
    /// <param name="userId">The identifier of the <see cref="User"/> whose consumer profile is requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="ConsumerProfile"/>, or <c>null</c> if not found.</returns>
    Task<ConsumerProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the consumer profile by its own identifier, eagerly loading the
    /// associated <see cref="User"/> navigation property.
    /// </summary>
    /// <remarks>
    /// Use this overload when the caller needs both profile and account data in a
    /// single round-trip, for example when building the profile response DTO.
    /// </remarks>
    /// <param name="profileId">The identifier of the consumer profile to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="ConsumerProfile"/> with the <see cref="User"/> navigation property
    /// populated, or <c>null</c> if not found.
    /// </returns>
    Task<ConsumerProfile?> GetByIdWithUserAsync(int profileId, CancellationToken ct = default);
}
