using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Notifications;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reviews;

namespace ResQ.API.Data;

public class ResQDbContext : DbContext
{
    public ResQDbContext(DbContextOptions<ResQDbContext> options) : base(options) { }

    // Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ConsumerProfile> ConsumerProfiles => Set<ConsumerProfile>();
    public DbSet<MerchantProfile> MerchantProfiles => Set<MerchantProfile>();

    // Catalog
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MerchantCategory> MerchantCategories => Set<MerchantCategory>();
    public DbSet<Product> Products => Set<Product>();

    // Orders
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    // Reviews
    public DbSet<Review> Reviews => Set<Review>();

    // Notifications
    public DbSet<MerchantNotification> MerchantNotifications => Set<MerchantNotification>();

    // Mercado Pago OAuth
    public DbSet<MerchantMpCredential> MerchantMpCredentials => Set<MerchantMpCredential>();
    public DbSet<MpTokenRefreshLog> MpTokenRefreshLogs => Set<MpTokenRefreshLog>();
    public DbSet<MpWebhookEvent> MpWebhookEvents => Set<MpWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
