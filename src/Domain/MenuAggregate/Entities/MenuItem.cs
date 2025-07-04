using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.TagAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuAggregate.Entities;

public sealed class MenuItem : Entity<MenuItemId>
{
    private readonly List<TagId> _dietaryTagIds = [];
    private readonly List<AppliedCustomization> _appliedCustomizations = [];

    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money BasePrice { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsAvailable { get; private set; }

    public IReadOnlyList<TagId> DietaryTagIds => _dietaryTagIds.AsReadOnly();
    public IReadOnlyList<AppliedCustomization> AppliedCustomizations => _appliedCustomizations.AsReadOnly();

    private MenuItem(
        MenuItemId menuItemId,
        string name,
        string description,
        Money basePrice,
        bool isAvailable,
        string? imageUrl,
        List<TagId> dietaryTagIds,
        List<AppliedCustomization> appliedCustomizations) 
        : base(menuItemId)
    {
        Name = name;
        Description = description;
        BasePrice = basePrice;
        IsAvailable = isAvailable;
        ImageUrl = imageUrl;
        _dietaryTagIds = dietaryTagIds;
        _appliedCustomizations = appliedCustomizations;
    }
    
    public static Result<MenuItem> Create(
        string name,
        string description,
        Money basePrice,
        string? imageUrl = null,
        bool isAvailable = true,
        List<TagId>? dietaryTagIds = null,
        List<AppliedCustomization>? appliedCustomizations = null)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<MenuItem>(MenuErrors.InvalidItemName(name));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<MenuItem>(MenuErrors.InvalidItemDescription(description));

        // Business rule: Menu item prices must be positive
        if (basePrice.Amount <= 0)
        {
            return Result.Failure<MenuItem>(MenuErrors.NegativeMenuItemPrice);
        }

        var menuItem = new MenuItem(
            MenuItemId.CreateUnique(),
            name,
            description,
            basePrice,
            isAvailable,
            imageUrl,
            dietaryTagIds ?? [],
            appliedCustomizations ?? []);

        return Result.Success(menuItem);
    }

    public void MarkAsUnavailable()
    {
        if (!IsAvailable) return;
        IsAvailable = false;
    }

    public void MarkAsAvailable()
    {
        if (IsAvailable) return;
        IsAvailable = true;
    }

    public Result UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(MenuErrors.InvalidItemName(name));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(MenuErrors.InvalidItemDescription(description));

        Name = name;
        Description = description;
        return Result.Success();
    }

    public Result UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            return Result.Failure(MenuErrors.NegativeMenuItemPrice);

        BasePrice = newPrice;
        return Result.Success();
    }

    public void UpdateImageUrl(string? imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public Result UpdateFullDetails(string name, string description, Money basePrice, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(MenuErrors.InvalidItemName(name));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(MenuErrors.InvalidItemDescription(description));

        if (basePrice.Amount <= 0)
            return Result.Failure(MenuErrors.NegativeMenuItemPrice);

        Name = name;
        Description = description;
        BasePrice = basePrice;
        ImageUrl = imageUrl;
        return Result.Success();
    }

#pragma warning disable CS8618
    private MenuItem() { }
#pragma warning restore CS8618
}
