namespace YummyZoom.Domain.Common.Models;

/// <summary>
/// Interface for entities that support soft deletion.
/// Entities implementing this interface will be marked as deleted rather than physically removed from the database.
/// 
/// Note: The setters are public to allow infrastructure components (interceptors, etc.) to set these values.
/// Domain logic should use the MarkAsDeleted method on the aggregate root instead of setting these properties directly.
/// </summary>
public interface ISoftDeletableEntity
{
    /// <summary>
    /// Indicates whether the entity has been soft-deleted
    /// </summary>
    bool IsDeleted { get; set; }
    
    /// <summary>
    /// The timestamp when the entity was soft-deleted
    /// </summary>
    DateTimeOffset? DeletedOn { get; set; }
    
    /// <summary>
    /// The identifier of who soft-deleted the entity
    /// </summary>
    string? DeletedBy { get; set; }
}
