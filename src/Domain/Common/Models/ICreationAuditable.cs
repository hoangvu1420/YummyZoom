namespace YummyZoom.Domain.Common.Models;

/// <summary>
/// Interface for entities that need to track their creation information.
/// Use this for immutable entities that only need to record when and by whom they were created.
/// </summary>
public interface ICreationAuditable
{
    DateTimeOffset Created { get; set; }
    string? CreatedBy { get; set; }
}
