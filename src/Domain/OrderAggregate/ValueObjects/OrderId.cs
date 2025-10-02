using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate.ValueObjects;

public sealed class OrderId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private OrderId(Guid value)
    {
        Value = value;
    }

    public static OrderId CreateUnique()
    {
        return new OrderId(Guid.NewGuid());
    }

    public static OrderId Create(Guid value)
    {
        return new OrderId(value);
    }

    public static Result<OrderId> Create(string value)
    {
        return !Guid.TryParse(value, out var guid)
            ? Result.Failure<OrderId>(OrderErrors.InvalidOrderId)
            : Result.Success(new OrderId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private OrderId() { }
#pragma warning restore CS8618
}
