using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantRegistrationAggregate.Events;

public sealed record RegistrationSubmitted(
    RestaurantRegistrationId RegistrationId,
    UserId SubmitterUserId,
    string Name,
    string City
) : DomainEventBase;

