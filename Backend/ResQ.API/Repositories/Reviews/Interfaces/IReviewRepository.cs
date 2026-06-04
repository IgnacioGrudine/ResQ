using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Reviews;

public interface IReviewRepository : IGenericRepository<Review>
{
    Task<IEnumerable<Review>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);
}
