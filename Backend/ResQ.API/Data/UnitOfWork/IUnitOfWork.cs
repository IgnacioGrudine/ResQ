using ResQ.API.Repositories.Auth;

namespace ResQ.API.Data.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IUserRoleRepository UserRoles { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IConsumerProfileRepository ConsumerProfiles { get; }
    IMerchantProfileRepository MerchantProfiles { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
