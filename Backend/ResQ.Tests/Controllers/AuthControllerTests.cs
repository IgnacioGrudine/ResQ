using FluentResults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.Auth;
using ResQ.API.Models.Settings;
using ResQ.API.Services.Auth;

namespace ResQ.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IWebHostEnvironment> _env = new();
    private readonly IOptions<JwtSettings> _jwtOptions = Options.Create(new JwtSettings
    {
        RefreshTokenExpirationDays   = 7,
        AccessTokenExpirationMinutes = 15
    });

    private AuthController CreateSut()
    {
        _env.Setup(e => e.EnvironmentName).Returns("Development");
        var controller = new AuthController(_authService.Object, _jwtOptions, _env.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    private static AuthResponse BuildAuthResponse(string role = "Consumer") => new()
    {
        AccessToken          = "at-token",
        RefreshToken         = "rt-token",
        AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
        Role                 = role,
        ProfileId            = 1
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // RegisterConsumer
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RegisterConsumer_WhenServiceReturnsOk_Returns201WithClientResponse()
    {
        // Arrange
        var authResponse = BuildAuthResponse();
        _authService.Setup(s => s.RegisterConsumerAsync(It.IsAny<RegisterConsumerRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Ok(authResponse));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.RegisterConsumer(new RegisterConsumerRequest(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(201, result.StatusCode);
        var response = Assert.IsType<ClientAuthResponse>(result.Value);
        Assert.Equal(authResponse.AccessToken, response.AccessToken);
        Assert.Equal(authResponse.Role, response.Role);
        Assert.Null(response.GetType().GetProperty("RefreshToken")); // no RefreshToken in client response
    }

    [Fact]
    public async Task RegisterConsumer_WhenEmailConflict_Returns409()
    {
        // Arrange
        _authService.Setup(s => s.RegisterConsumerAsync(It.IsAny<RegisterConsumerRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Fail(new ConflictError("El email ya está registrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.RegisterConsumer(new RegisterConsumerRequest(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<ConflictObjectResult>(actionResult.Result);
        Assert.Equal(409, result.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RegisterMerchant
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RegisterMerchant_WhenServiceReturnsOk_Returns201WithClientResponse()
    {
        // Arrange
        var authResponse = BuildAuthResponse("Merchant");
        _authService.Setup(s => s.RegisterMerchantAsync(It.IsAny<RegisterMerchantRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Ok(authResponse));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.RegisterMerchant(new RegisterMerchantRequest(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(201, result.StatusCode);
        var response = Assert.IsType<ClientAuthResponse>(result.Value);
        Assert.Equal("Merchant", response.Role);
    }

    [Fact]
    public async Task RegisterMerchant_WhenEmailConflict_Returns409()
    {
        // Arrange
        _authService.Setup(s => s.RegisterMerchantAsync(It.IsAny<RegisterMerchantRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Fail(new ConflictError("El email ya está registrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.RegisterMerchant(new RegisterMerchantRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<ConflictObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Login
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_WhenValidCredentials_Returns200WithClientResponse()
    {
        // Arrange
        var authResponse = BuildAuthResponse();
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Ok(authResponse));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Login(new LoginRequest("u@t.com", "pass"), CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, result.StatusCode);
        var response = Assert.IsType<ClientAuthResponse>(result.Value);
        Assert.Equal("at-token", response.AccessToken);
    }

    [Fact]
    public async Task Login_WhenInvalidCredentials_Returns401()
    {
        // Arrange
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Fail(new UnauthorizedError("Credenciales inválidas.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Login(new LoginRequest("u@t.com", "wrong"), CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Refresh
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Refresh_WhenTokenValid_Returns200WithNewAccessToken()
    {
        // Arrange
        var authResponse = BuildAuthResponse();
        _authService.Setup(s => s.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Ok(authResponse));

        var sut = CreateSut();
        sut.ControllerContext.HttpContext.Request.Headers["Cookie"] = "resq_refresh_token=old-rt";

        // Act
        var actionResult = await sut.Refresh(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, result.StatusCode);
        _authService.Verify(s => s.RefreshTokenAsync("old-rt", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenCookieMissing_PassesEmptyStringToService()
    {
        // Arrange
        _authService.Setup(s => s.RefreshTokenAsync(string.Empty, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Fail(new UnauthorizedError("Refresh token inválido.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Refresh(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
        _authService.Verify(s => s.RefreshTokenAsync(string.Empty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenTokenExpired_Returns401()
    {
        // Arrange
        _authService.Setup(s => s.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Fail(new UnauthorizedError("Refresh token expirado o revocado.")));

        var sut = CreateSut();
        sut.ControllerContext.HttpContext.Request.Headers["Cookie"] = "resq_refresh_token=expired-rt";

        // Act
        var actionResult = await sut.Refresh(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Logout
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Logout_Returns204NoContent()
    {
        // Arrange
        _authService.Setup(s => s.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Ok());

        var sut = CreateSut();
        sut.ControllerContext.HttpContext.Request.Headers["Cookie"] = "resq_refresh_token=rt";

        // Act
        var actionResult = await sut.Logout(CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
        _authService.Verify(s => s.LogoutAsync("rt", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_WhenNoCookiePresent_StillReturns204()
    {
        // Arrange
        _authService.Setup(s => s.LogoutAsync(string.Empty, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Logout(CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }
}
