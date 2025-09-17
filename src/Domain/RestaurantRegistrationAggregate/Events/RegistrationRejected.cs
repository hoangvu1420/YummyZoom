using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantRegistrationAggregate.Events;

public sealed record RegistrationRejected(
    RestaurantRegistrationId RegistrationId,
    UserId SubmitterUserId,
    UserId ReviewerUserId,
    string Reason
) : DomainEventBase;

