using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.Orders;
using ResQ.API.Repositories.Reviews;

namespace ResQ.API.Data.UnitOfWork;

public class UnitOfWork(ResQDbContext db) : IUnitOfWork
{
    // Auth
    private IUserRepository? _users;
    private IUserRoleRepository? _userRoles;
    private IRefreshTokenRepository? _refreshTokens;
    private IConsumerProfileRepository? _consumerProfiles;
    private IMerchantProfileRepository? _merchantProfiles;

    // Catalog
    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private IMerchantCategoryRepository? _merchantCategories;

    // Orders
    private IOrderRepository? _orders;

    // Reviews
    private IReviewRepository? _reviews;

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

    public IProductRepository Products
        => _products ??= new ProductRepository(db);

    public ICategoryRepository Categories
        => _categories ??= new CategoryRepository(db);

    public IMerchantCategoryRepository MerchantCategories
        => _merchantCategories ??= new MerchantCategoryRepository(db);

    public IOrderRepository Orders
        => _orders ??= new OrderRepository(db);

    public IReviewRepository Reviews
        => _reviews ??= new ReviewRepository(db);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);

    public async ValueTask DisposeAsync()
        => await db.DisposeAsync();
}
