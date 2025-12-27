using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.PayoutAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

namespace YummyZoom.Domain.PayoutAggregate.Events;

public record PayoutRequested(
    PayoutId PayoutId,
    RestaurantAccountId RestaurantAccountId,
    Money Amount) : DomainEventBase;
