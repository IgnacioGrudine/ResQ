using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ResQ.API.Models.Settings;
using ResQ.API.Services.Email;

namespace ResQ.Tests.Services;

/// <summary>
/// <see cref="SmtpEmailService"/> constructs MailKit's <c>SmtpClient</c> directly inside each
/// method — there is no injectable abstraction over the SMTP conversation, so it cannot be
/// mocked with Moq and the actual email content/delivery cannot be asserted in isolation.
/// <para/>
/// What *is* genuinely testable without a real mail server is the service's documented
/// "never block the caller" contract: every method wraps the send in try/catch and only logs
/// on failure. These tests point the client at an address nothing listens on (a closed local
/// port) so the connection is refused immediately, and assert the call still completes without
/// throwing and that the failure was logged.
/// </summary>
public class SmtpEmailServiceTests
{
    private readonly Mock<ILogger<SmtpEmailService>> _logger = new();

    private static IOptions<SmtpSettings> UnreachableSmtpOptions() => Options.Create(new SmtpSettings
    {
        Host = "127.0.0.1",
        Port = 59999,
        Username = "user",
        Password = "pass",
        FromEmail = "no-reply@resq.com.ar",
        FromName = "ResQ"
    });

    private static IOptions<MpSettings> FrontendOptions() => Options.Create(new MpSettings
    {
        FrontendBaseUrl = "https://app.resq.com.ar/"
    });

    private SmtpEmailService CreateSut() => new(UnreachableSmtpOptions(), FrontendOptions(), _logger.Object);

    private void VerifyLoggedError() =>
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

    // ═══════════════════════════════════════════════════════════════════════════
    // SendReviewRequestAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendReviewRequestAsync_WhenSmtpServerIsUnreachable_SwallowsExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var exception = await Record.ExceptionAsync(() =>
            sut.SendReviewRequestAsync("consumer@example.com", "Ana", "Pastelería Sol", 42, cts.Token));

        // Assert
        Assert.Null(exception);
        VerifyLoggedError();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SendPasswordResetAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendPasswordResetAsync_WhenSmtpServerIsUnreachable_SwallowsExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var exception = await Record.ExceptionAsync(() =>
            sut.SendPasswordResetAsync(
                "consumer@example.com", "Ana", "https://app.resq.com.ar/reset?token=abc", cts.Token));

        // Assert
        Assert.Null(exception);
        VerifyLoggedError();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SendGoogleOnlyAccountNoticeAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendGoogleOnlyAccountNoticeAsync_WhenSmtpServerIsUnreachable_SwallowsExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var exception = await Record.ExceptionAsync(() =>
            sut.SendGoogleOnlyAccountNoticeAsync("consumer@example.com", cts.Token));

        // Assert
        Assert.Null(exception);
        VerifyLoggedError();
    }
}
