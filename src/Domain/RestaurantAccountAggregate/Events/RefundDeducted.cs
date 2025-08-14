using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Events;

public record RefundDeducted(
    RestaurantAccountId RestaurantAccountId,
    OrderId RelatedOrderId,
    Money Amount) : DomainEventBase;
