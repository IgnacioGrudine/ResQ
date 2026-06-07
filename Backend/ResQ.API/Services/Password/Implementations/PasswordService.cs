namespace ResQ.API.Services.Password;

/// <summary>
/// Provides BCrypt-based password hashing and verification for user credentials.
/// </summary>
/// <remarks>
/// BCrypt is chosen over faster algorithms (PBKDF2, Argon2) because its cost factor
/// is intentionally adjustable and its slowness is a feature — it makes brute-force
/// attacks computationally expensive. The work factor is set to 12, which provides a
/// strong security/performance balance on modern hardware.
/// </remarks>
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    /// <summary>
    /// Hashes a plain-text password using BCrypt with a work factor of <c>12</c>.
    /// </summary>
    /// <param name="plainPassword">The user's plain-text password to hash.</param>
    /// <returns>
    /// A BCrypt hash string (60 characters) that includes the algorithm identifier,
    /// work factor, and salt, safe to store directly in the database.
    /// </returns>
    public string Hash(string plainPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);

    /// <summary>
    /// Verifies a plain-text password against a stored BCrypt hash.
    /// </summary>
    /// <param name="plainPassword">The plain-text password provided by the user at login.</param>
    /// <param name="hash">The stored BCrypt hash to compare against.</param>
    /// <returns>
    /// <c>true</c> if the password matches the hash; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// BCrypt's <c>Verify</c> method is timing-safe: it always performs the full computation
    /// regardless of where a mismatch occurs, mitigating timing-based side-channel attacks.
    /// </remarks>
    public bool Verify(string plainPassword, string hash)
        => BCrypt.Net.BCrypt.Verify(plainPassword, hash);
}
