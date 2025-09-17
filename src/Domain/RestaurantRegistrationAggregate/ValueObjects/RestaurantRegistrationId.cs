using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;

public sealed class RestaurantRegistrationId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private RestaurantRegistrationId(Guid value)
    {
        Value = value;
    }

    public static RestaurantRegistrationId CreateUnique()
        => new RestaurantRegistrationId(Guid.NewGuid());

    public static RestaurantRegistrationId Create(Guid value)
        => new RestaurantRegistrationId(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private RestaurantRegistrationId() { }
#pragma warning restore CS8618
}
