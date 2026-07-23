using Moq;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Notifications;
using ResQ.API.Repositories.Notifications;
using ResQ.API.Services.Notifications;

namespace ResQ.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<IMerchantNotificationRepository> _notifications = new();

    private NotificationService CreateSut() => new(_notifications.Object);

    private static MerchantNotification BuildNotification(
        int id = 1,
        int merchantId = 10,
        NotificationType type = NotificationType.OrderPaid,
        bool isRead = false,
        int? orderId = null) => new()
    {
        Id         = id,
        MerchantId = merchantId,
        Type       = type,
        Title      = "Nuevo pedido",
        Message    = "Tenés un nuevo pedido pago.",
        IsRead     = isRead,
        OrderId    = orderId,
        CreatedAt  = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_PersistsNotificationWithCorrectProperties()
    {
        // Arrange
        _notifications.Setup(n => n.AddAsync(It.IsAny<MerchantNotification>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        _notifications.Setup(n => n.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.CreateAsync(10, NotificationType.OrderPaid, "Nuevo pedido", "Tenés un nuevo pedido pago.", orderId: 55);

        // Assert
        _notifications.Verify(n => n.AddAsync(
            It.Is<MerchantNotification>(m =>
                m.MerchantId == 10 &&
                m.Type == NotificationType.OrderPaid &&
                m.Title == "Nuevo pedido" &&
                m.Message == "Tenés un nuevo pedido pago." &&
                m.OrderId == 55 &&
                m.IsRead == false),
            It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(n => n.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenOrderIdOmitted_PersistsNotificationWithNullOrderId()
    {
        // Arrange
        _notifications.Setup(n => n.AddAsync(It.IsAny<MerchantNotification>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        _notifications.Setup(n => n.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.CreateAsync(10, NotificationType.OrderCancelled, "Pedido cancelado", "El pago fue rechazado.");

        // Assert
        _notifications.Verify(n => n.AddAsync(
            It.Is<MerchantNotification>(m => m.OrderId == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetForMerchantAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetForMerchantAsync_ReturnsNotificationsMappedToResponse()
    {
        // Arrange
        var stored = new List<MerchantNotification>
        {
            BuildNotification(1, 10, NotificationType.OrderPaid, isRead: false, orderId: 5),
            BuildNotification(2, 10, NotificationType.OrderCancelled, isRead: true)
        };
        _notifications.Setup(n => n.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(stored);

        var sut = CreateSut();

        // Act
        var result = await sut.GetForMerchantAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("OrderPaid", items[0].Type);
        Assert.Equal(5, items[0].OrderId);
        Assert.True(items[1].IsRead);
    }

    [Fact]
    public async Task GetForMerchantAsync_WhenNoNotifications_ReturnsEmptyCollection()
    {
        // Arrange
        _notifications.Setup(n => n.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
                      .ReturnsAsync([]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetForMerchantAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUnreadCountAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCountFromRepository()
    {
        // Arrange
        _notifications.Setup(n => n.GetUnreadCountAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var sut = CreateSut();

        // Act
        var result = await sut.GetUnreadCountAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MarkAsReadAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _notifications.Setup(n => n.GetByIdForMerchantAsync(10, 99, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((MerchantNotification?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.MarkAsReadAsync(10, 99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrada", result.Errors[0].Message);
        _notifications.Verify(n => n.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationUnread_MarksAsReadAndSaves()
    {
        // Arrange
        var notification = BuildNotification(1, 10, isRead: false);
        _notifications.Setup(n => n.GetByIdForMerchantAsync(10, 1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(notification);
        _notifications.Setup(n => n.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.MarkAsReadAsync(10, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(notification.IsRead);
        Assert.NotNull(notification.UpdatedAt);
        _notifications.Verify(n => n.Update(notification), Times.Once);
        _notifications.Verify(n => n.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationAlreadyRead_DoesNotUpdateOrSave()
    {
        // Arrange
        var notification = BuildNotification(1, 10, isRead: true);
        _notifications.Setup(n => n.GetByIdForMerchantAsync(10, 1, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(notification);

        var sut = CreateSut();

        // Act
        var result = await sut.MarkAsReadAsync(10, 1);

        // Assert
        Assert.True(result.IsSuccess);
        _notifications.Verify(n => n.Update(It.IsAny<MerchantNotification>()), Times.Never);
        _notifications.Verify(n => n.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MarkAllAsReadAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MarkAllAsReadAsync_CallsRepositoryAndReturnsOk()
    {
        // Arrange
        _notifications.Setup(n => n.MarkAllAsReadAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(4);

        var sut = CreateSut();

        // Act
        var result = await sut.MarkAllAsReadAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        _notifications.Verify(n => n.MarkAllAsReadAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
