using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.Notifications;
using ResQ.API.Services.Notifications;

namespace ResQ.Tests.Controllers;

public class NotificationsControllerTests
{
    private const int MerchantProfileId = 42;

    private readonly Mock<INotificationService> _notificationService = new();

    private NotificationsController CreateSut()
    {
        var claims = new[]
        {
            new Claim("profileId", MerchantProfileId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, "100")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return new NotificationsController(_notificationService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static NotificationResponse BuildNotificationResponse(int id = 1, bool isRead = false) => new()
    {
        Id        = id,
        Type      = "OrderPaid",
        Title     = "Nuevo pedido",
        Message   = "Tenés un nuevo pedido pago.",
        IsRead    = isRead,
        OrderId   = 5,
        CreatedAt = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyNotifications
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyNotifications_WhenServiceReturnsOk_Returns200WithNotifications()
    {
        // Arrange
        var notifications = new List<NotificationResponse> { BuildNotificationResponse(1), BuildNotificationResponse(2) };
        _notificationService.Setup(s => s.GetForMerchantAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Result.Ok<IEnumerable<NotificationResponse>>(notifications));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyNotifications(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<NotificationResponse>>(result.Value).ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetMyNotifications_PassesMerchantProfileIdFromClaims()
    {
        // Arrange
        _notificationService.Setup(s => s.GetForMerchantAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Result.Ok<IEnumerable<NotificationResponse>>([]));

        var sut = CreateSut();

        // Act
        await sut.GetMyNotifications(CancellationToken.None);

        // Assert
        _notificationService.Verify(s => s.GetForMerchantAsync(MerchantProfileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUnreadCount
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUnreadCount_WhenServiceReturnsOk_Returns200WithCount()
    {
        // Arrange
        _notificationService.Setup(s => s.GetUnreadCountAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Result.Ok(7));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetUnreadCount(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(7, result.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MarkAsRead
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MarkAsRead_WhenNotificationNotFound_Returns404()
    {
        // Arrange
        _notificationService.Setup(s => s.MarkAsReadAsync(MerchantProfileId, 99, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Result.Fail(new NotFoundError("Notificación no encontrada.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.MarkAsRead(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsOk_Returns204NoContent()
    {
        // Arrange
        _notificationService.Setup(s => s.MarkAsReadAsync(MerchantProfileId, 1, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.MarkAsRead(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MarkAllAsRead
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MarkAllAsRead_WhenServiceReturnsOk_Returns204NoContent()
    {
        // Arrange
        _notificationService.Setup(s => s.MarkAllAsReadAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.MarkAllAsRead(CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
        _notificationService.Verify(s => s.MarkAllAsReadAsync(MerchantProfileId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
