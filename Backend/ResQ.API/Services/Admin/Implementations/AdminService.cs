using System.Globalization;
using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Admin;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Shared;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reporting;
using ResQ.API.Repositories.Admin;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Orders;
using ResQ.API.Services.Reporting;

namespace ResQ.API.Services.Admin;

/// <summary>
/// Implements the administration module: global dashboard aggregation, merchant and user
/// management, and exportable reports. Money metrics consider non-cancelled orders
/// (Paid + PickedUp) as realised revenue, consistent with the merchant dashboard.
/// </summary>
public class AdminService(
    IAdminRepository admin,
    IUserRepository users,
    IOrderRepository orders,
    IReportFileFactory reportFactory) : IAdminService
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-AR");
    private const int TopRankingSize = 5;
    private const int MpExpiryWarningDays = 7;

    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<AdminDashboardResponse>> GetDashboardAsync(DashboardQuery query, CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = ResolveRange(query.From, query.To);

        var rangeOrders = (await admin.GetOrdersInRangeAsync(fromUtc, toUtc, ct)).ToList();
        var merchants   = (await admin.GetAllMerchantsWithStatsAsync(ct)).ToList();

        var totalConsumers  = await admin.CountConsumersAsync(null, ct);
        var activeConsumers = await admin.CountConsumersAsync(true, ct);
        var newConsumers    = await admin.CountNewConsumersAsync(fromUtc, toUtc, ct);
        var newMerchants    = await admin.CountNewMerchantsAsync(fromUtc, toUtc, ct);

        var revenue   = rangeOrders.Where(IsRevenue).ToList();
        var completed = rangeOrders.Count(o => o.OrderStatus == OrderStatus.PickedUp);
        var cancelled = rangeOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled);
        var paidOrUp  = rangeOrders.Count(o => o.OrderStatus is OrderStatus.Paid or OrderStatus.PickedUp);

        var gmv             = revenue.Sum(o => o.TotalAmount);
        var platformRevenue = revenue.Sum(o => o.PlatformFee);
        var merchantEarn    = revenue.Sum(o => o.MerchantEarnings);

        var allReviews = merchants.SelectMany(m => m.Reviews).ToList();

        var response = new AdminDashboardResponse
        {
            RangeLabel = $"{fromUtc:dd/MM/yyyy} — {toUtc:dd/MM/yyyy}",

            Gmv               = gmv,
            PlatformRevenue   = platformRevenue,
            MerchantEarnings  = merchantEarn,
            AverageOrderValue = revenue.Count > 0 ? Math.Round(gmv / revenue.Count, 0) : 0,

            TotalOrders      = rangeOrders.Count,
            CompletedOrders  = completed,
            CancelledOrders  = cancelled,
            CancellationRate = rangeOrders.Count > 0 ? Math.Round((decimal)cancelled / rangeOrders.Count * 100, 1) : 0,
            PickupRate       = paidOrUp > 0 ? Math.Round((decimal)completed / paidOrUp * 100, 1) : 0,

            TotalMerchants  = merchants.Count,
            ActiveMerchants = merchants.Count(m => m.User.IsActive),
            MerchantsWithMp = merchants.Count(m => m.MpConnectionStatus == MpConnectionStatus.Connected),
            NewMerchants    = newMerchants,

            TotalConsumers  = totalConsumers,
            ActiveConsumers = activeConsumers,
            NewConsumers    = newConsumers,

            AverageRating = allReviews.Count > 0 ? Math.Round((decimal)allReviews.Average(r => r.Rating), 1) : 0,
            ReviewCount   = allReviews.Count,

            ActivePacks = merchants.SelectMany(m => m.Products).Count(p => p.IsActive),

            ActivitySeries        = BuildSeries(revenue, fromUtc, toUtc, query.Granularity),
            TopMerchantsByRevenue = BuildRanking(revenue, merchants, byRevenue: true),
            TopMerchantsByOrders  = BuildRanking(revenue, merchants, byRevenue: false),
            CategoryDistribution  = BuildCategoryDistribution(revenue),
            Alerts                = BuildAlerts(merchants)
        };

        return Result.Ok(response);
    }

    // ── Merchant management ───────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<PagedResponse<AdminMerchantListItemResponse>>> GetMerchantsAsync(
        MerchantListQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);

        var (items, total) = await admin.GetMerchantsPageAsync(
            query.Active, query.CategoryId, query.MpStatus, page, pageSize, ct);

        var mapped = items.Select(m =>
        {
            var revenue = m.Orders.Where(IsRevenue).ToList();
            return new AdminMerchantListItemResponse
            {
                Id                 = m.Id,
                BusinessName       = m.BusinessName,
                Cuit               = m.Cuit,
                Email              = m.User.Email,
                IsActive           = m.User.IsActive,
                MpConnectionStatus = m.MpConnectionStatus.ToString(),
                Categories         = m.MerchantCategories.Select(mc => mc.Category.Name).ToList(),
                TotalOrders        = m.Orders.Count(o => o.OrderStatus == OrderStatus.PickedUp),
                TotalRevenue       = revenue.Sum(o => o.PlatformFee),
                AverageRating      = m.Reviews.Count > 0 ? Math.Round((decimal)m.Reviews.Average(r => r.Rating), 1) : 0,
                CreatedAt          = m.CreatedAt
            };
        }).ToList();

        return Result.Ok(new PagedResponse<AdminMerchantListItemResponse>
        {
            Items      = mapped,
            Page       = page,
            PageSize   = pageSize,
            TotalItems = total
        });
    }

    /// <inheritdoc />
    public async Task<Result<AdminMerchantDetailResponse>> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default)
    {
        var m = await admin.GetMerchantDetailAsync(merchantId, ct);
        if (m is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        var logs    = (await admin.GetMpLogsByMerchantAsync(merchantId, ct)).ToList();
        var revenue = m.Orders.Where(IsRevenue).ToList();

        return Result.Ok(new AdminMerchantDetailResponse
        {
            Id                 = m.Id,
            BusinessName       = m.BusinessName,
            Cuit               = m.Cuit,
            Email              = m.User.Email,
            Address            = m.Address,
            ContactPhone       = m.ContactPhone,
            IsActive           = m.User.IsActive,
            MpConnectionStatus = m.MpConnectionStatus.ToString(),
            Categories         = m.MerchantCategories.Select(mc => mc.Category.Name).ToList(),
            TotalOrders        = m.Orders.Count(o => o.OrderStatus == OrderStatus.PickedUp),
            TotalGmv           = revenue.Sum(o => o.TotalAmount),
            TotalRevenue       = revenue.Sum(o => o.PlatformFee),
            AverageRating      = m.Reviews.Count > 0 ? Math.Round((decimal)m.Reviews.Average(r => r.Rating), 1) : 0,
            ReviewCount        = m.Reviews.Count,
            ActivePacks        = m.Products.Count(p => p.IsActive),
            CreatedAt          = m.CreatedAt,
            RecentOrders       = m.Orders
                                    .OrderByDescending(o => o.CreatedAt)
                                    .Take(10)
                                    .Select(MapMerchantOrder)
                                    .ToList(),
            MpRefreshLogs      = logs.Select(l => new MpRefreshLogResponse(l.Success, l.ErrorMessage, l.CreatedAt)).ToList()
        });
    }

    /// <inheritdoc />
    public async Task<Result> SetMerchantStatusAsync(int merchantId, bool isActive, CancellationToken ct = default)
    {
        var m = await admin.GetMerchantDetailAsync(merchantId, ct);
        if (m is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        return await SetUserStatusAsync(m.UserId, isActive, ct);
    }

    // ── User management ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<PagedResponse<AdminUserListItemResponse>>> GetUsersAsync(
        UserListQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);

        var (items, total) = await admin.GetUsersPageAsync(query.Role, query.Active, page, pageSize, ct);

        var mapped = items.Select(MapUser).ToList();

        return Result.Ok(new PagedResponse<AdminUserListItemResponse>
        {
            Items      = mapped,
            Page       = page,
            PageSize   = pageSize,
            TotalItems = total
        });
    }

    /// <inheritdoc />
    public async Task<Result> SetUserStatusAsync(int userId, bool isActive, CancellationToken ct = default)
    {
        var user = await users.GetWithRolesAsync(userId, ct);
        if (user is null)
            return Result.Fail(new NotFoundError("Usuario no encontrado."));

        if (user.UserRoles.Any(r => r.Role == Role.Admin))
            return Result.Fail(new BadRequestError("No se puede desactivar una cuenta de administrador."));

        user.IsActive  = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        users.Update(user);
        await users.SaveChangesAsync(ct);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<OrderSummaryResponse>>> GetUserOrdersAsync(int userId, CancellationToken ct = default)
    {
        var user = await admin.GetUserWithRolesAndProfilesAsync(userId, ct);
        if (user is null)
            return Result.Fail(new NotFoundError("Usuario no encontrado."));

        if (user.ConsumerProfile is null)
            return Result.Fail(new NotFoundError("El usuario no es un consumidor."));

        var history = await orders.GetByConsumerIdAsync(user.ConsumerProfile.Id, ct);

        return Result.Ok(history.Select(o => new OrderSummaryResponse
        {
            Id                = o.Id,
            ExternalReference = o.ExternalReference,
            MerchantName      = o.Merchant.BusinessName,
            TotalAmount       = o.TotalAmount,
            OrderStatus       = o.OrderStatus.ToString(),
            PickupCode        = o.PickupCode,
            CreatedAt         = o.CreatedAt,
            HasReview         = o.Review is not null,
            Items             = o.OrderDetails.Select(od => new DTOs.Shared.OrderDetailItemResponse
            {
                ProductName = od.Product.Name,
                Quantity    = od.Quantity,
                UnitPrice   = od.UnitPrice
            }).ToList()
        }));
    }

    // ── Reports ───────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Result<ReportFile>> GenerateGlobalReportAsync(ReportQuery query, CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = ResolveRange(query.From, query.To);
        var rangeOrders = (await admin.GetOrdersInRangeAsync(fromUtc, toUtc, ct)).ToList();
        var revenue     = rangeOrders.Where(IsRevenue).ToList();

        var series = BuildSeries(revenue, fromUtc, toUtc, ReportGranularity.Day);

        var model = new ReportModel
        {
            Title    = "Reporte financiero global",
            Subtitle = $"Período: {fromUtc:dd/MM/yyyy} — {toUtc:dd/MM/yyyy}",
            Kpis =
            [
                new ReportKpi("GMV (ventas brutas)",   FormatCurrency(revenue.Sum(o => o.TotalAmount))),
                new ReportKpi("Ingresos de plataforma", FormatCurrency(revenue.Sum(o => o.PlatformFee))),
                new ReportKpi("Ganancias de comercios", FormatCurrency(revenue.Sum(o => o.MerchantEarnings))),
                new ReportKpi("Órdenes (no canceladas)", revenue.Count.ToString("N0", Es)),
                new ReportKpi("Órdenes completadas",      rangeOrders.Count(o => o.OrderStatus == OrderStatus.PickedUp).ToString("N0", Es)),
                new ReportKpi("Órdenes canceladas",       rangeOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled).ToString("N0", Es))
            ],
            Columns =
            [
                new ReportColumn { Header = "Fecha",                 Type = ReportColumnType.Text,     Width = 2 },
                new ReportColumn { Header = "Órdenes",               Type = ReportColumnType.Number,   Width = 1 },
                new ReportColumn { Header = "GMV",                   Type = ReportColumnType.Currency, Width = 2 },
                new ReportColumn { Header = "Ingresos plataforma",   Type = ReportColumnType.Currency, Width = 2 }
            ],
            Rows = series.Select(p => new object?[] { p.Label, p.Orders, p.Gmv, p.PlatformRevenue }).ToList(),
            TotalsRow = ["Total", series.Sum(p => p.Orders), revenue.Sum(o => o.TotalAmount), revenue.Sum(o => o.PlatformFee)]
        };

        var file = reportFactory.Create(model, query.Format, $"resq-reporte-global_{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}");
        return Result.Ok(file);
    }

    /// <inheritdoc />
    public async Task<Result<ReportFile>> GenerateMerchantRankingReportAsync(ReportQuery query, CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = ResolveRange(query.From, query.To);
        var rangeOrders = (await admin.GetOrdersInRangeAsync(fromUtc, toUtc, ct)).ToList();
        var merchants   = (await admin.GetAllMerchantsWithStatsAsync(ct)).ToList();
        var revenue     = rangeOrders.Where(IsRevenue).ToList();

        var ranking = BuildRanking(revenue, merchants, byRevenue: true, top: int.MaxValue);

        var model = new ReportModel
        {
            Title    = "Ranking de comercios",
            Subtitle = $"Período: {fromUtc:dd/MM/yyyy} — {toUtc:dd/MM/yyyy}",
            Kpis =
            [
                new ReportKpi("Comercios con ventas", ranking.Count.ToString("N0", Es)),
                new ReportKpi("GMV total",            FormatCurrency(revenue.Sum(o => o.TotalAmount))),
                new ReportKpi("Ingresos plataforma",  FormatCurrency(revenue.Sum(o => o.PlatformFee)))
            ],
            Columns =
            [
                new ReportColumn { Header = "Comercio",            Type = ReportColumnType.Text,     Width = 3 },
                new ReportColumn { Header = "Órdenes",             Type = ReportColumnType.Number,   Width = 1 },
                new ReportColumn { Header = "GMV",                 Type = ReportColumnType.Currency, Width = 2 },
                new ReportColumn { Header = "Ingresos plataforma", Type = ReportColumnType.Currency, Width = 2 },
                new ReportColumn { Header = "Rating",              Type = ReportColumnType.Number,   Width = 1 }
            ],
            Rows = ranking.Select(r => new object?[] { r.BusinessName, r.Orders, r.Gmv, r.PlatformRevenue, r.AverageRating }).ToList(),
            TotalsRow = ["Total", ranking.Sum(r => r.Orders), revenue.Sum(o => o.TotalAmount), revenue.Sum(o => o.PlatformFee), null]
        };

        var file = reportFactory.Create(model, query.Format, $"resq-ranking-comercios_{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}");
        return Result.Ok(file);
    }

    /// <inheritdoc />
    public async Task<Result<ReportFile>> GenerateMerchantReportAsync(int merchantId, ReportQuery query, CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = ResolveRange(query.From, query.To);

        var m = await admin.GetMerchantDetailAsync(merchantId, ct);
        if (m is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        var rangeOrders = m.Orders.Where(o => o.CreatedAt >= fromUtc && o.CreatedAt <= toUtc).ToList();
        var revenue     = rangeOrders.Where(IsRevenue).ToList();

        var model = new ReportModel
        {
            Title    = $"Reporte de comercio — {m.BusinessName}",
            Subtitle = $"Período: {fromUtc:dd/MM/yyyy} — {toUtc:dd/MM/yyyy}",
            Kpis =
            [
                new ReportKpi("Órdenes completadas", rangeOrders.Count(o => o.OrderStatus == OrderStatus.PickedUp).ToString("N0", Es)),
                new ReportKpi("GMV",                 FormatCurrency(revenue.Sum(o => o.TotalAmount))),
                new ReportKpi("Ganancias",           FormatCurrency(revenue.Sum(o => o.MerchantEarnings)))
            ],
            Columns =
            [
                new ReportColumn { Header = "Fecha",   Type = ReportColumnType.Date,     Width = 2 },
                new ReportColumn { Header = "Estado",  Type = ReportColumnType.Text,     Width = 2 },
                new ReportColumn { Header = "Total",   Type = ReportColumnType.Currency, Width = 2 },
                new ReportColumn { Header = "Ganancia",Type = ReportColumnType.Currency, Width = 2 }
            ],
            Rows = rangeOrders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new object?[] { o.CreatedAt, TranslateStatus(o.OrderStatus), o.TotalAmount, o.MerchantEarnings })
                .ToList(),
            TotalsRow = ["Total", null, revenue.Sum(o => o.TotalAmount), revenue.Sum(o => o.MerchantEarnings)]
        };

        var file = reportFactory.Create(model, query.Format, $"resq-comercio-{merchantId}_{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}");
        return Result.Ok(file);
    }

    // ── Private helpers ─────────────────────────────────────────────────────────

    /// <summary>An order counts as realised revenue when it is not cancelled (Paid or PickedUp).</summary>
    private static bool IsRevenue(Order o) => o.OrderStatus != OrderStatus.Cancelled;

    /// <summary>
    /// Resolves an optional date range into a concrete inclusive UTC window. Defaults to the
    /// last 30 days; the end date is pushed to the end of its day so "today" is fully included.
    /// </summary>
    private static (DateTime From, DateTime To) ResolveRange(DateTime? from, DateTime? to)
    {
        var fromDate = (from ?? DateTime.UtcNow.AddDays(-30)).Date;
        var toDate   = (to   ?? DateTime.UtcNow).Date;
        if (fromDate > toDate) (fromDate, toDate) = (toDate, fromDate);

        // Query params bind as Kind=Unspecified; Npgsql requires UTC for timestamptz columns.
        var fromUtc = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
        var toUtc   = DateTime.SpecifyKind(toDate.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        return (fromUtc, toUtc);
    }

    /// <summary>Clamps paging parameters into safe bounds (page ≥ 1, 1 ≤ size ≤ 100).</summary>
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
        => (Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 10 : pageSize, 1, 100));

    /// <summary>Buckets revenue orders into an ordered activity time series by the given granularity.</summary>
    private static List<ActivityPointDto> BuildSeries(
        List<Order> revenue, DateTime fromUtc, DateTime toUtc, ReportGranularity granularity)
    {
        var bucketStarts = new List<DateTime>();

        switch (granularity)
        {
            case ReportGranularity.Week:
                for (var d = StartOfWeek(fromUtc.Date); d <= toUtc.Date; d = d.AddDays(7))
                    bucketStarts.Add(d);
                break;
            case ReportGranularity.Month:
                for (var d = new DateTime(fromUtc.Year, fromUtc.Month, 1); d <= toUtc.Date; d = d.AddMonths(1))
                    bucketStarts.Add(d);
                break;
            default: // Day
                for (var d = fromUtc.Date; d <= toUtc.Date; d = d.AddDays(1))
                    bucketStarts.Add(d);
                break;
        }

        return bucketStarts.Select(start =>
        {
            var end = granularity switch
            {
                ReportGranularity.Week  => start.AddDays(7),
                ReportGranularity.Month => start.AddMonths(1),
                _                       => start.AddDays(1)
            };

            var inBucket = revenue.Where(o => o.CreatedAt >= start && o.CreatedAt < end).ToList();

            var label = granularity == ReportGranularity.Month
                ? Capitalize(start.ToString("MMM yy", Es))
                : start.ToString("dd/MM", Es);

            return new ActivityPointDto(
                label,
                inBucket.Count,
                inBucket.Sum(o => o.TotalAmount),
                inBucket.Sum(o => o.PlatformFee));
        }).ToList();
    }

    /// <summary>Builds a merchant ranking from revenue orders, ordered by revenue or order count.</summary>
    private static List<MerchantRankingDto> BuildRanking(
        List<Order> revenue, List<MerchantProfile> merchants, bool byRevenue, int top = TopRankingSize)
    {
        var ratingByMerchant = merchants.ToDictionary(
            m => m.Id,
            m => m.Reviews.Count > 0 ? Math.Round((decimal)m.Reviews.Average(r => r.Rating), 1) : 0m);

        var grouped = revenue
            .GroupBy(o => new { o.MerchantId, o.Merchant.BusinessName })
            .Select(g => new MerchantRankingDto(
                g.Key.MerchantId,
                g.Key.BusinessName,
                g.Count(o => o.OrderStatus == OrderStatus.PickedUp),
                g.Sum(o => o.TotalAmount),
                g.Sum(o => o.PlatformFee),
                ratingByMerchant.GetValueOrDefault(g.Key.MerchantId)));

        grouped = byRevenue
            ? grouped.OrderByDescending(r => r.PlatformRevenue)
            : grouped.OrderByDescending(r => r.Orders);

        return grouped.Take(top).ToList();
    }

    /// <summary>Distributes revenue orders across food categories (first category per merchant).</summary>
    private static List<CategorySliceDto> BuildCategoryDistribution(List<Order> revenue)
        => revenue
            .GroupBy(o => o.Merchant.MerchantCategories.FirstOrDefault()?.Category.Name ?? "Sin categoría")
            .Select(g => new CategorySliceDto(g.Key, g.Count(), g.Sum(o => o.TotalAmount)))
            .OrderByDescending(c => c.Gmv)
            .ToList();

    /// <summary>Builds operational alerts from MP credential expiry state and missing connections.</summary>
    private static List<AdminAlertDto> BuildAlerts(List<MerchantProfile> merchants)
    {
        var now    = DateTime.UtcNow;
        var alerts = new List<AdminAlertDto>();

        foreach (var m in merchants.Where(m => m.MpCredential is { IsActive: true }))
        {
            var expiresAt = m.MpCredential!.AccessTokenExpiresAt;
            if (expiresAt < now)
                alerts.Add(new AdminAlertDto("critical", "Token de Mercado Pago vencido",
                    $"{m.BusinessName} tiene el token de MP vencido y no puede cobrar.", m.Id));
            else if (expiresAt < now.AddDays(MpExpiryWarningDays))
                alerts.Add(new AdminAlertDto("warning", "Token de Mercado Pago por vencer",
                    $"{m.BusinessName} renovará su token de MP el {expiresAt:dd/MM/yyyy}.", m.Id));
        }

        var withoutMp = merchants.Count(m => m.MpConnectionStatus != MpConnectionStatus.Connected);
        if (withoutMp > 0)
            alerts.Add(new AdminAlertDto("info", "Comercios sin Mercado Pago",
                $"{withoutMp} comercio(s) aún no conectaron su cuenta de Mercado Pago.", null));

        return alerts;
    }

    private static AdminUserListItemResponse MapUser(User u)
    {
        var primaryRole = u.UserRoles.FirstOrDefault()?.Role ?? Role.Consumer;
        var displayName = u.MerchantProfile?.BusinessName
                          ?? (u.ConsumerProfile is not null
                                ? $"{u.ConsumerProfile.FirstName} {u.ConsumerProfile.LastName}".Trim()
                                : u.Email.Split('@')[0]);

        return new AdminUserListItemResponse
        {
            Id          = u.Id,
            Email       = u.Email,
            Role        = primaryRole.ToString(),
            DisplayName = displayName,
            IsActive    = u.IsActive,
            ProfileId   = u.MerchantProfile?.Id ?? u.ConsumerProfile?.Id,
            CreatedAt   = u.CreatedAt
        };
    }

    private static MerchantOrderSummaryResponse MapMerchantOrder(Order o) => new()
    {
        Id           = o.Id,
        ConsumerName = o.Consumer is not null ? $"{o.Consumer.FirstName} {o.Consumer.LastName}".Trim() : "—",
        TotalAmount  = o.TotalAmount,
        OrderStatus  = o.OrderStatus.ToString(),
        PickupCode   = o.PickupCode,
        CreatedAt    = o.CreatedAt,
        Items        = o.OrderDetails.Select(od => new DTOs.Shared.OrderDetailItemResponse
        {
            ProductName = od.Product.Name,
            Quantity    = od.Quantity,
            UnitPrice   = od.UnitPrice
        }).ToList()
    };

    private static string FormatCurrency(decimal value) => "$" + value.ToString("N0", Es);

    private static string TranslateStatus(OrderStatus status) => status switch
    {
        OrderStatus.Pending   => "Pendiente",
        OrderStatus.Paid      => "Pagado",
        OrderStatus.PickedUp  => "Retirado",
        OrderStatus.Cancelled => "Cancelado",
        _                     => status.ToString()
    };

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
