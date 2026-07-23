using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.Tests.Repositories;

/// <summary>
/// Exercises <see cref="GenericRepository{T}"/> directly (rather than through a concrete
/// subclass) using <see cref="Category"/> as a simple, dependency-free entity.
/// </summary>
public class GenericRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly GenericRepository<Category> _sut;

    public GenericRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new GenericRepository<Category>(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Category> SeedCategoryAsync(string name = "Panadería")
    {
        var category = new Category { Name = name, CreatedAt = DateTime.UtcNow };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        var category = await SeedCategoryAsync();

        // Act
        var result = await _sut.GetByIdAsync(category.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Panadería", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAllAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_ReturnsAllPersistedEntities()
    {
        // Arrange
        await SeedCategoryAsync("Panadería");
        await SeedCategoryAsync("Sushi");

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoEntities_ReturnsEmptyCollection()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_QueriesWithoutAttachingToChangeTracker()
    {
        // Arrange
        await SeedCategoryAsync();
        _db.ChangeTracker.Clear();

        // Act
        await _sut.GetAllAsync();

        // Assert
        Assert.Empty(_db.ChangeTracker.Entries());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AddAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsEntity()
    {
        // Arrange
        var category = new Category { Name = "Pizza", CreatedAt = DateTime.UtcNow };

        // Act
        await _sut.AddAsync(category);
        var affected = await _sut.SaveChangesAsync();

        // Assert
        Assert.Equal(1, affected);
        var saved = await _db.Categories.FindAsync(category.Id);
        Assert.NotNull(saved);
        Assert.Equal("Pizza", saved.Name);
    }

    [Fact]
    public async Task AddAsync_WithoutSaveChanges_DoesNotPersistEntity()
    {
        // Arrange
        var category = new Category { Name = "Pizza", CreatedAt = DateTime.UtcNow };

        // Act
        await _sut.AddAsync(category);

        // Assert
        Assert.Empty(await _db.Categories.AsNoTracking().ToListAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Update
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Update_ThenSaveChanges_PersistsModifiedValues()
    {
        // Arrange
        var category = await SeedCategoryAsync("Original");
        category.Name = "Actualizado";

        // Act
        _sut.Update(category);
        var affected = await _sut.SaveChangesAsync();

        // Assert
        Assert.Equal(1, affected);
        var reloaded = await _db.Categories.AsNoTracking().FirstAsync(c => c.Id == category.Id);
        Assert.Equal("Actualizado", reloaded.Name);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_ThenSaveChanges_RemovesEntity()
    {
        // Arrange
        var category = await SeedCategoryAsync("ToDelete");

        // Act
        _sut.Delete(category);
        var affected = await _sut.SaveChangesAsync();

        // Assert
        Assert.Equal(1, affected);
        Assert.Null(await _db.Categories.FindAsync(category.Id));
    }

    [Fact]
    public async Task Delete_WithoutSaveChanges_DoesNotRemoveEntity()
    {
        // Arrange
        var category = await SeedCategoryAsync("Persistent");

        // Act
        _sut.Delete(category);

        // Assert
        var stillThere = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id);
        Assert.NotNull(stillThere);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SaveChangesAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveChangesAsync_WhenNoPendingChanges_ReturnsZero()
    {
        // Act
        var affected = await _sut.SaveChangesAsync();

        // Assert
        Assert.Equal(0, affected);
    }
}
