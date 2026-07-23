using Microsoft.Extensions.Configuration;
using Moq;
using ResQ.API.Services.Encryption;

namespace ResQ.Tests.Services;

public class AesEncryptionServiceTests
{
    // 32 ASCII characters == 32 bytes once decoded, satisfying the AES-256 key length check.
    private const string ValidKeyBase64 = "MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI=";

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static AesEncryptionService CreateSut(string? keyBase64 = ValidKeyBase64)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Encryption:Key"]).Returns(keyBase64);
        return new AesEncryptionService(config.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_WhenKeyMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Encryption:Key"]).Returns((string?)null);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => new AesEncryptionService(config.Object));

        // Assert
        Assert.Contains("not configured", ex.Message);
    }

    [Fact]
    public void Constructor_WhenKeyIsNotExactly32Bytes_ThrowsInvalidOperationException()
    {
        // Arrange
        var shortKeyBase64 = Convert.ToBase64String(new byte[16]);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => CreateSut(shortKeyBase64));

        // Assert
        Assert.Contains("32 bytes", ex.Message);
    }

    [Fact]
    public void Constructor_WhenKeyIsInvalidBase64_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => CreateSut("not-valid-base64!!!"));
    }

    [Fact]
    public void Constructor_WithValid32ByteKey_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() => CreateSut());

        // Assert
        Assert.Null(exception);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Encrypt / Decrypt
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Decrypt_OfEncryptedValue_ReturnsOriginalPlainText()
    {
        // Arrange
        var sut       = CreateSut();
        var plainText = "mp-access-token-123456";

        // Act
        var cipherText = sut.Encrypt(plainText);
        var decrypted  = sut.Decrypt(cipherText);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Encrypt_ReturnsValueDifferentFromPlainText()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var cipherText = sut.Encrypt("some-sensitive-token");

        // Assert
        Assert.NotEqual("some-sensitive-token", cipherText);
    }

    [Fact]
    public void Encrypt_CalledTwiceWithSamePlainText_ProducesDifferentCipherText()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var cipher1 = sut.Encrypt("same-plaintext");
        var cipher2 = sut.Encrypt("same-plaintext");

        // Assert
        Assert.NotEqual(cipher1, cipher2);
    }

    [Fact]
    public void Encrypt_ReturnsBase64StringLongerThanIvAlone()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var cipherText = sut.Encrypt("token-value");

        // Assert
        var bytes = Convert.FromBase64String(cipherText);
        Assert.True(bytes.Length > 16);
    }

    [Fact]
    public void Encrypt_EmptyString_RoundTripsToEmptyString()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var cipherText = sut.Encrypt(string.Empty);
        var decrypted  = sut.Decrypt(cipherText);

        // Assert
        Assert.Equal(string.Empty, decrypted);
    }

    [Fact]
    public void Decrypt_WithTamperedCipherText_ThrowsCryptographicException()
    {
        // Arrange
        var sut        = CreateSut();
        var cipherText = sut.Encrypt("original-token");
        var bytes      = Convert.FromBase64String(cipherText);
        bytes[^1]     ^= 0xFF; // corrupt the last byte, invalidating PKCS7 padding
        var tampered   = Convert.ToBase64String(bytes);

        // Act & Assert
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() => sut.Decrypt(tampered));
    }
}
