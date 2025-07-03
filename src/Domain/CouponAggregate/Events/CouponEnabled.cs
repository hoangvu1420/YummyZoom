using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.CouponAggregate.ValueObjects;

namespace YummyZoom.Domain.CouponAggregate.Events;

/// <summary>
/// Domain event raised when a coupon is enabled
/// </summary>
public record CouponEnabled(
    CouponId CouponId,
    DateTime EnabledAt) : IDomainEvent;
