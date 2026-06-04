using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
}
