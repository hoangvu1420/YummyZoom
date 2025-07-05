using YummyZoom.Domain.CouponAggregate.ValueObjects;

namespace YummyZoom.Domain.CouponAggregate.Events;

public record CouponDeleted(CouponId CouponId) : IDomainEvent;
