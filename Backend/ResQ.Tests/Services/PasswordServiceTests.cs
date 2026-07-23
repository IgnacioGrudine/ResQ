using ResQ.API.Services.Password;

namespace ResQ.Tests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    // ═══════════════════════════════════════════════════════════════════════════
    // Hash
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Hash_ReturnsValueDifferentFromPlainText()
    {
        // Act
        var hash = _sut.Hash("mySecretPass123");

        // Assert
        Assert.NotEqual("mySecretPass123", hash);
    }

    [Fact]
    public void Hash_ReturnsBCryptFormattedHash()
    {
        // Act
        var hash = _sut.Hash("mySecretPass123");

        // Assert
        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void Hash_CalledTwiceWithSamePassword_ProducesDifferentHashes()
    {
        // Act
        var hash1 = _sut.Hash("samePassword");
        var hash2 = _sut.Hash("samePassword");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Verify
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var hash = _sut.Hash("correctPassword");

        // Act
        var result = _sut.Verify("correctPassword", hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _sut.Hash("correctPassword");

        // Act
        var result = _sut.Verify("wrongPassword", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        // Arrange
        var hash = _sut.Hash("CaseSensitive");

        // Act
        var result = _sut.Verify("casesensitive", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashThenVerify_RoundTripsSuccessfullyForVariousInputs()
    {
        // Arrange
        string[] passwords =
        [
            "short",
            "a-very-long-password-with-many-characters-1234567890",
            "p@$$w0rd!ñ特殊"
        ];

        foreach (var password in passwords)
        {
            // Act
            var hash = _sut.Hash(password);

            // Assert
            Assert.True(_sut.Verify(password, hash));
        }
    }
}
