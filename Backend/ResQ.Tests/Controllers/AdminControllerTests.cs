using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.Admin;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Shared;
using ResQ.API.Services.Admin;
using ResQ.API.Services.Catalog;
using ResQ.API.Services.Reporting;

namespace ResQ.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IAdminService> _adminService = new();
    private readonly Mock<ICatalogService> _catalogService = new();

    private AdminController CreateSut() => new(_adminService.Object, _catalogService.Object);

    // ═══════════════════════════════════════════════════════════════════════════
    // GetDashboard
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDashboard_WhenServiceReturnsOk_Returns200WithDashboard()
    {
        // Arrange
        var response = new AdminDashboardResponse { RangeLabel = "01/01/2026 — 31/01/2026" };
        _adminService.Setup(s => s.GetDashboardAsync(It.IsAny<DashboardQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetDashboard(new DashboardQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<AdminDashboardResponse>(result.Value);
        Assert.Equal("01/01/2026 — 31/01/2026", returned.RangeLabel);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchants
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchants_WhenServiceReturnsOk_Returns200WithPagedMerchants()
    {
        // Arrange
        var paged = new PagedResponse<AdminMerchantListItemResponse>
        {
            Items = [new AdminMerchantListItemResponse { Id = 1, BusinessName = "Panadería A" }],
            Page = 1, PageSize = 10, TotalItems = 1
        };
        _adminService.Setup(s => s.GetMerchantsAsync(It.IsAny<MerchantListQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(paged));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMerchants(new MerchantListQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<PagedResponse<AdminMerchantListItemResponse>>(result.Value);
        Assert.Single(returned.Items);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantDetail
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantDetail_WhenMerchantNotFound_Returns404()
    {
        // Arrange
        _adminService.Setup(s => s.GetMerchantDetailAsync(99, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new NotFoundError("Comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMerchantDetail(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetMerchantDetail_WhenServiceReturnsOk_Returns200WithMerchant()
    {
        // Arrange
        var response = new AdminMerchantDetailResponse { Id = 1, BusinessName = "Panadería A" };
        _adminService.Setup(s => s.GetMerchantDetailAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMerchantDetail(1, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<AdminMerchantDetailResponse>(result.Value);
        Assert.Equal("Panadería A", returned.BusinessName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SetMerchantStatus
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SetMerchantStatus_WhenMerchantNotFound_Returns404()
    {
        // Arrange
        _adminService.Setup(s => s.SetMerchantStatusAsync(99, true, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new NotFoundError("Comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.SetMerchantStatus(99, new UpdateStatusRequest { IsActive = true }, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task SetMerchantStatus_WhenServiceReturnsOk_Returns204NoContent()
    {
        // Arrange
        _adminService.Setup(s => s.SetMerchantStatusAsync(1, false, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.SetMerchantStatus(1, new UpdateStatusRequest { IsActive = false }, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUsers
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUsers_WhenServiceReturnsOk_Returns200WithPagedUsers()
    {
        // Arrange
        var paged = new PagedResponse<AdminUserListItemResponse>
        {
            Items = [new AdminUserListItemResponse { Id = 1, Email = "ana@test.com" }],
            Page = 1, PageSize = 10, TotalItems = 1
        };
        _adminService.Setup(s => s.GetUsersAsync(It.IsAny<UserListQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(paged));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetUsers(new UserListQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<PagedResponse<AdminUserListItemResponse>>(result.Value);
        Assert.Single(returned.Items);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SetUserStatus
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SetUserStatus_WhenUserNotFound_Returns404()
    {
        // Arrange
        _adminService.Setup(s => s.SetUserStatusAsync(99, true, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new NotFoundError("Usuario no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.SetUserStatus(99, new UpdateStatusRequest { IsActive = true }, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task SetUserStatus_WhenTargetIsAdmin_Returns400()
    {
        // Arrange
        _adminService.Setup(s => s.SetUserStatusAsync(1, false, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new BadRequestError("No se puede desactivar una cuenta de administrador.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.SetUserStatus(1, new UpdateStatusRequest { IsActive = false }, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task SetUserStatus_WhenServiceReturnsOk_Returns204NoContent()
    {
        // Arrange
        _adminService.Setup(s => s.SetUserStatusAsync(1, false, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.SetUserStatus(1, new UpdateStatusRequest { IsActive = false }, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUserOrders
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUserOrders_WhenUserNotFound_Returns404()
    {
        // Arrange
        _adminService.Setup(s => s.GetUserOrdersAsync(99, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new NotFoundError("Usuario no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetUserOrders(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetUserOrders_WhenServiceReturnsOk_Returns200WithOrders()
    {
        // Arrange
        var orders = new List<OrderSummaryResponse> { new() { Id = 1, MerchantName = "Panadería A" } };
        _adminService.Setup(s => s.GetUserOrdersAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok<IEnumerable<OrderSummaryResponse>>(orders));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetUserOrders(1, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<OrderSummaryResponse>>(result.Value).ToList();
        Assert.Single(returned);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetGlobalReport
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetGlobalReport_WhenServiceReturnsOk_ReturnsFileResult()
    {
        // Arrange
        var file = new ReportFile([1, 2, 3], "application/pdf", "resq-reporte-global.pdf");
        _adminService.Setup(s => s.GenerateGlobalReportAsync(It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(file));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetGlobalReport(new ReportQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("resq-reporte-global.pdf", result.FileDownloadName);
        Assert.Equal(file.Content, result.FileContents);
    }

    [Fact]
    public async Task GetGlobalReport_WhenServiceFailsWithNotFoundError_Returns404()
    {
        // Arrange
        _adminService.Setup(s => s.GenerateGlobalReportAsync(It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail<ReportFile>(new NotFoundError("Comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetGlobalReport(new ReportQuery(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task GetGlobalReport_WhenServiceFailsWithOtherError_Returns400()
    {
        // Arrange
        _adminService.Setup(s => s.GenerateGlobalReportAsync(It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail<ReportFile>(new ValidationError("Formato inválido.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetGlobalReport(new ReportQuery(), CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantRankingReport
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantRankingReport_WhenServiceReturnsOk_ReturnsFileResult()
    {
        // Arrange
        var file = new ReportFile([4, 5], "application/xlsx", "resq-ranking-comercios.xlsx");
        _adminService.Setup(s => s.GenerateMerchantRankingReportAsync(It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(file));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMerchantRankingReport(new ReportQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("resq-ranking-comercios.xlsx", result.FileDownloadName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantReport
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantReport_WhenMerchantNotFound_Returns404()
    {
        // Arrange
        _adminService.Setup(s => s.GenerateMerchantReportAsync(99, It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail<ReportFile>(new NotFoundError("Comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMerchantReport(99, new ReportQuery(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task GetMerchantReport_WhenServiceReturnsOk_ReturnsFileResult()
    {
        // Arrange
        var file = new ReportFile([7], "application/pdf", "resq-comercio-1.pdf");
        _adminService.Setup(s => s.GenerateMerchantReportAsync(1, It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(file));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMerchantReport(1, new ReportQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("resq-comercio-1.pdf", result.FileDownloadName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateCategory
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateCategory_WhenServiceReturnsOk_Returns200WithCategory()
    {
        // Arrange
        var response = new CategoryResponse { Id = 1, Name = "Sushi" };
        _catalogService.Setup(s => s.CreateCategoryAsync("Sushi", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.CreateCategory(new CategoryRequest { Name = "Sushi" }, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<CategoryResponse>(result.Value);
        Assert.Equal("Sushi", returned.Name);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateCategory
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateCategory_WhenCategoryNotFound_Returns404()
    {
        // Arrange
        _catalogService.Setup(s => s.UpdateCategoryAsync(99, "Sushi", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Fail(new NotFoundError("Categoría no encontrada.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UpdateCategory(99, new CategoryRequest { Name = "Sushi" }, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task UpdateCategory_WhenServiceReturnsOk_Returns200WithCategory()
    {
        // Arrange
        var response = new CategoryResponse { Id = 1, Name = "Sushi renombrado" };
        _catalogService.Setup(s => s.UpdateCategoryAsync(1, "Sushi renombrado", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UpdateCategory(1, new CategoryRequest { Name = "Sushi renombrado" }, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<CategoryResponse>(result.Value);
        Assert.Equal("Sushi renombrado", returned.Name);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteCategory
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteCategory_WhenMerchantsStillAssigned_Returns409()
    {
        // Arrange
        _catalogService.Setup(s => s.DeleteCategoryAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Fail(new ConflictError("Hay comercios asignados a esta categoría.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.DeleteCategory(1, CancellationToken.None);

        // Assert
        Assert.IsType<ConflictObjectResult>(actionResult);
    }

    [Fact]
    public async Task DeleteCategory_WhenServiceReturnsOk_Returns204NoContent()
    {
        // Arrange
        _catalogService.Setup(s => s.DeleteCategoryAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.DeleteCategory(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }
}
