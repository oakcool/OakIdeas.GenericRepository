using System;

namespace OakIdeas.GenericRepository.Models;

/// <summary>
/// Interface for entities that support soft delete pattern.
/// Soft delete marks entities as deleted without physically removing them from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether this entity is soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was soft-deleted.
    /// Null if the entity is not deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted the entity.
    /// Null if the entity is not deleted or no user context is available.
    /// </summary>
    string? DeletedBy { get; set; }
}
