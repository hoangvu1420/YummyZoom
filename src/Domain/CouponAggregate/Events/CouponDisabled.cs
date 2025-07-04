using YummyZoom.Domain.CouponAggregate.ValueObjects;

namespace YummyZoom.Domain.CouponAggregate.Events;

/// <summary>
/// Domain event raised when a coupon is disabled
/// </summary>
public record CouponDisabled(
    CouponId CouponId,
    DateTime DisabledAt) : IDomainEvent;
