using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Common;

namespace ResQ.API.Repositories.Common;

/// <summary>
/// Generic EF Core repository that provides standard CRUD operations for any entity
/// that derives from <see cref="BaseEntity"/>. Concrete repositories extend this class
/// to inherit these base operations and add domain-specific queries.
/// </summary>
/// <typeparam name="T">The entity type managed by this repository. Must derive from <see cref="BaseEntity"/>.</typeparam>
public class GenericRepository<T>(ResQDbContext db) : IGenericRepository<T>
    where T : BaseEntity
{
    /// <summary>The EF Core database context used to access the database.</summary>
    protected readonly ResQDbContext _db = db;

    /// <summary>The EF Core <see cref="DbSet{T}"/> used to query and persist entities of type <typeparamref name="T"/>.</summary>
    protected readonly DbSet<T> _set = db.Set<T>();

    /// <summary>
    /// Retrieves a single entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value of the entity to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The entity with the specified identifier, or <c>null</c> if it does not exist.
    /// </returns>
    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);

    /// <summary>
    /// Retrieves all entities of type <typeparamref name="T"/> as a no-tracking query.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all entities in the table.</returns>
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.AsNoTracking().ToListAsync(ct);

    /// <summary>
    /// Adds a new entity to the change tracker. The caller must call
    /// <see cref="SaveChangesAsync"/> to persist the record to the database.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    /// <summary>
    /// Marks an existing entity as modified in the change tracker so that all its
    /// scalar properties will be included in the next <c>UPDATE</c> statement.
    /// The caller must call <see cref="SaveChangesAsync"/> to persist the changes.
    /// </summary>
    /// <param name="entity">The entity with updated values to persist.</param>
    public void Update(T entity)
        => _db.Entry(entity).State = EntityState.Modified;

    /// <summary>
    /// Marks an entity for deletion in the change tracker.
    /// The caller must call <see cref="SaveChangesAsync"/> to execute the <c>DELETE</c>.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    public void Delete(T entity)
        => _set.Remove(entity);

    /// <summary>
    /// Persists all pending changes in the change tracker to the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
