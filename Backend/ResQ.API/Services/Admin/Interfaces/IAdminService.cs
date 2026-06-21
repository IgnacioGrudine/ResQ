using FluentResults;
using ResQ.API.DTOs.Admin;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Shared;
using ResQ.API.Services.Reporting;

namespace ResQ.API.Services.Admin;

/// <summary>
/// Business logic for the administration module: the global platform dashboard,
/// merchant and user management, and exportable financial/operational reports.
/// All metrics cover money, orders, merchants, and users — no environmental-impact data.
/// </summary>
public interface IAdminService
{
    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the global platform dashboard for the requested date range and granularity.
    /// </summary>
    /// <param name="query">Date range and time-series granularity (with sensible defaults).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A successful result with the aggregated <see cref="AdminDashboardResponse"/>.</returns>
    Task<Result<AdminDashboardResponse>> GetDashboardAsync(DashboardQuery query, CancellationToken ct = default);

    // ── Merchant management ───────────────────────────────────────────────────

    /// <summary>Returns a filtered, paginated page of merchants for the management list.</summary>
    Task<Result<PagedResponse<AdminMerchantListItemResponse>>> GetMerchantsAsync(
        MerchantListQuery query, CancellationToken ct = default);

    /// <summary>
    /// Returns the full admin detail of a merchant, including recent orders and MP refresh logs.
    /// Fails with a <c>NotFoundError</c> if the merchant does not exist.
    /// </summary>
    Task<Result<AdminMerchantDetailResponse>> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Activates or deactivates a merchant by toggling the owning user's <c>IsActive</c> flag.
    /// Fails with a <c>NotFoundError</c> if the merchant does not exist.
    /// </summary>
    Task<Result> SetMerchantStatusAsync(int merchantId, bool isActive, CancellationToken ct = default);

    // ── User management ───────────────────────────────────────────────────────

    /// <summary>Returns a filtered, paginated page of user accounts for the management list.</summary>
    Task<Result<PagedResponse<AdminUserListItemResponse>>> GetUsersAsync(
        UserListQuery query, CancellationToken ct = default);

    /// <summary>
    /// Activates or deactivates a user account by toggling its <c>IsActive</c> flag.
    /// Admin accounts cannot be deactivated. Fails with a <c>NotFoundError</c> if the user
    /// does not exist, or a <c>BadRequestError</c> when targeting an admin.
    /// </summary>
    Task<Result> SetUserStatusAsync(int userId, bool isActive, CancellationToken ct = default);

    /// <summary>
    /// Returns the order history of a consumer user. Fails with a <c>NotFoundError</c> if the
    /// user does not exist or is not a consumer.
    /// </summary>
    Task<Result<IEnumerable<OrderSummaryResponse>>> GetUserOrdersAsync(int userId, CancellationToken ct = default);

    // ── Reports (PDF / Excel) ─────────────────────────────────────────────────

    /// <summary>Generates the global financial/operational report (activity by period) as PDF or Excel.</summary>
    Task<Result<ReportFile>> GenerateGlobalReportAsync(ReportQuery query, CancellationToken ct = default);

    /// <summary>Generates the merchant ranking report (by revenue) as PDF or Excel.</summary>
    Task<Result<ReportFile>> GenerateMerchantRankingReportAsync(ReportQuery query, CancellationToken ct = default);

    /// <summary>
    /// Generates a per-merchant orders report as PDF or Excel.
    /// Fails with a <c>NotFoundError</c> if the merchant does not exist.
    /// </summary>
    Task<Result<ReportFile>> GenerateMerchantReportAsync(int merchantId, ReportQuery query, CancellationToken ct = default);
}
