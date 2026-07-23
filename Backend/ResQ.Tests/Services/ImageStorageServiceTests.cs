using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Moq;
using ResQ.API.Models.Settings;
using ResQ.API.Services.Storage;

namespace ResQ.Tests.Services;

public class ImageStorageServiceTests
{
    private readonly Mock<IMinioClient> _minio = new();

    private static MinioSettings BuildSettings() => new()
    {
        Endpoint = "localhost:9000",
        AccessKey = "minioadmin",
        SecretKey = "minioadmin",
        BucketName = "resq-images",
        UseSSL = false,
        PublicBaseUrl = "https://storage.resq.com.ar"
    };

    private ImageStorageService CreateSut(MinioSettings? settings = null) =>
        new(_minio.Object, Options.Create(settings ?? BuildSettings()));

    private static Mock<IFormFile> BuildImageFile(
        string contentType = "image/jpeg",
        long length = 1024,
        string fileName = "photo.jpg")
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.ContentType).Returns(contentType);
        file.SetupGet(f => f.Length).Returns(length);
        file.SetupGet(f => f.FileName).Returns(fileName);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(new byte[length]));
        return file;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ResolvePublicUrl — pure, dependency-free logic
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ResolvePublicUrl_WhenStoredValueIsNull_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.ResolvePublicUrl(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolvePublicUrl_WhenStoredValueIsEmpty_ReturnsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.ResolvePublicUrl(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("http://cdn.example.com/img.jpg")]
    [InlineData("https://cdn.example.com/img.jpg")]
    [InlineData("HTTPS://cdn.example.com/img.jpg")]
    [InlineData("HTTP://cdn.example.com/img.jpg")]
    public void ResolvePublicUrl_WhenAlreadyAnAbsoluteUrl_ReturnsUnchanged(string absoluteUrl)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.ResolvePublicUrl(absoluteUrl);

        // Assert
        Assert.Equal(absoluteUrl, result);
    }

    [Fact]
    public void ResolvePublicUrl_WhenStoredValueIsRelativePath_PrefixesWithPublicBaseUrl()
    {
        // Arrange
        var settings = BuildSettings();
        var sut = CreateSut(settings);

        // Act
        var result = sut.ResolvePublicUrl("resq-images/products/42/photo.jpg");

        // Assert
        Assert.Equal($"{settings.PublicBaseUrl}/resq-images/products/42/photo.jpg", result);
    }

    [Fact]
    public void ResolvePublicUrl_UsesCurrentlyConfiguredPublicBaseUrl()
    {
        // Arrange — resolving must always use the *current* configuration, not one baked in
        // at upload time (that's the whole point of storing a relative path).
        var settings = BuildSettings();
        settings.PublicBaseUrl = "https://new-domain.resq.com.ar";
        var sut = CreateSut(settings);

        // Act
        var result = sut.ResolvePublicUrl("resq-images/merchants/7/logo.png");

        // Assert
        Assert.Equal("https://new-domain.resq.com.ar/resq-images/merchants/7/logo.png", result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UploadAsync — validation
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UploadAsync_WhenContentTypeNotAllowed_ThrowsInvalidOperationException()
    {
        // Arrange
        var file = BuildImageFile(contentType: "application/pdf");
        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.UploadAsync(file.Object, "products/1"));
        Assert.Contains("JPG, PNG o WebP", ex.Message);
        _minio.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenFileExceeds5Mb_ThrowsInvalidOperationException()
    {
        // Arrange
        var file = BuildImageFile(length: 5 * 1024 * 1024 + 1);
        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.UploadAsync(file.Object, "products/1"));
        Assert.Contains("5 MB", ex.Message);
        _minio.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UploadAsync — happy path
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UploadAsync_WhenValid_UploadsAndReturnsBucketRelativePath()
    {
        // Arrange
        var settings = BuildSettings();
        _minio.Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);
        _minio.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((PutObjectResponse)null!);

        var file = BuildImageFile(fileName: "photo.jpg");
        var sut = CreateSut(settings);

        // Act
        var result = await sut.UploadAsync(file.Object, "products/42");

        // Assert
        var pattern = $"^{Regex.Escape(settings.BucketName)}/products/42/" +
                      @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.jpg$";
        Assert.Matches(pattern, result);
        _minio.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
        _minio.Verify(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenBucketDoesNotExist_CreatesItWithPublicReadPolicy()
    {
        // Arrange
        _minio.Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _minio.Setup(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        _minio.Setup(m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        _minio.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((PutObjectResponse)null!);

        var file = BuildImageFile();
        var sut = CreateSut();

        // Act
        await sut.UploadAsync(file.Object, "merchants/7");

        // Assert
        _minio.Verify(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Once);
        _minio.Verify(m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
