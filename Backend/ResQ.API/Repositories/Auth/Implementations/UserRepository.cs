using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// Provides data access operations for the <see cref="User"/> entity, extending generic
/// CRUD with authentication-oriented queries such as email lookup, role loading,
/// and existence checks.
/// </summary>
public class UserRepository(ResQDbContext db) : GenericRepository<User>(db), IUserRepository
{
    /// <summary>
    /// Retrieves a user by email address, normalising the input to lowercase before querying
    /// to ensure case-insensitive matching against the stored value.
    /// </summary>
    /// <param name="email">The email address to search for (case-insensitive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="User"/>, or <c>null</c> if no user with that email exists.
    /// </returns>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    /// <summary>
    /// Retrieves a user by identifier, eagerly loading the associated <c>UserRoles</c>
    /// collection so that role-based authorization checks can be performed without
    /// a subsequent query.
    /// </summary>
    /// <param name="id">The identifier of the user to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="User"/> with <c>UserRoles</c> loaded, or <c>null</c> if not found.
    /// </returns>
    public async Task<User?> GetWithRolesAsync(int id, CancellationToken ct = default)
        => await _set
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <summary>
    /// Checks whether a user with the given email address already exists in the database.
    /// The comparison is case-insensitive (email is lowercased before querying).
    /// </summary>
    /// <param name="email">The email address to check for uniqueness.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if a user with that email exists; otherwise <c>false</c>.</returns>
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _set.AnyAsync(u => u.Email == email.ToLower(), ct);
}
