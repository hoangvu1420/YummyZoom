using FluentValidation;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Home.Queries.ActiveDeals;

public sealed record ActiveDealCardDto(
    Guid RestaurantId,
    string Name,
    string? LogoUrl,
    string BestCouponLabel
);

public sealed record ListActiveDealsQuery(int Limit = 10)
    : IRequest<Result<IReadOnlyList<ActiveDealCardDto>>>;

public sealed class ListActiveDealsQueryValidator : AbstractValidator<ListActiveDealsQuery>
{
    public ListActiveDealsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
    }
}
