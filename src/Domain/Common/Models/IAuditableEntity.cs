namespace YummyZoom.Domain.Common.Models;

/// <summary>
/// A composite interface for entities that need both creation and modification auditing.
/// This provides convenience for entities that are fully auditable.
/// </summary>
public interface IAuditableEntity : ICreationAuditable, IModificationAuditable
{
}
