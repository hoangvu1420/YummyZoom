namespace YummyZoom.Domain.Common.Models;

public interface IHasEventId
{
	Guid EventId { get; }
	DateTime OccurredOnUtc { get; }
}
