using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

public sealed class MessageId : ValueObject
{
    public Guid Value { get; private set; }

    private MessageId(Guid value)
    {
        Value = value;
    }

    public static MessageId CreateUnique()
    {
        return new MessageId(Guid.NewGuid());
    }

    public static MessageId Create(Guid value)
    {
        return new MessageId(value);
    }

    public static Result<MessageId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<MessageId>(SupportTicketErrors.InvalidMessageId(value));
        }

        return Result.Success(new MessageId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private MessageId()
    {
    }
#pragma warning restore CS8618
}
