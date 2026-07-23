using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;

namespace ResQ.Tests.Repositories;

// NOTE: TryInsertAsync's duplicate-notification branch (catching a PostgreSQL 23505 unique-violation
// and returning false) cannot be exercised against EF Core's InMemory provider: InMemory enforces the
// unique index configured on MpNotificationId, but raises its own exception type rather than
// Npgsql.PostgresException, so the repository's targeted catch clause never triggers here. That
// idempotency contract (duplicate -> no-op) is covered instead at the service layer in
// MpWebhookIngestionServiceTests, where TryInsertAsync is mocked to return false directly.
public class MpWebhookEventRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly MpWebhookEventRepository _sut;

    public MpWebhookEventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new MpWebhookEventRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static MpWebhookEvent BuildEvent(long notificationId = 1001) => new()
    {
        MpNotificationId = notificationId,
        Topic            = "payment",
        MpResourceId     = notificationId,
        RawPayload       = "{}",
        ProcessingStatus = WebhookProcessingStatus.Pending,
        CreatedAt        = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // TryInsertAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TryInsertAsync_WhenNewNotification_InsertsAndReturnsTrue()
    {
        // Arrange
        var ev = BuildEvent();

        // Act
        var inserted = await _sut.TryInsertAsync(ev);

        // Assert
        Assert.True(inserted);
        var saved = await _db.MpWebhookEvents.FirstOrDefaultAsync(e => e.MpNotificationId == 1001);
        Assert.NotNull(saved);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByNotificationIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByNotificationIdAsync_WhenEventExists_ReturnsEvent()
    {
        // Arrange
        var ev = BuildEvent(2002);
        await _sut.TryInsertAsync(ev);

        // Act
        var result = await _sut.GetByNotificationIdAsync(2002);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2002, result.MpNotificationId);
    }

    [Fact]
    public async Task GetByNotificationIdAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByNotificationIdAsync(9999);

        // Assert
        Assert.Null(result);
    }
}
