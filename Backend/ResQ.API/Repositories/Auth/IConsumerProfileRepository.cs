using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IConsumerProfileRepository : IGenericRepository<ConsumerProfile>
{
    Task<ConsumerProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default);
}
