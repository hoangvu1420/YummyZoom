namespace YummyZoom.Application.Common.Authorization;

public interface IContextualCommand
{
    string ResourceType { get; }
    string ResourceId { get; }
}
