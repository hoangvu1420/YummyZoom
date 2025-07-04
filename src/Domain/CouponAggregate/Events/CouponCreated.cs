using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.CouponAggregate.Events;

/// <summary>
/// Domain event raised when a new coupon is created
/// </summary>
public record CouponCreated(
    CouponId CouponId,
    RestaurantId RestaurantId,
    string Code,
    CouponType Type,
    DateTime ValidityStartDate,
    DateTime ValidityEndDate) : IDomainEvent;
