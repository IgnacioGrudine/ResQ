using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Services.MercadoPago;

namespace ResQ.Tests.Services;

public class MpWebhookIngestionServiceTests
{
    private readonly Mock<IMpWebhookEventRepository> _webhookEventRepo = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobs = new();

    private MpWebhookIngestionService CreateSut() => new(_webhookEventRepo.Object, _backgroundJobs.Object);

    private static MpWebhookPayload BuildPaymentPayload(string? id = "999") => new()
    {
        Type = "payment",
        Data = new MpWebhookData { Id = id }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // IngestAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task IngestAsync_WhenPayloadIsNull_DoesNothing()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.IngestAsync(null!, CancellationToken.None);

        // Assert
        _webhookEventRepo.Verify(r => r.TryInsertAsync(It.IsAny<MpWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WhenTypeIsNotPayment_DoesNothing()
    {
        // Arrange
        var payload = new MpWebhookPayload { Type = "merchant_order", Data = new MpWebhookData { Id = "1" } };
        var sut = CreateSut();

        // Act
        await sut.IngestAsync(payload, CancellationToken.None);

        // Assert
        _webhookEventRepo.Verify(r => r.TryInsertAsync(It.IsAny<MpWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WhenDataIdIsNull_DoesNothing()
    {
        // Arrange
        var payload = BuildPaymentPayload(id: null);
        var sut = CreateSut();

        // Act
        await sut.IngestAsync(payload, CancellationToken.None);

        // Assert
        _webhookEventRepo.Verify(r => r.TryInsertAsync(It.IsAny<MpWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WhenDataIdIsNotNumeric_DoesNothing()
    {
        // Arrange
        var payload = BuildPaymentPayload("not-a-number");
        var sut = CreateSut();

        // Act
        await sut.IngestAsync(payload, CancellationToken.None);

        // Assert
        _webhookEventRepo.Verify(r => r.TryInsertAsync(It.IsAny<MpWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WhenDuplicateNotification_DoesNotEnqueueProcessingJob()
    {
        // Arrange
        var payload = BuildPaymentPayload("999");
        _webhookEventRepo.Setup(r => r.TryInsertAsync(It.IsAny<MpWebhookEvent>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(false);

        var sut = CreateSut();

        // Act
        await sut.IngestAsync(payload, CancellationToken.None);

        // Assert
        _backgroundJobs.Verify(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WhenNewNotification_PersistsPendingEventAndEnqueuesProcessingJob()
    {
        // Arrange
        var payload = BuildPaymentPayload("999");
        MpWebhookEvent? captured = null;
        _webhookEventRepo.Setup(r => r.TryInsertAsync(It.IsAny<MpWebhookEvent>(), It.IsAny<CancellationToken>()))
                          .Callback<MpWebhookEvent, CancellationToken>((ev, _) => captured = ev)
                          .ReturnsAsync(true);
        _backgroundJobs.Setup(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-1");

        var sut = CreateSut();

        // Act
        await sut.IngestAsync(payload, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(999, captured!.MpNotificationId);
        Assert.Equal(999, captured.MpResourceId);
        Assert.Equal(WebhookProcessingStatus.Pending, captured.ProcessingStatus);
        _backgroundJobs.Verify(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }
}
