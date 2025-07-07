using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuItemAggregate;

public abstract class MenuItemTestHelpers
{
    protected static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    protected static readonly MenuCategoryId DefaultMenuCategoryId = MenuCategoryId.CreateUnique();
    protected const string DefaultItemName = "Test Item";
    protected const string DefaultItemDescription = "Test Item Description";
    protected static readonly Money DefaultBasePrice = new Money(12.99m, Currencies.Default);
    protected const string DefaultImageUrl = "https://example.com/image.jpg";

    protected static MenuItem CreateValidMenuItem(bool isAvailable = true)
    {
        return MenuItem.Create(
            DefaultRestaurantId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice,
            isAvailable: isAvailable).Value;
    }

    protected static MenuItem CreateValidMenuItem(string name, string description, Money price)
    {
        return MenuItem.Create(
            DefaultRestaurantId,
            DefaultMenuCategoryId,
            name,
            description,
            price).Value;
    }
}
