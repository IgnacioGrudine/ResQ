using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IUserRepository : IGenericRepository<User>
{
    /// <summary>
    /// Returns the user with the given email address, or <c>null</c> if no user
    /// with that email exists.
    /// </summary>
    /// <remarks>
    /// Email addresses are treated as case-insensitive unique identifiers.
    /// Used during login and duplicate-email validation.
    /// </remarks>
    /// <param name="email">The email address to search for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="User"/>, or <c>null</c> if not found.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Returns the user by their identifier, eagerly loading the associated
    /// <see cref="UserRole"/> collection with each role's <see cref="Role"/> reference.
    /// </summary>
    /// <remarks>
    /// Used to retrieve role information when generating JWT access tokens.
    /// </remarks>
    /// <param name="id">The identifier of the user to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="User"/> with roles populated, or <c>null</c> if not found.
    /// </returns>
    Task<User?> GetWithRolesAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a user account with the given email address already exists.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if at least one user with that email exists; <c>false</c> otherwise.
    /// </returns>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
}
