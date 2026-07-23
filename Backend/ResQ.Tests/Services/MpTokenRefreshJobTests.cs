using FluentResults;
using Moq;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Services.MercadoPago;

namespace ResQ.Tests.Services;

public class MpTokenRefreshJobTests
{
    private readonly Mock<IMerchantMpCredentialRepository> _credentialRepo = new();
    private readonly Mock<IMercadoPagoOAuthService> _oauthService = new();

    private MpTokenRefreshJob CreateSut() => new(_credentialRepo.Object, _oauthService.Object);

    // ═══════════════════════════════════════════════════════════════════════════
    // ExecuteAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExecuteAsync_WhenNoCredentialsExpiring_DoesNotSaveChangesOrRefreshAnything()
    {
        // Arrange
        _credentialRepo.Setup(c => c.GetExpiringSoonAsync(7, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<MerchantMpCredential>());

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync();

        // Assert
        _oauthService.Verify(o => o.RefreshTokensAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepo.Verify(c => c.AddRefreshLogAsync(It.IsAny<MpTokenRefreshLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepo.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCredentialsExpiring_RefreshesEachAndLogsSuccess()
    {
        // Arrange
        var credentials = new List<MerchantMpCredential>
        {
            new() { Id = 1, MerchantId = 10, IsActive = true },
            new() { Id = 2, MerchantId = 20, IsActive = true }
        };
        _credentialRepo.Setup(c => c.GetExpiringSoonAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(credentials);
        _oauthService.Setup(o => o.RefreshTokensAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok());
        _oauthService.Setup(o => o.RefreshTokensAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok());
        _credentialRepo.Setup(c => c.AddRefreshLogAsync(It.IsAny<MpTokenRefreshLog>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _credentialRepo.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync();

        // Assert
        _oauthService.Verify(o => o.RefreshTokensAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _oauthService.Verify(o => o.RefreshTokensAsync(20, It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(c => c.AddRefreshLogAsync(
            It.Is<MpTokenRefreshLog>(l => l.MerchantId == 10 && l.Success && l.ErrorMessage == null),
            It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(c => c.AddRefreshLogAsync(
            It.Is<MpTokenRefreshLog>(l => l.MerchantId == 20 && l.Success),
            It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshFails_LogsFailureWithErrorMessage()
    {
        // Arrange
        var credentials = new List<MerchantMpCredential> { new() { Id = 1, MerchantId = 10, IsActive = true } };
        _credentialRepo.Setup(c => c.GetExpiringSoonAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(credentials);
        _oauthService.Setup(o => o.RefreshTokensAsync(10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail("MP rejected refresh"));
        _credentialRepo.Setup(c => c.AddRefreshLogAsync(It.IsAny<MpTokenRefreshLog>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _credentialRepo.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync();

        // Assert
        _credentialRepo.Verify(c => c.AddRefreshLogAsync(
            It.Is<MpTokenRefreshLog>(l => l.MerchantId == 10 && !l.Success && l.ErrorMessage == "MP rejected refresh"),
            It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepo.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
