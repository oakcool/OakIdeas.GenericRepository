using System;

namespace OakIdeas.GenericRepository.Models;

/// <summary>
/// Base class for entities with a generic primary key that support soft delete pattern.
/// </summary>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public abstract class SoftDeletableEntity<TKey> : EntityBase<TKey>, ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether this entity is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was soft-deleted.
    /// Null if the entity is not deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted the entity.
    /// Null if the entity is not deleted or no user context is available.
    /// </summary>
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base class for entities with an integer primary key that support soft delete pattern.
/// Provided for backward compatibility with existing code.
/// </summary>
public abstract class SoftDeletableEntity : SoftDeletableEntity<int>
{
}
