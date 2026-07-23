using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Models.Settings;
using ResQ.API.Services.MercadoPago;

namespace ResQ.Tests.Controllers;

public class MercadoPagoOAuthControllerTests
{
    private const int MerchantProfileId = 42;

    private readonly Mock<IMercadoPagoOAuthService> _oauthService = new();

    private readonly IOptions<MpSettings> _mpOptions = Options.Create(new MpSettings
    {
        FrontendBaseUrl = "https://resq.com"
    });

    private MercadoPagoOAuthController CreateSut()
    {
        var claims = new[]
        {
            new Claim("profileId", MerchantProfileId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, "100")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return new MercadoPagoOAuthController(_oauthService.Object, _mpOptions)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAuthUrl
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetAuthUrl_ReturnsOkWithAuthUrlBuiltFromProfileIdClaim()
    {
        // Arrange
        _oauthService.Setup(s => s.BuildAuthorizationUrl(MerchantProfileId, null))
                     .Returns("https://auth.mercadopago.com.ar/authorization?client_id=X");

        var sut = CreateSut();

        // Act
        var actionResult = sut.GetAuthUrl(null);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<MpAuthUrlResponse>(result.Value);
        Assert.Equal("https://auth.mercadopago.com.ar/authorization?client_id=X", response.AuthUrl);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Callback
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Callback_WhenErrorParamPresent_RedirectsWithErrorStatus()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var actionResult = await sut.Callback(code: null, state: null, error: "access_denied", CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(actionResult);
        Assert.Equal("https://resq.com/mp/callback?status=error&message=access_denied", redirect.Url);
    }

    [Fact]
    public async Task Callback_WhenCodeMissing_RedirectsWithMissingParamsError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var actionResult = await sut.Callback(code: null, state: "5", error: null, CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(actionResult);
        Assert.Equal("https://resq.com/mp/callback?status=error&message=missing_params", redirect.Url);
    }

    [Fact]
    public async Task Callback_WhenStateInvalid_RedirectsWithInvalidStateError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var actionResult = await sut.Callback(code: "code123", state: "not-a-number", error: null, CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(actionResult);
        Assert.Equal("https://resq.com/mp/callback?status=error&message=invalid_state", redirect.Url);
    }

    [Fact]
    public async Task Callback_WhenHandleCallbackFails_RedirectsWithErrorMessage()
    {
        // Arrange
        _oauthService.Setup(s => s.HandleCallbackAsync("code123", 5, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new BadRequestError("bad exchange")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Callback(code: "code123", state: "5", error: null, CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(actionResult);
        Assert.StartsWith("https://resq.com/mp/callback?status=error&message=", redirect.Url);
    }

    [Fact]
    public async Task Callback_WhenSuccessful_RedirectsWithSuccessStatus()
    {
        // Arrange
        _oauthService.Setup(s => s.HandleCallbackAsync("code123", 5, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Callback(code: "code123", state: "5", error: null, CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(actionResult);
        Assert.Equal("https://resq.com/mp/callback?status=success", redirect.Url);
    }

    [Fact]
    public async Task Callback_WithAllowedLocalhostReturnOrigin_RedirectsToThatOrigin()
    {
        // Arrange
        var state = MpOAuthState.Encode(5, "http://localhost:4200");
        _oauthService.Setup(s => s.HandleCallbackAsync("code123", 5, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Callback(code: "code123", state: state, error: null, CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(actionResult);
        Assert.Equal("http://localhost:4200/mp/callback?status=success", redirect.Url);
    }

    [Fact]
    public async Task Callback_WithDisallowedReturnOrigin_FallsBackToConfiguredFrontendBaseUrl()
    {
        // Arrange
        var state = MpOAuthState.Encode(5, "https://evil-site.com");
        _oauthService.Setup(s => s.HandleCallbackAsync("code123", 5, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Callback(code: "code123", state: state, error: null, CancellationToken.None);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(actionResult);
        Assert.Equal("https://resq.com/mp/callback?status=success", redirect.Url);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Disconnect
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Disconnect_WhenServiceReturnsOk_Returns204NoContent()
    {
        // Arrange
        _oauthService.Setup(s => s.DisconnectAsync(MerchantProfileId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Disconnect(CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public async Task Disconnect_WhenMerchantNotFound_Returns404()
    {
        // Arrange
        _oauthService.Setup(s => s.DisconnectAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new NotFoundError("Comercio no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.Disconnect(CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }
}
