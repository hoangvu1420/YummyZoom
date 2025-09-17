using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantRegistrationAggregate.Events;

public sealed record RegistrationApproved(
    RestaurantRegistrationId RegistrationId,
    UserId SubmitterUserId,
    UserId ReviewerUserId,
    Guid RestaurantId
) : DomainEventBase;

