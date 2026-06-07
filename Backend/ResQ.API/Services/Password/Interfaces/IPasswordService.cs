namespace ResQ.API.Services.Password;

public interface IPasswordService
{
    /// <summary>
    /// Computes a secure hash of the given plain-text password, incorporating a
    /// randomly generated salt so that each call produces a unique output.
    /// </summary>
    /// <param name="plainPassword">The plain-text password to hash.</param>
    /// <returns>
    /// A string containing the hashed password and the embedded salt in a format
    /// suitable for persistent storage.
    /// </returns>
    string Hash(string plainPassword);

    /// <summary>
    /// Verifies that the given plain-text password matches the stored hash.
    /// </summary>
    /// <param name="plainPassword">The plain-text password provided during login.</param>
    /// <param name="hash">The stored hash previously produced by <see cref="Hash"/>.</param>
    /// <returns>
    /// <c>true</c> if the plain-text password corresponds to the stored hash;
    /// <c>false</c> otherwise.
    /// </returns>
    bool Verify(string plainPassword, string hash);
}
