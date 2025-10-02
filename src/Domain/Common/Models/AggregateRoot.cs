namespace YummyZoom.Domain.Common.Models;

public abstract class AggregateRoot<TId, TIdType> : Entity<TId>
    where TId : AggregateRootId<TIdType>
{
    // Remove the shadowing property - let Entity<TId>.Id remain as TId
    // This ensures LINQ queries work with the specific typed ID (e.g., MenuItemId)

    protected AggregateRoot(TId id) : base(id)
    {
    }

#pragma warning disable CS8618
    protected AggregateRoot()
    {
    }
#pragma warning restore CS8618
}
