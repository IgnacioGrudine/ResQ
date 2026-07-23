using Microsoft.AspNetCore.Mvc;
using Moq;
using ResQ.API.Controllers;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Services.MercadoPago;

namespace ResQ.Tests.Controllers;

public class MpWebhookControllerTests
{
    private readonly Mock<IMpWebhookIngestionService> _ingestionService = new();

    private MpWebhookController CreateSut() => new(_ingestionService.Object);

    private static MpWebhookPayload BuildPayload() => new()
    {
        Type = "payment",
        Data = new MpWebhookData { Id = "123" }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // HandleWebhook
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleWebhook_AlwaysReturnsOk()
    {
        // Arrange
        var payload = BuildPayload();
        var sut = CreateSut();

        // Act
        var actionResult = await sut.HandleWebhook(payload, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(actionResult);
    }

    [Fact]
    public async Task HandleWebhook_DelegatesIngestionToService()
    {
        // Arrange
        var payload = BuildPayload();
        var sut = CreateSut();

        // Act
        await sut.HandleWebhook(payload, CancellationToken.None);

        // Assert
        _ingestionService.Verify(s => s.IngestAsync(payload, It.IsAny<CancellationToken>()), Times.Once);
    }
}
