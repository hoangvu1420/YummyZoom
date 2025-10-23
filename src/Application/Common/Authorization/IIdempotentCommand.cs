namespace YummyZoom.Application.Common.Authorization;

/// <summary>
/// Marker interface for commands that support idempotency.
/// Commands implementing this interface will have their responses cached
/// for a short period to prevent duplicate execution.
/// </summary>
public interface IIdempotentCommand
{
    /// <summary>
    /// Gets the idempotency key for this command.
    /// Should be a unique identifier (typically a GUID) provided by the client.
    /// </summary>
    string? IdempotencyKey { get; }
}
