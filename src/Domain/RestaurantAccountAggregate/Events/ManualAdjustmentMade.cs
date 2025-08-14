using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Events;

public record ManualAdjustmentMade(
    RestaurantAccountId RestaurantAccountId,
    Money Amount,
    string Reason,
    Guid AdminId) : DomainEventBase;
