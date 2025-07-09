using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.Errors;
using YummyZoom.Domain.MenuItemAggregate.Events;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate;

public sealed class MenuItem : AggregateRoot<MenuItemId, Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private readonly List<TagId> _dietaryTagIds = [];
    private readonly List<AppliedCustomization> _appliedCustomizations = [];

    public RestaurantId RestaurantId { get; private set; }
    public MenuCategoryId MenuCategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money BasePrice { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsAvailable { get; private set; }

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Properties from ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    public IReadOnlyList<TagId> DietaryTagIds => _dietaryTagIds.AsReadOnly();
    public IReadOnlyList<AppliedCustomization> AppliedCustomizations => _appliedCustomizations.AsReadOnly();

    private MenuItem(
        MenuItemId menuItemId,
        RestaurantId restaurantId,
        MenuCategoryId menuCategoryId,
        string name,
        string description,
        Money basePrice,
        bool isAvailable,
        string? imageUrl,
        List<TagId> dietaryTagIds,
        List<AppliedCustomization> appliedCustomizations)
        : base(menuItemId)
    {
        RestaurantId = restaurantId;
        MenuCategoryId = menuCategoryId;
        Name = name;
        Description = description;
        BasePrice = basePrice;
        IsAvailable = isAvailable;
        ImageUrl = imageUrl;
        _dietaryTagIds = new List<TagId>(dietaryTagIds);
        _appliedCustomizations = new List<AppliedCustomization>(appliedCustomizations);
    }

    public static Result<MenuItem> Create(
        RestaurantId restaurantId,
        MenuCategoryId menuCategoryId,
        string name,
        string description,
        Money basePrice,
        string? imageUrl = null,
        bool isAvailable = true,
        List<TagId>? dietaryTagIds = null,
        List<AppliedCustomization>? appliedCustomizations = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<MenuItem>(MenuItemErrors.InvalidName(name));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<MenuItem>(MenuItemErrors.InvalidDescription(description));

        if (basePrice.Amount <= 0)
            return Result.Failure<MenuItem>(MenuItemErrors.NegativePrice);

        var menuItem = new MenuItem(
            MenuItemId.CreateUnique(),
            restaurantId,
            menuCategoryId,
            name,
            description,
            basePrice,
            isAvailable,
            imageUrl,
            dietaryTagIds ?? [],
            appliedCustomizations ?? []);

        menuItem.AddDomainEvent(new MenuItemCreated((MenuItemId)menuItem.Id, menuItem.RestaurantId, menuItem.MenuCategoryId));
        
        return Result.Success(menuItem);
    }

    public void MarkAsUnavailable()
    {
        if (!IsAvailable) return;
        IsAvailable = false;
        AddDomainEvent(new MenuItemAvailabilityChanged((MenuItemId)Id, IsAvailable));
    }

    public void MarkAsAvailable()
    {
        if (IsAvailable) return;
        IsAvailable = true;
        AddDomainEvent(new MenuItemAvailabilityChanged((MenuItemId)Id, IsAvailable));
    }

    public Result UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(MenuItemErrors.InvalidName(name));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(MenuItemErrors.InvalidDescription(description));

        Name = name;
        Description = description;
        return Result.Success();
    }

    public Result UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            return Result.Failure(MenuItemErrors.NegativePrice);

        BasePrice = newPrice;
        AddDomainEvent(new MenuItemPriceChanged((MenuItemId)Id, newPrice));
        return Result.Success();
    }

    public Result AssignToCategory(MenuCategoryId newCategoryId)
    {
        var oldCategoryId = MenuCategoryId;
        MenuCategoryId = newCategoryId;
        AddDomainEvent(new MenuItemAssignedToCategory((MenuItemId)Id, oldCategoryId, newCategoryId));
        return Result.Success();
    }

    /// <summary>
    /// Marks this menu item as deleted. This is the single, authoritative way to delete this aggregate.
    /// </summary>
    /// <returns>A Result indicating success</returns>
    public Result MarkAsDeleted()
    {
        AddDomainEvent(new MenuItemDeleted((MenuItemId)Id));

        return Result.Success();
    }

    /// <summary>
    /// Assigns a customization group to this menu item if not already present.
    /// </summary>
    /// <param name="customization">The customization to assign</param>
    /// <returns>A Result indicating success or failure</returns>
    public Result AssignCustomizationGroup(AppliedCustomization customization)
    {
        if (customization is null)
            return Result.Failure(Error.Validation("MenuItem.InvalidCustomization", "Customization cannot be null."));

        // Check if customization group is already assigned
        if (_appliedCustomizations.Any(c => c.CustomizationGroupId == customization.CustomizationGroupId))
            return Result.Failure(MenuItemErrors.CustomizationAlreadyAssigned(customization.CustomizationGroupId.Value.ToString()));

        _appliedCustomizations.Add(customization);
        AddDomainEvent(new MenuItemCustomizationAssigned(
            (MenuItemId)Id, 
            customization.CustomizationGroupId, 
            customization.DisplayTitle));

        return Result.Success();
    }

    /// <summary>
    /// Removes a customization group from this menu item.
    /// </summary>
    /// <param name="groupId">The ID of the customization group to remove</param>
    /// <returns>A Result indicating success or failure</returns>
    public Result RemoveCustomizationGroup(CustomizationGroupId groupId)
    {
        var existingCustomization = _appliedCustomizations.FirstOrDefault(c => c.CustomizationGroupId == groupId);
        if (existingCustomization is null)
            return Result.Failure(MenuItemErrors.CustomizationNotFound(groupId.Value.ToString()));

        _appliedCustomizations.Remove(existingCustomization);
        AddDomainEvent(new MenuItemCustomizationRemoved((MenuItemId)Id, groupId));

        return Result.Success();
    }

    /// <summary>
    /// Sets the dietary tags for this menu item, replacing the entire list.
    /// </summary>
    /// <param name="tagIds">The list of tag IDs to set (can be null or empty)</param>
    /// <returns>A Result indicating success</returns>
    public Result SetDietaryTags(List<TagId>? tagIds)
    {
        _dietaryTagIds.Clear();
        
        if (tagIds != null && tagIds.Count > 0)
        {
            _dietaryTagIds.AddRange(tagIds);
        }

        AddDomainEvent(new MenuItemDietaryTagsUpdated((MenuItemId)Id, new List<TagId>(_dietaryTagIds)));

        return Result.Success();
    }

    /// <summary>
    /// Marks this menu item as deleted (soft delete).
    /// </summary>
    /// <param name="deletedOn">The timestamp when the item was deleted</param>
    /// <param name="deletedBy">The user who deleted the item</param>
    /// <returns>A Result indicating success or failure</returns>
    public Result MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy = null)
    {
        if (IsDeleted)
        {
            // Already deleted, consider this a success
            return Result.Success();
        }

        IsDeleted = true;
        DeletedOn = deletedOn;
        DeletedBy = deletedBy;

        AddDomainEvent(new MenuItemDeleted((MenuItemId)Id));
        
        return Result.Success();
    }

#pragma warning disable CS8618
    private MenuItem() { }
#pragma warning restore CS8618
}
