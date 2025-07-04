using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

public sealed class SupportTicketId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private SupportTicketId(Guid value)
    {
        Value = value;
    }

    public static SupportTicketId CreateUnique()
    {
        return new SupportTicketId(Guid.NewGuid());
    }

    public static SupportTicketId Create(Guid value)
    {
        return new SupportTicketId(value);
    }

    public static Result<SupportTicketId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<SupportTicketId>(SupportTicketErrors.InvalidSupportTicketId(value));
        }

        return Result.Success(new SupportTicketId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private SupportTicketId()
    {
    }
#pragma warning restore CS8618
}
