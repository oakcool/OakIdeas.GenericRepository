namespace OakIdeas.GenericRepository.Models;

/// <summary>
/// Base class for entities with a generic primary key type.
/// </summary>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public abstract class EntityBase<TKey>
{
    /// <summary>
    /// Gets or sets the primary key identifier.
    /// </summary>
    public TKey ID { get; set; } = default!;
}

/// <summary>
/// Base class for entities with an integer primary key.
/// Provided for backward compatibility with existing code.
/// </summary>
public abstract class EntityBase : EntityBase<int>
{
}
