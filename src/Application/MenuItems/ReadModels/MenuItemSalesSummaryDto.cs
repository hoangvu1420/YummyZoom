namespace YummyZoom.Application.MenuItems.ReadModels;

public sealed record MenuItemSalesSummaryDto(
    Guid RestaurantId,
    Guid MenuItemId,
    long LifetimeQuantity,
    long Rolling7DayQuantity,
    long Rolling30DayQuantity,
    DateTimeOffset? LastSoldAt,
    DateTimeOffset LastUpdatedAt);
