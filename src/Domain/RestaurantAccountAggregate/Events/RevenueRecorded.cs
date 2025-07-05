using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Events;

public record RevenueRecorded(
    RestaurantAccountId RestaurantAccountId,
    OrderId RelatedOrderId,
    Money Amount) : IDomainEvent;
