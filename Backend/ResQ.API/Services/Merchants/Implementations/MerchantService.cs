using System.Globalization;
using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Admin;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Products;
using ResQ.API.DTOs.Reviews;
using ResQ.API.DTOs.Shared;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reporting;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.Orders;
using ResQ.API.Repositories.Reviews;
using ResQ.API.Services.Orders;
using ResQ.API.Services.Reporting;
using ResQ.API.Services.Storage;

namespace ResQ.API.Services.Merchants;

/// <summary>
/// Manages merchant-facing operations: public catalog exposure, authenticated profile management,
/// order confirmation, dashboard metrics, filterable analytics, exportable reports,
/// review retrieval, and profile photo upload.
/// </summary>
public class MerchantService(
    IMerchantProfileRepository merchantProfiles,
    IMerchantCategoryRepository merchantCategories,
    IReviewRepository reviews,
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IOrderService orderService,
    IImageStorageService imageStorage,
    IReportFileFactory reportFactory) : IMerchantService
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-AR");
    // ─── Public catalog ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns a summary list of all merchants available in the public catalog,
    /// including their categories, average rating, and minimum pack price.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an enumerable of <see cref="MerchantListItemResponse"/>.
    /// </returns>
    public async Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default)
    {
        var merchants = await merchantProfiles.GetAllWithCatalogDataAsync(ct);

        var result = merchants.Select(m => new MerchantListItemResponse
        {
            Id                 = m.Id,
            BusinessName       = m.BusinessName,
            Address            = m.Address,
            Latitude           = m.Latitude,
            Longitude          = m.Longitude,
            ContactPhone       = m.ContactPhone,
            PhotoUrl           = imageStorage.ResolvePublicUrl(m.PhotoUrl),
            Categories         = m.MerchantCategories.Select(mc => mc.Category.Name).ToList(),
            AverageRating      = m.Reviews.Any() ? Math.Round((decimal)m.Reviews.Average(r => r.Rating), 1) : 0,
            ReviewCount        = m.Reviews.Count,
            MinSalePrice       = m.Products.Any() ? m.Products.Min(p => p.SalePrice) : 0,
            ActiveProductCount = m.Products.Count
        });

        return Result.Ok(result);
    }

    // ─── Public detail ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full public profile of a single merchant, including their active packs,
    /// categories, aggregated rating, and all of their reviews (newest first) so the client
    /// can paginate and filter by star rating without losing data behind a server-side cap.
    /// </summary>
    /// <param name="merchantId">The merchant profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{MerchantDetailResponse}"/>,
    /// or a <c>NotFoundError</c> if no merchant with the given ID exists.
    /// </returns>
    public async Task<Result<MerchantDetailResponse>> GetByIdAsync(int merchantId, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdWithPublicDetailAsync(merchantId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        return Result.Ok(new MerchantDetailResponse
        {
            Id             = merchant.Id,
            BusinessName   = merchant.BusinessName,
            Address        = merchant.Address,
            Latitude       = merchant.Latitude,
            Longitude      = merchant.Longitude,
            ContactPhone   = merchant.ContactPhone,
            PhotoUrl       = imageStorage.ResolvePublicUrl(merchant.PhotoUrl),
            Categories     = merchant.MerchantCategories
                                 .Select(mc => new CategoryResponse { Id = mc.CategoryId, Name = mc.Category.Name })
                                 .ToList(),
            AverageRating  = merchant.Reviews.Any() ? Math.Round((decimal)merchant.Reviews.Average(r => r.Rating), 1) : 0,
            ReviewCount    = merchant.Reviews.Count,
            ActiveProducts = merchant.Products.Select(MapProduct).ToList(),
            RecentReviews  = merchant.Reviews
                                 .OrderByDescending(r => r.CreatedAt)
                                 .Select(r => new ReviewResponse
                                 {
                                     Id        = r.Id,
                                     Rating    = r.Rating,
                                     Comment   = r.Comment,
                                     CreatedAt = r.CreatedAt
                                 })
                                 .ToList()
        });
    }

    // ─── Authenticated merchant — own profile ─────────────────────────────────

    /// <summary>
    /// Retrieves the authenticated merchant's own profile, including their assigned categories
    /// and Mercado Pago connection status.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{MerchantProfileResponse}"/>,
    /// or a <c>NotFoundError</c> if the profile does not exist.
    /// </returns>
    public async Task<Result<MerchantProfileResponse>> GetMyProfileAsync(int merchantProfileId, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdWithCategoriesAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        return Result.Ok(MapMerchantProfile(merchant));
    }

    /// <summary>
    /// Updates the authenticated merchant's editable profile fields and replaces their
    /// category assignments when a non-empty category list is provided.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="request">The update payload with business details and an optional list of category IDs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{MerchantProfileResponse}"/> with the refreshed profile,
    /// or a <c>NotFoundError</c> if the profile does not exist.
    /// </returns>
    /// <remarks>
    /// Category replacement is all-or-nothing: if <c>request.CategoryIds</c> is non-empty,
    /// all existing <c>MerchantCategory</c> rows for this merchant are deleted and replaced
    /// with the new set. Duplicate IDs in the request are de-duplicated via <c>Distinct()</c>.
    /// If <c>request.CategoryIds</c> is empty, the current categories are left unchanged.
    /// </remarks>
    public async Task<Result<MerchantProfileResponse>> UpdateMyProfileAsync(
        int merchantProfileId, UpdateMerchantProfileRequest request, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        merchant.BusinessName = request.BusinessName;
        merchant.Address      = request.Address;
        merchant.Latitude     = request.Latitude;
        merchant.Longitude    = request.Longitude;
        merchant.ContactPhone = request.ContactPhone;
        merchant.UpdatedAt    = DateTime.UtcNow;
        merchantProfiles.Update(merchant);

        if (request.CategoryIds.Count > 0)
        {
            var existing = await merchantCategories.GetByMerchantIdAsync(merchantProfileId, ct);
            merchantCategories.DeleteRange(existing);

            foreach (var catId in request.CategoryIds.Distinct())
                await merchantCategories.AddAsync(
                    new MerchantCategory { MerchantId = merchantProfileId, CategoryId = catId }, ct);
        }

        await merchantProfiles.SaveChangesAsync(ct);

        var updated = await merchantProfiles.GetByIdWithCategoriesAsync(merchantProfileId, ct);
        return Result.Ok(MapMerchantProfile(updated!));
    }

    // ─── Authenticated merchant — orders ─────────────────────────────────────

    /// <summary>
    /// Returns a summary list of all orders received by the authenticated merchant.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with an enumerable of <see cref="MerchantOrderSummaryResponse"/>.
    /// Delegates to <see cref="IOrderService.GetMerchantOrdersAsync"/>.
    /// </returns>
    public Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMyOrdersAsync(int merchantProfileId, CancellationToken ct = default)
        => orderService.GetMerchantOrdersAsync(merchantProfileId, ct);

    /// <summary>
    /// Confirms a consumer's pack pickup by validating the provided pickup code against
    /// orders belonging to the authenticated merchant.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="pickupCode">The alphanumeric pickup code presented by the consumer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{MerchantOrderSummaryResponse}"/> with the confirmed order,
    /// or a business error if the code is invalid, expired, or already used.
    /// Delegates to <see cref="IOrderService.ConfirmPickupAsync"/>.
    /// </returns>
    public Task<Result<MerchantOrderSummaryResponse>> ConfirmPickupAsync(
        int merchantProfileId, string pickupCode, CancellationToken ct = default)
        => orderService.ConfirmPickupAsync(merchantProfileId, pickupCode, ct);

    // ─── Authenticated merchant — dashboard ──────────────────────────────────

    /// <summary>
    /// Computes and returns the merchant's operational dashboard metrics, including
    /// today's income, weekly sales chart data, total packs sold, food rescued in kg,
    /// and average rating.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{MerchantDashboardResponse}"/> with aggregated metrics,
    /// or a <c>NotFoundError</c> if the merchant profile does not exist.
    /// </returns>
    /// <remarks>
    /// All aggregations are performed in memory after loading the necessary data from the
    /// database. The weekly sales chart covers the last 7 days ending today (UTC), with
    /// day labels localised to Spanish short names. Food rescued is estimated at 1 kg per
    /// pack sold. Cancelled orders are excluded from all financial and sustainability metrics.
    /// </remarks>
    public async Task<Result<MerchantDashboardResponse>> GetDashboardAsync(int merchantProfileId, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        var orders   = (await orderRepository.GetByMerchantIdAsync(merchantProfileId, ct)).ToList();
        var products = (await productRepository.GetByMerchantIdAsync(merchantProfileId, ct)).ToList();
        var allReviews = (await reviews.GetByMerchantIdAsync(merchantProfileId, ct)).ToList();

        var today     = DateTime.UtcNow.Date;
        var completed = orders.Where(o => o.OrderStatus != OrderStatus.Cancelled).ToList();
        var todayDone = completed.Where(o => o.CreatedAt.Date == today).ToList();

        var totalPacks = completed.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity);

        // Last 7 days ending today (index 0 = 6 days ago, index 6 = today)
        var dayLabels   = new[] { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };
        var weeklySales = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-6 + i))
            .Select(day => new DailySalesDto(
                Day:    dayLabels[(int)day.DayOfWeek],
                Orders: completed.Count(o => o.CreatedAt.Date == day),
                Income: completed.Where(o => o.CreatedAt.Date == day).Sum(o => o.MerchantEarnings)
            ))
            .ToList();

        return Result.Ok(new MerchantDashboardResponse
        {
            BusinessName       = merchant.BusinessName,

            ActiveOrders       = orders.Count(o => o.OrderStatus == OrderStatus.Paid),
            TodayIncome        = todayDone.Sum(o => o.MerchantEarnings),
            PacksSoldToday     = todayDone.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity),

            TotalSales         = completed.Count,
            TotalIncome        = completed.Sum(o => o.MerchantEarnings),
            KgFoodRescued      = Math.Round(totalPacks * 1.0m, 1),

            AverageRating      = allReviews.Count > 0
                                     ? Math.Round((decimal)allReviews.Average(r => r.Rating), 1) : 0,
            ReviewCount        = allReviews.Count,

            ActivePackCount    = products.Count(p => p.IsActive),
            MpConnectionStatus = merchant.MpConnectionStatus.ToString(),

            WeeklySales        = weeklySales
        });
    }

    // ─── Authenticated merchant — reviews ────────────────────────────────────

    /// <summary>
    /// Returns all reviews written for the authenticated merchant's business.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an enumerable of <see cref="ReviewResponse"/>.
    /// </returns>
    public async Task<Result<IEnumerable<ReviewResponse>>> GetMyReviewsAsync(int merchantProfileId, CancellationToken ct = default)
    {
        var result = await reviews.GetByMerchantIdAsync(merchantProfileId, ct);

        return Result.Ok(result.Select(r => new ReviewResponse
        {
            Id        = r.Id,
            Rating    = r.Rating,
            Comment   = r.Comment,
            CreatedAt = r.CreatedAt
        }));
    }

    // ─── Authenticated merchant — photo ──────────────────────────────────────

    /// <summary>
    /// Uploads a new profile photo for the authenticated merchant to MinIO object storage
    /// and persists the resulting public URL on the merchant profile.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="file">The image file to upload. Validated for allowed content type and size by <see cref="IImageStorageService"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{MerchantProfileResponse}"/> with the updated profile (including the new photo URL),
    /// or a <c>NotFoundError</c> if the merchant profile does not exist.
    /// </returns>
    /// <remarks>
    /// Images are stored under the <c>merchants/{merchantProfileId}</c> folder in the configured MinIO bucket.
    /// Uploading a new photo overwrites the <c>PhotoUrl</c> field but does not delete the previously stored object.
    /// </remarks>
    public async Task<Result<MerchantProfileResponse>> UploadPhotoAsync(
        int merchantProfileId, IFormFile file, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdWithCategoriesAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        var photoUrl = await imageStorage.UploadAsync(file, $"merchants/{merchantProfileId}", ct);

        merchant.PhotoUrl  = photoUrl;
        merchant.UpdatedAt = DateTime.UtcNow;
        merchantProfiles.Update(merchant);
        await merchantProfiles.SaveChangesAsync(ct);

        return Result.Ok(MapMerchantProfile(merchant));
    }

    // ─── Authenticated merchant — analytics & reports ────────────────────────

    /// <summary>
    /// Computes filterable analytics for the merchant over the requested range, comparing
    /// earnings against the immediately-preceding equal-length period for a growth figure.
    /// All money metrics consider non-cancelled orders (Paid + PickedUp) as realised revenue.
    /// </summary>
    public async Task<Result<MerchantAnalyticsResponse>> GetAnalyticsAsync(
        int merchantProfileId, DashboardQuery query, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        var allOrders  = (await orderRepository.GetByMerchantIdAsync(merchantProfileId, ct)).ToList();
        var allReviews = (await reviews.GetByMerchantIdAsync(merchantProfileId, ct)).ToList();

        var (fromUtc, toUtc) = ResolveRange(query.From, query.To);
        var rangeLength      = toUtc - fromUtc;
        var prevFrom         = fromUtc.Add(-rangeLength);

        var inRange  = allOrders.Where(o => o.CreatedAt >= fromUtc && o.CreatedAt <= toUtc).ToList();
        var revenue  = inRange.Where(o => o.OrderStatus != OrderStatus.Cancelled).ToList();
        var prevRev  = allOrders
            .Where(o => o.CreatedAt >= prevFrom && o.CreatedAt < fromUtc && o.OrderStatus != OrderStatus.Cancelled)
            .ToList();

        var earnings     = revenue.Sum(o => o.MerchantEarnings);
        var prevEarnings = prevRev.Sum(o => o.MerchantEarnings);
        var gmv          = revenue.Sum(o => o.TotalAmount);
        var completed    = inRange.Count(o => o.OrderStatus == OrderStatus.PickedUp);
        var cancelled    = inRange.Count(o => o.OrderStatus == OrderStatus.Cancelled);

        var topProducts = revenue
            .SelectMany(o => o.OrderDetails)
            .GroupBy(od => new { od.ProductId, od.Product.Name })
            .Select(g => new TopProductDto(
                g.Key.ProductId, g.Key.Name, g.Sum(od => od.Quantity), g.Sum(od => od.UnitPrice * od.Quantity)))
            .OrderByDescending(p => p.UnitsSold)
            .Take(5)
            .ToList();

        var ratingDistribution = Enumerable.Range(1, 5)
            .Select(stars => new RatingBucketDto(stars, allReviews.Count(r => r.Rating == stars)))
            .ToList();

        return Result.Ok(new MerchantAnalyticsResponse
        {
            RangeLabel        = $"{fromUtc:dd/MM/yyyy} — {toUtc:dd/MM/yyyy}",
            Gmv               = gmv,
            Earnings          = earnings,
            AverageOrderValue = revenue.Count > 0 ? Math.Round(gmv / revenue.Count, 0) : 0,
            PreviousEarnings  = prevEarnings,
            // null when the previous period has no earnings to compare against — a flat
            // "+100%" in that case would be meaningless (true regardless of scale), so the
            // frontend shows a "Nuevo" badge instead of a fabricated percentage.
            EarningsGrowthPct = prevEarnings > 0
                                    ? Math.Round((earnings - prevEarnings) / prevEarnings * 100, 1)
                                    : null,
            CompletedOrders   = completed,
            CancelledOrders   = cancelled,
            CancellationRate  = inRange.Count > 0 ? Math.Round((decimal)cancelled / inRange.Count * 100, 1) : 0,
            PacksSold         = revenue.SelectMany(o => o.OrderDetails).Sum(od => od.Quantity),
            AverageRating     = allReviews.Count > 0 ? Math.Round((decimal)allReviews.Average(r => r.Rating), 1) : 0,
            ReviewCount       = allReviews.Count,
            RatingDistribution = ratingDistribution,
            TopProducts       = topProducts,
            ActivitySeries    = BuildMerchantSeries(revenue, fromUtc, toUtc, query.Granularity)
        });
    }

    /// <summary>
    /// Builds an exportable sales report for the merchant: KPI summary plus a per-order table
    /// of the orders within the requested range, rendered to PDF or Excel.
    /// </summary>
    public async Task<Result<ReportFile>> GenerateSalesReportAsync(
        int merchantProfileId, ReportQuery query, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        var (fromUtc, toUtc) = ResolveRange(query.From, query.To);

        var inRange = (await orderRepository.GetByMerchantIdAsync(merchantProfileId, ct))
            .Where(o => o.CreatedAt >= fromUtc && o.CreatedAt <= toUtc)
            .ToList();
        var revenue = inRange.Where(o => o.OrderStatus != OrderStatus.Cancelled).ToList();

        var model = new ReportModel
        {
            Title    = $"Reporte de ventas — {merchant.BusinessName}",
            Subtitle = $"Período: {fromUtc:dd/MM/yyyy} — {toUtc:dd/MM/yyyy}",
            Kpis =
            [
                new ReportKpi("Órdenes completadas", inRange.Count(o => o.OrderStatus == OrderStatus.PickedUp).ToString("N0", Es)),
                new ReportKpi("Ventas brutas (GMV)", FormatCurrency(revenue.Sum(o => o.TotalAmount))),
                new ReportKpi("Ganancias netas",     FormatCurrency(revenue.Sum(o => o.MerchantEarnings)))
            ],
            Columns =
            [
                new ReportColumn { Header = "Fecha",    Type = ReportColumnType.Date,     Width = 2 },
                new ReportColumn { Header = "Cliente",  Type = ReportColumnType.Text,     Width = 3 },
                new ReportColumn { Header = "Estado",   Type = ReportColumnType.Text,     Width = 2 },
                new ReportColumn { Header = "Total",    Type = ReportColumnType.Currency, Width = 2 },
                new ReportColumn { Header = "Ganancia", Type = ReportColumnType.Currency, Width = 2 }
            ],
            Rows = inRange
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new object?[]
                {
                    o.CreatedAt,
                    o.Consumer is not null ? $"{o.Consumer.FirstName} {o.Consumer.LastName}".Trim() : "—",
                    TranslateStatus(o.OrderStatus),
                    o.TotalAmount,
                    o.MerchantEarnings
                })
                .ToList(),
            TotalsRow = ["Total", null, null, revenue.Sum(o => o.TotalAmount), revenue.Sum(o => o.MerchantEarnings)]
        };

        var file = reportFactory.Create(model, query.Format,
            $"resq-ventas_{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}");
        return Result.Ok(file);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    // "N0" alone silently truncates real cents to whole pesos (e.g. a $0.10 platform fee on
    // a small order displays as "$0", which reads as "no commission at all" rather than
    // "a fraction of a peso"). Whole amounts still print without decimals; only a value with
    // an actual fractional part gets them.
    private static string FormatCurrency(decimal value) =>
        "$" + value.ToString(value == Math.Truncate(value) ? "N0" : "N2", Es);

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

    /// <summary>Buckets non-cancelled orders into an ordered earnings/orders series by granularity.</summary>
    private static List<DailySalesDto> BuildMerchantSeries(
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
            default:
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

            return new DailySalesDto(label, inBucket.Count, inBucket.Sum(o => o.MerchantEarnings));
        }).ToList();
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    private static string TranslateStatus(OrderStatus status) => status switch
    {
        OrderStatus.Pending   => "Pendiente",
        OrderStatus.Paid      => "Pagado",
        OrderStatus.PickedUp  => "Retirado",
        OrderStatus.Cancelled => "Cancelado",
        _                     => status.ToString()
    };

    /// <summary>
    /// Maps a <see cref="Models.Catalog.Product"/> entity to a <see cref="ProductResponse"/> DTO.
    /// </summary>
    /// <param name="p">The product entity to map.</param>
    /// <returns>A populated <see cref="ProductResponse"/> DTO.</returns>
    private ProductResponse MapProduct(Models.Catalog.Product p) => new()
    {
        Id              = p.Id,
        Name            = p.Name,
        Description     = p.Description,
        ImageUrl        = imageStorage.ResolvePublicUrl(p.ImageUrl),
        ProductType     = p.ProductType.ToString(),
        OriginalPrice   = p.OriginalPrice,
        SalePrice       = p.SalePrice,
        StockQuantity   = p.StockQuantity,
        PickupTimeStart = p.PickupTimeStart,
        PickupTimeEnd   = p.PickupTimeEnd,
        IsActive        = p.IsActive
    };

    /// <summary>
    /// Maps a <see cref="Models.Auth.MerchantProfile"/> entity to a <see cref="MerchantProfileResponse"/> DTO,
    /// including the merchant's associated categories.
    /// </summary>
    /// <param name="m">The merchant profile entity with <c>MerchantCategories</c> and their <c>Category</c> navigation properties loaded.</param>
    /// <returns>A populated <see cref="MerchantProfileResponse"/> DTO.</returns>
    private MerchantProfileResponse MapMerchantProfile(Models.Auth.MerchantProfile m) => new()
    {
        Id                 = m.Id,
        BusinessName       = m.BusinessName,
        Cuit               = m.Cuit,
        Address            = m.Address,
        Latitude           = m.Latitude,
        Longitude          = m.Longitude,
        ContactPhone       = m.ContactPhone,
        PhotoUrl           = imageStorage.ResolvePublicUrl(m.PhotoUrl),
        MpConnectionStatus = m.MpConnectionStatus.ToString(),
        Categories         = m.MerchantCategories
                               .Select(mc => new CategoryResponse { Id = mc.CategoryId, Name = mc.Category.Name })
                               .ToList()
    };
}
