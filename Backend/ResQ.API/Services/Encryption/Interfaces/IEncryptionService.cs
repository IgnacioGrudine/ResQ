namespace ResQ.API.Services.Encryption;

public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plain-text string using AES-256 and returns the resulting cipher text
    /// encoded as a Base64 string, suitable for storage at rest.
    /// </summary>
    /// <remarks>
    /// Used primarily to protect Mercado Pago OAuth tokens before they are persisted to
    /// the database. The encryption key must be provided via the application configuration
    /// and must never be stored in the database.
    /// </remarks>
    /// <param name="plainText">The plain-text value to encrypt.</param>
    /// <returns>A Base64-encoded cipher text string.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts a Base64-encoded AES-256 cipher text string and returns the original plain-text value.
    /// </summary>
    /// <remarks>
    /// Used to retrieve Mercado Pago OAuth tokens from storage so they can be sent to the
    /// Mercado Pago API. The cipher text must have been produced by <see cref="Encrypt"/>.
    /// </remarks>
    /// <param name="cipherText">The Base64-encoded cipher text string to decrypt.</param>
    /// <returns>The original plain-text string.</returns>
    string Decrypt(string cipherText);
}
