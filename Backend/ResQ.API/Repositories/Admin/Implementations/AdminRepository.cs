using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Orders;

namespace ResQ.API.Repositories.Admin;

/// <summary>
/// EF Core implementation of <see cref="IAdminRepository"/>. Queries the <see cref="ResQDbContext"/>
/// directly to produce the cross-entity reads required by the administration module.
/// All read queries run no-tracking.
/// </summary>
public class AdminRepository(ResQDbContext db) : IAdminRepository
{
    // ── Dashboard aggregation sources ─────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetOrdersInRangeAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        => await db.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Include(o => o.Merchant).ThenInclude(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .Where(o => o.CreatedAt >= fromUtc && o.CreatedAt <= toUtc)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IEnumerable<MerchantProfile>> GetAllMerchantsWithStatsAsync(CancellationToken ct = default)
        => await db.MerchantProfiles
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.User)
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Reviews)
            .Include(m => m.Products)
            .Include(m => m.MpCredential)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<int> CountConsumersAsync(bool? active, CancellationToken ct = default)
        => await db.ConsumerProfiles
            .AsNoTracking()
            .Where(c => active == null || c.User.IsActive == active)
            .CountAsync(ct);

    /// <inheritdoc />
    public async Task<int> CountNewConsumersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        => await db.ConsumerProfiles
            .AsNoTracking()
            .CountAsync(c => c.CreatedAt >= fromUtc && c.CreatedAt <= toUtc, ct);

    /// <inheritdoc />
    public async Task<int> CountNewMerchantsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        => await db.MerchantProfiles
            .AsNoTracking()
            .CountAsync(m => m.CreatedAt >= fromUtc && m.CreatedAt <= toUtc, ct);

    // ── Merchant management ───────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<(List<MerchantProfile> Items, int Total)> GetMerchantsPageAsync(
        bool? active, int? categoryId, string? mpStatus, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.MerchantProfiles
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.User)
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Reviews)
            .Include(m => m.Orders)
            .AsQueryable();

        if (active is not null)
            query = query.Where(m => m.User.IsActive == active);

        if (categoryId is not null)
            query = query.Where(m => m.MerchantCategories.Any(mc => mc.CategoryId == categoryId));

        if (!string.IsNullOrWhiteSpace(mpStatus) && Enum.TryParse<MpConnectionStatus>(mpStatus, true, out var status))
            query = query.Where(m => m.MpConnectionStatus == status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<MerchantProfile?> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default)
        => await db.MerchantProfiles
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.User)
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Reviews)
            .Include(m => m.Products)
            .Include(m => m.Orders).ThenInclude(o => o.Consumer)
            .Include(m => m.Orders).ThenInclude(o => o.OrderDetails).ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(m => m.Id == merchantId, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<MpTokenRefreshLog>> GetMpLogsByMerchantAsync(int merchantId, CancellationToken ct = default)
        => await db.MpTokenRefreshLogs
            .AsNoTracking()
            .Where(l => l.MerchantId == merchantId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

    // ── User management ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<(List<User> Items, int Total)> GetUsersPageAsync(
        Role? role, bool? active, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .Include(u => u.ConsumerProfile)
            .Include(u => u.MerchantProfile)
            .AsQueryable();

        if (role is not null)
            query = query.Where(u => u.UserRoles.Any(r => r.Role == role));

        if (active is not null)
            query = query.Where(u => u.IsActive == active);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserWithRolesAndProfilesAsync(int userId, CancellationToken ct = default)
        => await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .Include(u => u.ConsumerProfile)
            .Include(u => u.MerchantProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
}
