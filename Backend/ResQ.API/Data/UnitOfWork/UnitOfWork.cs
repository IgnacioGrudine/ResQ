using ResQ.API.Repositories.Auth;

namespace ResQ.API.Data.UnitOfWork;

public class UnitOfWork(ResQDbContext db) : IUnitOfWork
{
    private IUserRepository? _users;
    private IUserRoleRepository? _userRoles;
    private IRefreshTokenRepository? _refreshTokens;
    private IConsumerProfileRepository? _consumerProfiles;
    private IMerchantProfileRepository? _merchantProfiles;

    public IUserRepository Users
        => _users ??= new UserRepository(db);

    public IUserRoleRepository UserRoles
        => _userRoles ??= new UserRoleRepository(db);

    public IRefreshTokenRepository RefreshTokens
        => _refreshTokens ??= new RefreshTokenRepository(db);

    public IConsumerProfileRepository ConsumerProfiles
        => _consumerProfiles ??= new ConsumerProfileRepository(db);

    public IMerchantProfileRepository MerchantProfiles
        => _merchantProfiles ??= new MerchantProfileRepository(db);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);

    public async ValueTask DisposeAsync()
        => await db.DisposeAsync();
}
