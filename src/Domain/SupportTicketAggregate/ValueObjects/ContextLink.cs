using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

public sealed class ContextLink : ValueObject
{
    public ContextEntityType EntityType { get; private set; }
    public Guid EntityID { get; private set; }

    private ContextLink(ContextEntityType entityType, Guid entityId)
    {
        EntityType = entityType;
        EntityID = entityId;
    }

    public static Result<ContextLink> Create(ContextEntityType entityType, Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            return Result.Failure<ContextLink>(SupportTicketErrors.InvalidContextEntityId("Entity ID cannot be empty"));
        }

        return Result.Success(new ContextLink(entityType, entityId));
    }

    public static Result<ContextLink> Create(ContextEntityType entityType, string entityId)
    {
        if (!Guid.TryParse(entityId, out var guid))
        {
            return Result.Failure<ContextLink>(SupportTicketErrors.InvalidContextEntityId($"Entity ID '{entityId}' is not a valid GUID"));
        }

        return Create(entityType, guid);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EntityType;
        yield return EntityID;
    }

#pragma warning disable CS8618
    // For EF Core
    private ContextLink()
    {
    }
#pragma warning restore CS8618
}
