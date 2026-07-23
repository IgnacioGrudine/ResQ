using System.Net;
using System.Text;
using FluentResults;
using Microsoft.Extensions.Options;
using Moq;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Services.Encryption;
using ResQ.API.Services.MercadoPago;

namespace ResQ.Tests.Services;

public class MercadoPagoOAuthServiceTests
{
    private readonly Mock<IMercadoPagoHttpClient> _mpClient = new();
    private readonly Mock<IEncryptionService> _encryption = new();
    private readonly Mock<IMerchantMpCredentialRepository> _credentialRepo = new();
    private readonly Mock<IMerchantProfileRepository> _merchantRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();

    private readonly IOptions<MpSettings> _mpOptions = Options.Create(new MpSettings
    {
        ClientId     = "CLIENT123",
        ClientSecret = "SECRET456",
        RedirectUri  = "https://api.resq.com/api/auth/mp/callback",
        UseTestMode  = true
    });

    private MercadoPagoOAuthService CreateSut() => new(
        _mpOptions,
        _mpClient.Object,
        _encryption.Object,
        _credentialRepo.Object,
        _merchantRepo.Object,
        _productRepo.Object);

    private static HttpResponseMessage BuildTokenResponse(
        string accessToken = "access-tok", string refreshToken = "refresh-tok",
        int expiresIn = 21600, string scope = "read write", long userId = 123456) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"{{accessToken}}","expires_in":{{expiresIn}},"refresh_token":"{{refreshToken}}","scope":"{{scope}}","user_id":{{userId}}}""",
                Encoding.UTF8,
                "application/json")
        };

    // ═══════════════════════════════════════════════════════════════════════════
    // BuildAuthorizationUrl
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildAuthorizationUrl_WithoutReturnOrigin_ReturnsUrlWithMerchantIdAsState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var url = sut.BuildAuthorizationUrl(42);

        // Assert
        Assert.StartsWith("https://auth.mercadopago.com.ar/authorization?", url);
        Assert.Contains("client_id=CLIENT123", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("platform_id=mp", url);
        Assert.Contains("state=42", url);
    }

    [Fact]
    public void BuildAuthorizationUrl_WithReturnOrigin_EncodesOriginIntoState()
    {
        // Arrange
        var sut = CreateSut();
        var expectedState = MpOAuthState.Encode(42, "https://app.resq.com");

        // Act
        var url = sut.BuildAuthorizationUrl(42, "https://app.resq.com");

        // Assert
        Assert.Contains($"state={Uri.EscapeDataString(expectedState)}", url);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HandleCallbackAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleCallbackAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _merchantRepo.Setup(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleCallbackAsync("code123", 99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task HandleCallbackAsync_WhenCodeExchangeFails_ReturnsBadRequestError()
    {
        // Arrange
        var merchant = new MerchantProfile { Id = 5, UserId = 1 };
        _merchantRepo.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _mpClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                 {
                     Content = new StringContent("invalid_grant", Encoding.UTF8, "application/json")
                 });

        var sut = CreateSut();

        // Act
        var result = await sut.HandleCallbackAsync("bad-code", 5);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("MP rechazó el intercambio de código", result.Errors[0].Message);
    }

    [Fact]
    public async Task HandleCallbackAsync_WhenNoExistingCredential_InsertsNewCredentialAndSetsConnectedStatus()
    {
        // Arrange
        var merchant = new MerchantProfile { Id = 5, UserId = 1, MpConnectionStatus = MpConnectionStatus.Disconnected };
        _merchantRepo.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantMpCredential?)null);
        _mpClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildTokenResponse());
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"enc({s})");
        _credentialRepo.Setup(c => c.AddAsync(It.IsAny<MerchantMpCredential>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _credentialRepo.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleCallbackAsync("good-code", 5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(MpConnectionStatus.Connected, merchant.MpConnectionStatus);
        _credentialRepo.Verify(c => c.AddAsync(
            It.Is<MerchantMpCredential>(cr => cr.MerchantId == 5 && cr.AccessToken == "enc(access-tok)" && cr.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
        _merchantRepo.Verify(m => m.Update(merchant), Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_WhenCredentialAlreadyExists_UpdatesExistingCredential()
    {
        // Arrange
        var merchant = new MerchantProfile { Id = 5, UserId = 1 };
        var existing = new MerchantMpCredential
        {
            Id = 1, MerchantId = 5, AccessToken = "old-enc-access", RefreshToken = "old-enc-refresh", IsActive = true
        };
        _merchantRepo.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mpClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildTokenResponse());
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"enc({s})");
        _credentialRepo.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleCallbackAsync("good-code", 5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("enc(access-tok)", existing.AccessToken);
        _credentialRepo.Verify(c => c.Update(existing), Times.Once);
        _credentialRepo.Verify(c => c.AddAsync(It.IsAny<MerchantMpCredential>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RefreshTokensAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefreshTokensAsync_WhenNoCredential_ReturnsNotFoundError()
    {
        // Arrange
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantMpCredential?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.RefreshTokensAsync(5);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontradas o inactivas", result.Errors[0].Message);
    }

    [Fact]
    public async Task RefreshTokensAsync_WhenCredentialInactive_ReturnsNotFoundError()
    {
        // Arrange
        var credential = new MerchantMpCredential { Id = 1, MerchantId = 5, IsActive = false };
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(credential);

        var sut = CreateSut();

        // Act
        var result = await sut.RefreshTokensAsync(5);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontradas o inactivas", result.Errors[0].Message);
    }

    [Fact]
    public async Task RefreshTokensAsync_WhenMpRejectsRefresh_MarksCredentialExpiredAndReturnsBadRequestError()
    {
        // Arrange
        var credential = new MerchantMpCredential { Id = 1, MerchantId = 5, RefreshToken = "enc-refresh", IsActive = true };
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _encryption.Setup(e => e.Decrypt("enc-refresh")).Returns("plain-refresh");
        _mpClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var merchant = new MerchantProfile { Id = 5, UserId = 1, MpConnectionStatus = MpConnectionStatus.Connected };
        _merchantRepo.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _credentialRepo.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.RefreshTokensAsync(5);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No se pudo renovar el token", result.Errors[0].Message);
        Assert.False(credential.IsActive);
        Assert.Equal(MpConnectionStatus.TokenExpired, merchant.MpConnectionStatus);
        _credentialRepo.Verify(c => c.Update(credential), Times.Once);
    }

    [Fact]
    public async Task RefreshTokensAsync_WhenSuccessful_UpdatesCredentialTokensAndReturnsOk()
    {
        // Arrange
        var credential = new MerchantMpCredential
        {
            Id = 1, MerchantId = 5, RefreshToken = "enc-refresh", AccessToken = "enc-access-old", IsActive = true
        };
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        _encryption.Setup(e => e.Decrypt("enc-refresh")).Returns("plain-refresh");
        _mpClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildTokenResponse(accessToken: "new-access", refreshToken: "new-refresh"));
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"enc({s})");
        _credentialRepo.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.RefreshTokensAsync(5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("enc(new-access)", credential.AccessToken);
        Assert.Equal("enc(new-refresh)", credential.RefreshToken);
        _credentialRepo.Verify(c => c.Update(credential), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DisconnectAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DisconnectAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantMpCredential?)null);
        _merchantRepo.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.DisconnectAsync(5);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task DisconnectAsync_WhenNoCredentialExists_StillDisconnectsMerchant()
    {
        // Arrange
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantMpCredential?)null);
        var merchant = new MerchantProfile { Id = 5, UserId = 1, MpConnectionStatus = MpConnectionStatus.Connected };
        _merchantRepo.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _productRepo.Setup(p => p.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());
        _credentialRepo.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.DisconnectAsync(5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(MpConnectionStatus.Disconnected, merchant.MpConnectionStatus);
        _credentialRepo.Verify(c => c.Update(It.IsAny<MerchantMpCredential>()), Times.Never);
    }

    [Fact]
    public async Task DisconnectAsync_WhenSuccessful_DeactivatesCredentialAndActiveProductsOnly()
    {
        // Arrange
        var credential = new MerchantMpCredential { Id = 1, MerchantId = 5, IsActive = true };
        _credentialRepo.Setup(c => c.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(credential);
        var merchant = new MerchantProfile { Id = 5, UserId = 1, MpConnectionStatus = MpConnectionStatus.Connected };
        _merchantRepo.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var activeProduct = new Product { Id = 1, MerchantId = 5, IsActive = true };
        var inactiveProduct = new Product { Id = 2, MerchantId = 5, IsActive = false };
        _productRepo.Setup(p => p.GetByMerchantIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Product> { activeProduct, inactiveProduct });
        _credentialRepo.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.DisconnectAsync(5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(credential.IsActive);
        Assert.False(activeProduct.IsActive);
        _productRepo.Verify(p => p.Update(activeProduct), Times.Once);
        _productRepo.Verify(p => p.Update(inactiveProduct), Times.Never);
        _credentialRepo.Verify(c => c.Update(credential), Times.Once);
    }
}
