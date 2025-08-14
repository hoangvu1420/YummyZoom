using YummyZoom.Domain.CouponAggregate.ValueObjects;

namespace YummyZoom.Domain.CouponAggregate.Events;

/// <summary>
/// Domain event raised when a coupon is used (usage count incremented)
/// </summary>
public record CouponUsed(
    CouponId CouponId,
    int PreviousUsageCount,
    int NewUsageCount,
    DateTime UsedAt) : DomainEventBase;
