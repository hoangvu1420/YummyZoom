using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Events;

public record PayoutMethodUpdated(
    RestaurantAccountId RestaurantAccountId,
    PayoutMethodDetails NewPayoutMethod) : DomainEventBase;
