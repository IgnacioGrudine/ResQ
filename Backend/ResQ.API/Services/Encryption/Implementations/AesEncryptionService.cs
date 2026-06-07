using System.Security.Cryptography;
using System.Text;

namespace ResQ.API.Services.Encryption;

/// <summary>
/// Provides AES-256-CBC symmetric encryption and decryption for sensitive data at rest,
/// primarily used to protect Mercado Pago OAuth access and refresh tokens stored in the database.
/// </summary>
/// <remarks>
/// A new random 16-byte IV is generated for every <see cref="Encrypt"/> call, ensuring that
/// the same plaintext produces a different ciphertext each time (semantic security).
/// The IV is prepended to the ciphertext and the combined bytes are stored as a single
/// Base64-encoded string, so no out-of-band IV management is required.
///
/// The 32-byte (256-bit) encryption key is loaded from configuration key <c>Encryption:Key</c>
/// as a Base64 string and should be stored in Azure Key Vault in production — never hardcoded
/// or committed to source control.
/// </remarks>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    /// <summary>
    /// Initialises the service by loading and validating the AES-256 key from configuration.
    /// </summary>
    /// <param name="config">The application configuration, expected to contain <c>Encryption:Key</c> as a Base64 string.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <c>Encryption:Key</c> is missing or if the decoded key is not exactly 32 bytes.
    /// </exception>
    public AesEncryptionService(IConfiguration config)
    {
        var b64 = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is not configured.");
        _key = Convert.FromBase64String(b64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:Key must be exactly 32 bytes (AES-256).");
    }

    /// <summary>
    /// Encrypts a UTF-8 plain-text string using AES-256-CBC with a randomly generated IV.
    /// </summary>
    /// <param name="plainText">The plain-text string to encrypt (e.g., a Mercado Pago OAuth token).</param>
    /// <returns>
    /// A Base64-encoded string that encodes <c>IV (16 bytes) || ciphertext</c>.
    /// This value is safe to store directly in the database.
    /// </returns>
    /// <remarks>
    /// The IV is prepended to the ciphertext so that <see cref="Decrypt"/> can extract it
    /// without any additional storage column.
    /// </remarks>
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes  = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // IV (16 bytes) || ciphertext — stored together as single Base64 string
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts a Base64-encoded ciphertext previously produced by <see cref="Encrypt"/>.
    /// </summary>
    /// <param name="cipherText">
    /// A Base64 string encoding <c>IV (16 bytes) || ciphertext</c>, as returned by <see cref="Encrypt"/>.
    /// </param>
    /// <returns>The original UTF-8 plain-text string.</returns>
    /// <remarks>
    /// The first 16 bytes of the decoded buffer are extracted as the IV;
    /// the remainder is the actual ciphertext passed to the AES decryptor.
    /// </remarks>
    public string Decrypt(string cipherText)
    {
        var allBytes    = Convert.FromBase64String(cipherText);
        var iv          = allBytes[..16];
        var cipherBytes = allBytes[16..];

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV  = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
