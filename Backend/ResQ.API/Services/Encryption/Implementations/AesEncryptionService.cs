using System.Security.Cryptography;
using System.Text;

namespace ResQ.API.Services.Encryption;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration config)
    {
        var b64 = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is not configured.");
        _key = Convert.FromBase64String(b64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:Key must be exactly 32 bytes (AES-256).");
    }

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
