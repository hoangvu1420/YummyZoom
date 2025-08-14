using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Events;

public record PayoutSettled(
    RestaurantAccountId RestaurantAccountId,
    Money PayoutAmount,
    Money NewBalance) : DomainEventBase;
