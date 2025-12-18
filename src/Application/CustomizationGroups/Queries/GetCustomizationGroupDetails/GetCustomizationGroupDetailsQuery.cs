using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.CustomizationGroups.Queries.GetCustomizationGroupDetails;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetCustomizationGroupDetailsQuery(Guid RestaurantId, Guid GroupId) : IRequest<Result<CustomizationGroupDetailsDto>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record CustomizationGroupDetailsDto(
    Guid Id,
    string Name,
    int MinSelections,
    int MaxSelections,
    List<CustomizationChoiceDetailsDto> Choices);

public sealed record CustomizationChoiceDetailsDto(
    Guid Id,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    bool IsDefault,
    int DisplayOrder);

public static class CustomizationGroupErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "CustomizationGroup.NotFound", "The customization group was not found.");
}
