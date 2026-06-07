using ResQ.API.Models.Common;

namespace ResQ.API.Repositories.Common;

public interface IGenericRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Returns a single entity by its primary key, or <c>null</c> if no entity
    /// with the given identifier exists.
    /// </summary>
    /// <param name="id">The primary key of the entity to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching entity, or <c>null</c> if not found.</returns>
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns all entities of type <typeparamref name="T"/> from the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection containing all persisted entities.</returns>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new entity to the change tracker so it will be inserted on the
    /// next call to <see cref="SaveChangesAsync"/>.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing entity as modified in the change tracker so its changes
    /// will be persisted on the next call to <see cref="SaveChangesAsync"/>.
    /// </summary>
    /// <param name="entity">The entity with updated values to persist.</param>
    void Update(T entity);

    /// <summary>
    /// Marks an existing entity for removal in the change tracker so it will be
    /// deleted on the next call to <see cref="SaveChangesAsync"/>.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    void Delete(T entity);

    /// <summary>
    /// Persists all pending changes tracked by the current EF Core DbContext to the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
