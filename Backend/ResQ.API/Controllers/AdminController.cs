using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Admin;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Shared;
using ResQ.API.Extensions;
using ResQ.API.Services.Admin;
using ResQ.API.Services.Catalog;
using ResQ.API.Services.Reporting;

namespace ResQ.API.Controllers;

/// <summary>
/// Platform administration endpoints: the global dashboard, merchant and user management,
/// and exportable financial/operational reports. Every endpoint requires a valid JWT
/// with role <c>Admin</c>. There is no public registration for this role.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService, ICatalogService catalogService) : ControllerBase
{
    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the global platform dashboard (money, orders, merchants, consumers,
    /// reputation, time series, rankings, category distribution, and alerts) for the
    /// requested date range and granularity. Requires role: <c>Admin</c>.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> GetDashboard([FromQuery] DashboardQuery query, CancellationToken ct)
        => (await adminService.GetDashboardAsync(query, ct)).ToActionResult();

    // ── Merchant management ───────────────────────────────────────────────────

    /// <summary>Returns a filtered, paginated list of merchants. Requires role: <c>Admin</c>.</summary>
    [HttpGet("merchants")]
    public async Task<ActionResult<PagedResponse<AdminMerchantListItemResponse>>> GetMerchants(
        [FromQuery] MerchantListQuery query, CancellationToken ct)
        => (await adminService.GetMerchantsAsync(query, ct)).ToActionResult();

    /// <summary>Returns the full detail of a merchant, including recent orders and MP logs. Requires role: <c>Admin</c>.</summary>
    [HttpGet("merchants/{id:int}")]
    public async Task<ActionResult<AdminMerchantDetailResponse>> GetMerchantDetail(int id, CancellationToken ct)
        => (await adminService.GetMerchantDetailAsync(id, ct)).ToActionResult();

    /// <summary>Activates or deactivates a merchant. Requires role: <c>Admin</c>.</summary>
    [HttpPatch("merchants/{id:int}/status")]
    public async Task<IActionResult> SetMerchantStatus(int id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
        => (await adminService.SetMerchantStatusAsync(id, request.IsActive, ct)).ToActionResult();

    // ── User management ───────────────────────────────────────────────────────

    /// <summary>Returns a filtered, paginated list of user accounts. Requires role: <c>Admin</c>.</summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<AdminUserListItemResponse>>> GetUsers(
        [FromQuery] UserListQuery query, CancellationToken ct)
        => (await adminService.GetUsersAsync(query, ct)).ToActionResult();

    /// <summary>Activates or deactivates a user account (admins excluded). Requires role: <c>Admin</c>.</summary>
    [HttpPatch("users/{id:int}/status")]
    public async Task<IActionResult> SetUserStatus(int id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
        => (await adminService.SetUserStatusAsync(id, request.IsActive, ct)).ToActionResult();

    /// <summary>Returns the order history of a consumer user. Requires role: <c>Admin</c>.</summary>
    [HttpGet("users/{id:int}/orders")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> GetUserOrders(int id, CancellationToken ct)
        => (await adminService.GetUserOrdersAsync(id, ct)).ToActionResult();

    // ── Reports (PDF / Excel) ─────────────────────────────────────────────────

    /// <summary>Downloads the global financial/operational report. Requires role: <c>Admin</c>.</summary>
    [HttpGet("reports/global")]
    public async Task<IActionResult> GetGlobalReport([FromQuery] ReportQuery query, CancellationToken ct)
        => ToFileResult(await adminService.GenerateGlobalReportAsync(query, ct));

    /// <summary>Downloads the merchant ranking report (by revenue). Requires role: <c>Admin</c>.</summary>
    [HttpGet("reports/merchants-ranking")]
    public async Task<IActionResult> GetMerchantRankingReport([FromQuery] ReportQuery query, CancellationToken ct)
        => ToFileResult(await adminService.GenerateMerchantRankingReportAsync(query, ct));

    /// <summary>Downloads a per-merchant orders report. Requires role: <c>Admin</c>.</summary>
    [HttpGet("reports/merchants/{id:int}")]
    public async Task<IActionResult> GetMerchantReport(int id, [FromQuery] ReportQuery query, CancellationToken ct)
        => ToFileResult(await adminService.GenerateMerchantReportAsync(id, query, ct));

    // ── Category management ───────────────────────────────────────────────────

    /// <summary>Creates a new food category. Requires role: <c>Admin</c>.</summary>
    [HttpPost("categories")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory([FromBody] CategoryRequest request, CancellationToken ct)
        => (await catalogService.CreateCategoryAsync(request.Name, ct)).ToActionResult();

    /// <summary>Renames an existing category. Requires role: <c>Admin</c>.</summary>
    [HttpPut("categories/{id:int}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, [FromBody] CategoryRequest request, CancellationToken ct)
        => (await catalogService.UpdateCategoryAsync(id, request.Name, ct)).ToActionResult();

    /// <summary>
    /// Deletes a category. Returns 409 Conflict if merchants are still assigned to it.
    /// Requires role: <c>Admin</c>.
    /// </summary>
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
        => (await catalogService.DeleteCategoryAsync(id, ct)).ToActionResult();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a report <see cref="Result{ReportFile}"/> into a file download on success,
    /// or the appropriate problem-details error response on failure.
    /// </summary>
    private IActionResult ToFileResult(Result<ReportFile> result)
    {
        if (result.IsSuccess)
            return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);

        var error = result.Errors.FirstOrDefault();
        return error is NotFoundError
            ? NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = error.Message })
            : BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error?.Message });
    }
}
