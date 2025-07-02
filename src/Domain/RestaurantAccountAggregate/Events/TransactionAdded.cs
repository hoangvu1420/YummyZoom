using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Enums;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Events;

public record TransactionAdded(
    RestaurantAccountId RestaurantAccountId,
    AccountTransactionId TransactionId,
    TransactionType Type,
    Money Amount,
    OrderId? RelatedOrderId) : IDomainEvent;
