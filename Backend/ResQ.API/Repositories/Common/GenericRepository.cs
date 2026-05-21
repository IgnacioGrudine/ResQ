using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Common;

namespace ResQ.API.Repositories.Common;

public class GenericRepository<T>(ResQDbContext db) : IGenericRepository<T>
    where T : BaseEntity
{
    protected readonly ResQDbContext _db = db;
    protected readonly DbSet<T> _set = db.Set<T>();

    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void Update(T entity)
        => _db.Entry(entity).State = EntityState.Modified;

    public void Delete(T entity)
        => _set.Remove(entity);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
