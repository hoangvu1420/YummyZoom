using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetFullMenu;

public sealed record GetFullMenuQuery(Guid RestaurantId) : IRequest<Result<GetFullMenuResponse>>;

public sealed record GetFullMenuResponse(string MenuJson, DateTimeOffset LastRebuiltAt);

public static class GetFullMenuErrors
{
    public static Error NotFound => Error.NotFound(
        "Public.GetFullMenu.NotFound", "Menu view for the restaurant was not found.");
}
