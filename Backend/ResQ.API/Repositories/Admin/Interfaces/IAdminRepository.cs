using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Orders;

namespace ResQ.API.Repositories.Admin;

/// <summary>
/// Cross-entity data access for the administration module. Unlike the per-actor
/// repositories, these queries span all merchants, consumers, and orders to power
/// the global dashboard, management lists, and exportable reports.
/// </summary>
public interface IAdminRepository
{
    // ── Dashboard aggregation sources ─────────────────────────────────────────

    /// <summary>
    /// Returns all orders created within the inclusive UTC range, eagerly loading line items,
    /// products, the merchant, and the merchant's categories. Used to compute money, order,
    /// ranking, category, and time-series metrics in one pass.
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersInRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>
    /// Returns every merchant profile with the data needed for platform-wide stats and alerts:
    /// owning user, categories, reviews, products, and Mercado Pago credentials.
    /// </summary>
    Task<IEnumerable<MerchantProfile>> GetAllMerchantsWithStatsAsync(CancellationToken ct = default);

    /// <summary>Counts consumer accounts, optionally filtered by active state.</summary>
    Task<int> CountConsumersAsync(bool? active, CancellationToken ct = default);

    /// <summary>Counts consumers whose account was created within the inclusive UTC range.</summary>
    Task<int> CountNewConsumersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>Counts merchants whose profile was created within the inclusive UTC range.</summary>
    Task<int> CountNewMerchantsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    // ── Merchant management ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a filtered, paginated page of merchants for the management list, together with
    /// the total matching count. Eagerly loads user, categories, reviews, and orders so the
    /// service can compute headline metrics per merchant.
    /// </summary>
    Task<(List<MerchantProfile> Items, int Total)> GetMerchantsPageAsync(
        bool? active, int? categoryId, string? mpStatus, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns the full detail of a single merchant: user, categories, reviews, products,
    /// and orders (with consumer and line items), or null if not found.
    /// </summary>
    Task<MerchantProfile?> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default);

    /// <summary>Returns the Mercado Pago token-refresh log entries for a merchant, newest first.</summary>
    Task<IEnumerable<MpTokenRefreshLog>> GetMpLogsByMerchantAsync(int merchantId, CancellationToken ct = default);

    // ── User management ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns a filtered, paginated page of user accounts for the management list, together
    /// with the total matching count. Eagerly loads roles and both profile types.
    /// </summary>
    Task<(List<User> Items, int Total)> GetUsersPageAsync(
        Role? role, bool? active, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Returns a user with roles and both profile types loaded, or null if not found.</summary>
    Task<User?> GetUserWithRolesAndProfilesAsync(int userId, CancellationToken ct = default);
}
