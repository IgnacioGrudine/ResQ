namespace ResQ.API.Models.Common;

/// <summary>
/// Abstract base class inherited by all persistent entities in the ResQ domain.
/// Provides a surrogate primary key and standardised audit timestamps managed
/// automatically by the EF Core <c>SaveChanges</c> interceptor or conventions.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Auto-incremented integer primary key assigned by the database on insert.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// UTC timestamp recording when this entity was first persisted to the database.
    /// Set once on insert and never updated.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp recording the last time this entity was modified.
    /// Null if the entity has never been updated after its initial creation.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
