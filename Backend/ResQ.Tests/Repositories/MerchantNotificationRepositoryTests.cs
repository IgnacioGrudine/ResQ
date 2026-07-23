using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Notifications;
using ResQ.API.Repositories.Notifications;

namespace ResQ.Tests.Repositories;

public class MerchantNotificationRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly MerchantNotificationRepository _sut;

    public MerchantNotificationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new MerchantNotificationRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<MerchantProfile> SeedMerchantAsync(string name = "La Panadería")
    {
        var merchant = new MerchantProfile
        {
            BusinessName = name,
            Cuit         = "30-12345678-9",
            Address      = "Av. 123",
            ContactPhone = "351-000000",
            CreatedAt    = DateTime.UtcNow
        };
        _db.MerchantProfiles.Add(merchant);
        await _db.SaveChangesAsync();
        return merchant;
    }

    private async Task<MerchantNotification> SeedNotificationAsync(
        int merchantId,
        bool isRead = false,
        NotificationType type = NotificationType.OrderPaid,
        DateTime? createdAt = null)
    {
        var notification = new MerchantNotification
        {
            MerchantId = merchantId,
            Type       = type,
            Title      = "Nuevo pedido",
            Message    = "Tenés un nuevo pedido pago.",
            IsRead     = isRead,
            CreatedAt  = createdAt ?? DateTime.UtcNow
        };
        _db.MerchantNotifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByMerchantIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByMerchantIdAsync_ReturnsOnlyNotificationsForGivenMerchant()
    {
        // Arrange
        var m1 = await SeedMerchantAsync("Panadería A");
        var m2 = await SeedMerchantAsync("Panadería B");
        await SeedNotificationAsync(m1.Id);
        await SeedNotificationAsync(m1.Id);
        await SeedNotificationAsync(m2.Id);

        // Act
        var result = (await _sut.GetByMerchantIdAsync(m1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, n => Assert.Equal(m1.Id, n.MerchantId));
    }

    [Fact]
    public async Task GetByMerchantIdAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var now = DateTime.UtcNow;
        await SeedNotificationAsync(merchant.Id, createdAt: now.AddHours(-2));
        var newest = await SeedNotificationAsync(merchant.Id, createdAt: now);

        // Act
        var result = (await _sut.GetByMerchantIdAsync(merchant.Id)).ToList();

        // Assert
        Assert.Equal(newest.Id, result[0].Id);
    }

    [Fact]
    public async Task GetByMerchantIdAsync_WhenMerchantHasNoNotifications_ReturnsEmptyList()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.GetByMerchantIdAsync(merchant.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByMerchantIdAsync_CapsResultsAtMaxNotifications()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        for (var i = 0; i < 55; i++)
            await SeedNotificationAsync(merchant.Id, createdAt: DateTime.UtcNow.AddMinutes(-i));

        // Act
        var result = await _sut.GetByMerchantIdAsync(merchant.Id);

        // Assert
        Assert.Equal(50, result.Count());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUnreadCountAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCountOfUnreadNotificationsOnly()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        await SeedNotificationAsync(merchant.Id, isRead: false);
        await SeedNotificationAsync(merchant.Id, isRead: false);
        await SeedNotificationAsync(merchant.Id, isRead: true);

        // Act
        var result = await _sut.GetUnreadCountAsync(merchant.Id);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WhenAllRead_ReturnsZero()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        await SeedNotificationAsync(merchant.Id, isRead: true);

        // Act
        var result = await _sut.GetUnreadCountAsync(merchant.Id);

        // Assert
        Assert.Equal(0, result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdForMerchantAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdForMerchantAsync_WhenNotificationBelongsToMerchant_ReturnsNotification()
    {
        // Arrange
        var merchant     = await SeedMerchantAsync();
        var notification = await SeedNotificationAsync(merchant.Id);

        // Act
        var result = await _sut.GetByIdForMerchantAsync(merchant.Id, notification.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(notification.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdForMerchantAsync_WhenNotificationBelongsToDifferentMerchant_ReturnsNull()
    {
        // Arrange
        var m1           = await SeedMerchantAsync("A");
        var m2           = await SeedMerchantAsync("B");
        var notification = await SeedNotificationAsync(m1.Id);

        // Act
        var result = await _sut.GetByIdForMerchantAsync(m2.Id, notification.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdForMerchantAsync_WhenNotificationNotFound_ReturnsNull()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.GetByIdForMerchantAsync(merchant.Id, 9999);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MarkAllAsReadAsync
    // The production implementation uses EF Core's ExecuteUpdateAsync (bulk SQL UPDATE,
    // no entity materialization). The InMemory provider does not support ExecuteUpdate/
    // ExecuteUpdateAsync at all — it throws InvalidOperationException regardless of the
    // query shape — so this method cannot be exercised via EF Core InMemory. It needs an
    // integration test against a real relational provider (Postgres) instead.
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact(Skip = "EF Core InMemory provider does not support ExecuteUpdateAsync; needs a real relational provider (Postgres) to exercise this method.")]
    public async Task MarkAllAsReadAsync_MarksAllUnreadNotificationsForMerchantAsRead()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var n1 = await SeedNotificationAsync(merchant.Id, isRead: false);
        var n2 = await SeedNotificationAsync(merchant.Id, isRead: false);

        // Act
        var updatedCount = await _sut.MarkAllAsReadAsync(merchant.Id);

        // Assert
        Assert.Equal(2, updatedCount);
        var reloaded1 = await _db.MerchantNotifications.FindAsync(n1.Id);
        var reloaded2 = await _db.MerchantNotifications.FindAsync(n2.Id);
        Assert.True(reloaded1!.IsRead);
        Assert.True(reloaded2!.IsRead);
        Assert.NotNull(reloaded1.UpdatedAt);
    }

    [Fact(Skip = "EF Core InMemory provider does not support ExecuteUpdateAsync; needs a real relational provider (Postgres) to exercise this method.")]
    public async Task MarkAllAsReadAsync_DoesNotAffectOtherMerchantsNotifications()
    {
        // Arrange
        var m1 = await SeedMerchantAsync("A");
        var m2 = await SeedMerchantAsync("B");
        var otherMerchantNotification = await SeedNotificationAsync(m2.Id, isRead: false);
        await SeedNotificationAsync(m1.Id, isRead: false);

        // Act
        await _sut.MarkAllAsReadAsync(m1.Id);

        // Assert
        var reloaded = await _db.MerchantNotifications.FindAsync(otherMerchantNotification.Id);
        Assert.False(reloaded!.IsRead);
    }

    [Fact(Skip = "EF Core InMemory provider does not support ExecuteUpdateAsync; needs a real relational provider (Postgres) to exercise this method.")]
    public async Task MarkAllAsReadAsync_WhenNoUnreadNotifications_ReturnsZero()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        await SeedNotificationAsync(merchant.Id, isRead: true);

        // Act
        var updatedCount = await _sut.MarkAllAsReadAsync(merchant.Id);

        // Assert
        Assert.Equal(0, updatedCount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenericRepository — AddAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsNotification()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var notification = new MerchantNotification
        {
            MerchantId = merchant.Id,
            Type       = NotificationType.OrderCancelled,
            Title      = "Pedido cancelado",
            Message    = "El pago fue rechazado.",
            IsRead     = false,
            CreatedAt  = DateTime.UtcNow
        };

        // Act
        await _sut.AddAsync(notification);
        await _sut.SaveChangesAsync();

        // Assert
        var saved = await _db.MerchantNotifications.FindAsync(notification.Id);
        Assert.NotNull(saved);
        Assert.Equal("Pedido cancelado", saved.Title);
    }
}
