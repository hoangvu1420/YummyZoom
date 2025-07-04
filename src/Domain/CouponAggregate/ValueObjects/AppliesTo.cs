using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.Domain.CouponAggregate.Errors;

namespace YummyZoom.Domain.CouponAggregate.ValueObjects;

/// <summary>
/// Represents the scope and specific items/categories a coupon applies to
/// </summary>
public sealed class AppliesTo : ValueObject
{
    public CouponScope Scope { get; private set; }
    public IReadOnlyList<MenuItemId> ItemIds { get; private set; }
    public IReadOnlyList<MenuCategoryId> CategoryIds { get; private set; }

    private AppliesTo(CouponScope scope, List<MenuItemId> itemIds, List<MenuCategoryId> categoryIds)
    {
        Scope = scope;
        ItemIds = itemIds.AsReadOnly();
        CategoryIds = categoryIds.AsReadOnly();
    }

    /// <summary>
    /// Creates an AppliesTo for whole order scope
    /// </summary>
    public static Result<AppliesTo> CreateForWholeOrder()
    {
        return Result.Success(new AppliesTo(CouponScope.WholeOrder, [], []));
    }

    /// <summary>
    /// Creates an AppliesTo for specific menu items
    /// </summary>
    /// <param name="itemIds">List of menu item IDs the coupon applies to</param>
    public static Result<AppliesTo> CreateForSpecificItems(List<MenuItemId> itemIds)
    {
        if (itemIds == null || itemIds.Count == 0)
        {
            return Result.Failure<AppliesTo>(CouponErrors.EmptyItemIds);
        }

        if (itemIds.Distinct().Count() != itemIds.Count)
        {
            return Result.Failure<AppliesTo>(CouponErrors.DuplicateItemIds);
        }

        return Result.Success(new AppliesTo(CouponScope.SpecificItems, itemIds, []));
    }

    /// <summary>
    /// Creates an AppliesTo for specific menu categories
    /// </summary>
    /// <param name="categoryIds">List of menu category IDs the coupon applies to</param>
    public static Result<AppliesTo> CreateForSpecificCategories(List<MenuCategoryId> categoryIds)
    {
        if (categoryIds == null || categoryIds.Count == 0)
        {
            return Result.Failure<AppliesTo>(CouponErrors.EmptyCategoryIds);
        }

        if (categoryIds.Distinct().Count() != categoryIds.Count)
        {
            return Result.Failure<AppliesTo>(CouponErrors.DuplicateCategoryIds);
        }

        return Result.Success(new AppliesTo(CouponScope.SpecificCategories, [], categoryIds));
    }

    /// <summary>
    /// Checks if the coupon applies to a specific menu item
    /// </summary>
    /// <param name="menuItemId">Menu item ID to check</param>
    /// <param name="categoryId">Category ID of the menu item</param>
    public bool AppliesToItem(MenuItemId menuItemId, MenuCategoryId categoryId)
    {
        return Scope switch
        {
            CouponScope.WholeOrder => true,
            CouponScope.SpecificItems => ItemIds.Contains(menuItemId),
            CouponScope.SpecificCategories => CategoryIds.Contains(categoryId),
            _ => false
        };
    }

    /// <summary>
    /// Gets display text describing what the coupon applies to
    /// </summary>
    public string GetDisplayText()
    {
        return Scope switch
        {
            CouponScope.WholeOrder => "Entire order",
            CouponScope.SpecificItems => $"Specific items ({ItemIds.Count} items)",
            CouponScope.SpecificCategories => $"Specific categories ({CategoryIds.Count} categories)",
            _ => "Unknown scope"
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Scope;
        
        // For collections, we need to ensure consistent ordering for equality
        foreach (var itemId in ItemIds.OrderBy(x => x.Value))
        {
            yield return itemId;
        }
        
        foreach (var categoryId in CategoryIds.OrderBy(x => x.Value))
        {
            yield return categoryId;
        }
    }

#pragma warning disable CS8618
    private AppliesTo() { }
#pragma warning restore CS8618
}
