
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.TagAggregate.ValueObjects;

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
    
    public static MenuItem Create(
        string name,
        string description,
        Money basePrice,
        string? imageUrl = null,
        bool isAvailable = true,
        List<TagId>? dietaryTagIds = null,
        List<AppliedCustomization>? appliedCustomizations = null)
    {
        return new MenuItem(
            MenuItemId.CreateUnique(),
            name,
            description,
            basePrice,
            isAvailable,
            imageUrl,
            dietaryTagIds ?? [],
            appliedCustomizations ?? []);
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

#pragma warning disable CS8618
    private MenuItem() { }
#pragma warning restore CS8618
}
