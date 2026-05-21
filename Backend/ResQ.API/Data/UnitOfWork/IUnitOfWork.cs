using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.Orders;
using ResQ.API.Repositories.Reviews;

namespace ResQ.API.Data.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    // Auth
    IUserRepository Users { get; }
    IUserRoleRepository UserRoles { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IConsumerProfileRepository ConsumerProfiles { get; }
    IMerchantProfileRepository MerchantProfiles { get; }

    // Catalog
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IMerchantCategoryRepository MerchantCategories { get; }

    // Orders
    IOrderRepository Orders { get; }

    // Reviews
    IReviewRepository Reviews { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
