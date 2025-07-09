namespace YummyZoom.Domain.Common.Models;

/// <summary>
/// Interface for entities that need to track their modification information.
/// Use this for entities that can be updated and need to track when and by whom they were last modified.
/// </summary>
public interface IModificationAuditable
{
    DateTimeOffset LastModified { get; set; }
    string? LastModifiedBy { get; set; }
}
