using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.Admin;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Reviews;
using ResQ.API.Services.Merchants;
using ResQ.API.Services.Reporting;

namespace ResQ.Tests.Controllers;

public class MerchantsControllerTests
{
    private const int MerchantProfileId = 42;

    private readonly Mock<IMerchantService> _merchantService = new();

    private MerchantsController CreateSut()
    {
        var claims = new[]
        {
            new Claim("profileId", MerchantProfileId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, "100")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return new MerchantsController(_merchantService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static MerchantProfileResponse BuildProfileResponse() => new()
    {
        Id                 = MerchantProfileId,
        BusinessName       = "Panadería Don José",
        Cuit               = "30-12345678-9",
        Address            = "Calle Falsa 123",
        ContactPhone       = "351-1234567",
        MpConnectionStatus = "Connected"
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyProfile
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyProfile_WhenServiceReturnsOk_Returns200WithProfile()
    {
        // Arrange
        _merchantService.Setup(s => s.GetMyProfileAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(BuildProfileResponse()));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyProfile(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<MerchantProfileResponse>(result.Value);
        Assert.Equal("Panadería Don José", returned.BusinessName);
    }

    [Fact]
    public async Task GetMyProfile_WhenNotFound_Returns404()
    {
        // Arrange
        _merchantService.Setup(s => s.GetMyProfileAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Perfil de comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyProfile(CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UploadPhoto
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UploadPhoto_WhenServiceReturnsOk_Returns200WithUpdatedPhotoUrl()
    {
        // Arrange
        var response = BuildProfileResponse();
        response.PhotoUrl = "https://cdn.resq.com/merchants/42/photo.jpg";
        var mockFile = new Mock<IFormFile>();
        _merchantService.Setup(s => s.UploadPhotoAsync(MerchantProfileId, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UploadPhoto(mockFile.Object, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<MerchantProfileResponse>(result.Value);
        Assert.Equal("https://cdn.resq.com/merchants/42/photo.jpg", returned.PhotoUrl);
    }

    [Fact]
    public async Task UploadPhoto_WhenNotFound_Returns404()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        _merchantService.Setup(s => s.UploadPhotoAsync(MerchantProfileId, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Perfil de comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UploadPhoto(mockFile.Object, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateMyProfile
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateMyProfile_WhenServiceReturnsOk_Returns200WithUpdatedProfile()
    {
        // Arrange
        var response = BuildProfileResponse();
        response.BusinessName = "Nuevo nombre";
        _merchantService.Setup(s => s.UpdateMyProfileAsync(MerchantProfileId, It.IsAny<UpdateMerchantProfileRequest>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UpdateMyProfile(new UpdateMerchantProfileRequest(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<MerchantProfileResponse>(result.Value);
        Assert.Equal("Nuevo nombre", returned.BusinessName);
    }

    [Fact]
    public async Task UpdateMyProfile_WhenNotFound_Returns404()
    {
        // Arrange
        _merchantService.Setup(s => s.UpdateMyProfileAsync(MerchantProfileId, It.IsAny<UpdateMerchantProfileRequest>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Perfil de comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UpdateMyProfile(new UpdateMerchantProfileRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetDashboard
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDashboard_WhenServiceReturnsOk_Returns200WithMetrics()
    {
        // Arrange
        var response = new MerchantDashboardResponse { BusinessName = "Panadería Don José", TotalSales = 5 };
        _merchantService.Setup(s => s.GetDashboardAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetDashboard(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<MerchantDashboardResponse>(result.Value);
        Assert.Equal(5, returned.TotalSales);
    }

    [Fact]
    public async Task GetDashboard_WhenNotFound_Returns404()
    {
        // Arrange
        _merchantService.Setup(s => s.GetDashboardAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Perfil de comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetDashboard(CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyOrders
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyOrders_WhenServiceReturnsOk_Returns200WithOrders()
    {
        // Arrange
        var orders = new List<MerchantOrderSummaryResponse> { new() { Id = 1 }, new() { Id = 2 } };
        _merchantService.Setup(s => s.GetMyOrdersAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok<IEnumerable<MerchantOrderSummaryResponse>>(orders));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyOrders(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<MerchantOrderSummaryResponse>>(result.Value).ToList();
        Assert.Equal(2, items.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ConfirmPickup
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConfirmPickup_WhenServiceReturnsOk_Returns200WithConfirmedOrder()
    {
        // Arrange
        var response = new MerchantOrderSummaryResponse { Id = 1, PickupCode = "ABC123", OrderStatus = "PickedUp" };
        _merchantService.Setup(s => s.ConfirmPickupAsync(MerchantProfileId, "ABC123", It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.ConfirmPickup(new ConfirmPickupRequest { PickupCode = "ABC123" }, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<MerchantOrderSummaryResponse>(result.Value);
        Assert.Equal("PickedUp", returned.OrderStatus);
    }

    [Fact]
    public async Task ConfirmPickup_WhenCodeInvalid_Returns404()
    {
        // Arrange
        _merchantService.Setup(s => s.ConfirmPickupAsync(MerchantProfileId, "BADCODE", It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Código de retiro inválido.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.ConfirmPickup(new ConfirmPickupRequest { PickupCode = "BADCODE" }, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyReviews
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyReviews_WhenServiceReturnsOk_Returns200WithReviews()
    {
        // Arrange
        var reviews = new List<ReviewResponse> { new() { Id = 1, Rating = 5 } };
        _merchantService.Setup(s => s.GetMyReviewsAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok<IEnumerable<ReviewResponse>>(reviews));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyReviews(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<ReviewResponse>>(result.Value).ToList();
        Assert.Single(items);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAnalytics
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAnalytics_WhenServiceReturnsOk_Returns200WithAnalytics()
    {
        // Arrange
        var response = new MerchantAnalyticsResponse { RangeLabel = "01/01/2026 — 10/01/2026" };
        _merchantService.Setup(s => s.GetAnalyticsAsync(MerchantProfileId, It.IsAny<DashboardQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetAnalytics(new DashboardQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<MerchantAnalyticsResponse>(result.Value);
        Assert.Equal("01/01/2026 — 10/01/2026", returned.RangeLabel);
    }

    [Fact]
    public async Task GetAnalytics_WhenNotFound_Returns404()
    {
        // Arrange
        _merchantService.Setup(s => s.GetAnalyticsAsync(MerchantProfileId, It.IsAny<DashboardQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Perfil de comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetAnalytics(new DashboardQuery(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetSalesReport
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSalesReport_WhenServiceReturnsOk_ReturnsFileDownload()
    {
        // Arrange
        var file = new ReportFile([1, 2, 3], "application/pdf", "resq-ventas.pdf");
        _merchantService.Setup(s => s.GenerateSalesReportAsync(MerchantProfileId, It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(file));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetSalesReport(new ReportQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("resq-ventas.pdf", result.FileDownloadName);
    }

    [Fact]
    public async Task GetSalesReport_WhenNotFound_Returns404ProblemDetails()
    {
        // Arrange
        _merchantService.Setup(s => s.GenerateSalesReportAsync(MerchantProfileId, It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Perfil de comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetSalesReport(new ReportQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<NotFoundObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task GetSalesReport_WhenOtherError_Returns400ProblemDetails()
    {
        // Arrange
        _merchantService.Setup(s => s.GenerateSalesReportAsync(MerchantProfileId, It.IsAny<ReportQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new ValidationError("Rango de fechas inválido.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetSalesReport(new ReportQuery(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(400, problem.Status);
    }
}
