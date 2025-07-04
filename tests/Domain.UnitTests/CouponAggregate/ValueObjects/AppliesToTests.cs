using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.MenuAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CouponAggregate.ValueObjects;

[TestFixture]
public class AppliesToTests
{
    #region CreateForWholeOrder() Method Tests

    [Test]
    public void CreateForWholeOrder_ShouldSucceedAndSetCorrectProperties()
    {
        // Arrange & Act
        var result = AppliesTo.CreateForWholeOrder();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var appliesTo = result.Value;
        
        appliesTo.Scope.Should().Be(CouponScope.WholeOrder);
        appliesTo.ItemIds.Should().BeEmpty();
        appliesTo.CategoryIds.Should().BeEmpty();
    }

    [Test]
    public void CreateForWholeOrder_GetDisplayText_ShouldReturnCorrectValue()
    {
        // Arrange
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var displayText = appliesTo.GetDisplayText();

        // Assert
        displayText.Should().Be("Entire order");
    }

    #endregion

    #region CreateForSpecificItems() Method Tests

    [Test]
    public void CreateForSpecificItems_WithValidItemIds_ShouldSucceedAndSetCorrectProperties()
    {
        // Arrange
        var itemIds = new List<MenuItemId>
        {
            MenuItemId.CreateUnique(),
            MenuItemId.CreateUnique(),
            MenuItemId.CreateUnique()
        };

        // Act
        var result = AppliesTo.CreateForSpecificItems(itemIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var appliesTo = result.Value;
        
        appliesTo.Scope.Should().Be(CouponScope.SpecificItems);
        appliesTo.ItemIds.Should().HaveCount(3);
        appliesTo.ItemIds.Should().BeEquivalentTo(itemIds);
        appliesTo.CategoryIds.Should().BeEmpty();
    }

    [Test]
    public void CreateForSpecificItems_WithSingleItem_ShouldSucceed()
    {
        // Arrange
        var itemIds = new List<MenuItemId> { MenuItemId.CreateUnique() };

        // Act
        var result = AppliesTo.CreateForSpecificItems(itemIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ItemIds.Should().HaveCount(1);
        result.Value.ItemIds.First().Should().Be(itemIds.First());
    }

    [Test]
    public void CreateForSpecificItems_WithEmptyList_ShouldFailWithValidationError()
    {
        // Arrange
        var emptyItemIds = new List<MenuItemId>();

        // Act
        var result = AppliesTo.CreateForSpecificItems(emptyItemIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.EmptyItemIds);
    }

    [Test]
    public void CreateForSpecificItems_WithNullList_ShouldFailWithValidationError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = AppliesTo.CreateForSpecificItems(null);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.EmptyItemIds);
    }

    [Test]
    public void CreateForSpecificItems_WithDuplicateItemIds_ShouldFailWithValidationError()
    {
        // Arrange
        var itemId = MenuItemId.CreateUnique();
        var itemIds = new List<MenuItemId> { itemId, itemId, MenuItemId.CreateUnique() };

        // Act
        var result = AppliesTo.CreateForSpecificItems(itemIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.DuplicateItemIds);
    }

    [Test]
    public void CreateForSpecificItems_GetDisplayText_ShouldReturnCorrectValue()
    {
        // Arrange
        var itemIds = new List<MenuItemId>
        {
            MenuItemId.CreateUnique(),
            MenuItemId.CreateUnique()
        };
        var appliesTo = AppliesTo.CreateForSpecificItems(itemIds).Value;

        // Act
        var displayText = appliesTo.GetDisplayText();

        // Assert
        displayText.Should().Be("Specific items (2 items)");
    }

    #endregion

    #region CreateForSpecificCategories() Method Tests

    [Test]
    public void CreateForSpecificCategories_WithValidCategoryIds_ShouldSucceedAndSetCorrectProperties()
    {
        // Arrange
        var categoryIds = new List<MenuCategoryId>
        {
            MenuCategoryId.CreateUnique(),
            MenuCategoryId.CreateUnique(),
            MenuCategoryId.CreateUnique()
        };

        // Act
        var result = AppliesTo.CreateForSpecificCategories(categoryIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var appliesTo = result.Value;
        
        appliesTo.Scope.Should().Be(CouponScope.SpecificCategories);
        appliesTo.CategoryIds.Should().HaveCount(3);
        appliesTo.CategoryIds.Should().BeEquivalentTo(categoryIds);
        appliesTo.ItemIds.Should().BeEmpty();
    }

    [Test]
    public void CreateForSpecificCategories_WithSingleCategory_ShouldSucceed()
    {
        // Arrange
        var categoryIds = new List<MenuCategoryId> { MenuCategoryId.CreateUnique() };

        // Act
        var result = AppliesTo.CreateForSpecificCategories(categoryIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryIds.Should().HaveCount(1);
        result.Value.CategoryIds.First().Should().Be(categoryIds.First());
    }

    [Test]
    public void CreateForSpecificCategories_WithEmptyList_ShouldFailWithValidationError()
    {
        // Arrange
        var emptyCategoryIds = new List<MenuCategoryId>();

        // Act
        var result = AppliesTo.CreateForSpecificCategories(emptyCategoryIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.EmptyCategoryIds);
    }

    [Test]
    public void CreateForSpecificCategories_WithNullList_ShouldFailWithValidationError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = AppliesTo.CreateForSpecificCategories(null);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.EmptyCategoryIds);
    }

    [Test]
    public void CreateForSpecificCategories_WithDuplicateCategoryIds_ShouldFailWithValidationError()
    {
        // Arrange
        var categoryId = MenuCategoryId.CreateUnique();
        var categoryIds = new List<MenuCategoryId> { categoryId, categoryId, MenuCategoryId.CreateUnique() };

        // Act
        var result = AppliesTo.CreateForSpecificCategories(categoryIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.DuplicateCategoryIds);
    }

    [Test]
    public void CreateForSpecificCategories_GetDisplayText_ShouldReturnCorrectValue()
    {
        // Arrange
        var categoryIds = new List<MenuCategoryId>
        {
            MenuCategoryId.CreateUnique(),
            MenuCategoryId.CreateUnique(),
            MenuCategoryId.CreateUnique()
        };
        var appliesTo = AppliesTo.CreateForSpecificCategories(categoryIds).Value;

        // Act
        var displayText = appliesTo.GetDisplayText();

        // Assert
        displayText.Should().Be("Specific categories (3 categories)");
    }

    #endregion

    #region AppliesToItem() Method Tests

    [Test]
    public void AppliesToItem_WithWholeOrderScope_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        var menuItemId = MenuItemId.CreateUnique();
        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = appliesTo.AppliesToItem(menuItemId, categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithSpecificItemsScope_WhenItemMatches_ShouldReturnTrue()
    {
        // Arrange
        var targetItemId = MenuItemId.CreateUnique();
        var otherItemId = MenuItemId.CreateUnique();
        var itemIds = new List<MenuItemId> { targetItemId, otherItemId };
        var appliesTo = AppliesTo.CreateForSpecificItems(itemIds).Value;
        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = appliesTo.AppliesToItem(targetItemId, categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithSpecificItemsScope_WhenItemDoesNotMatch_ShouldReturnFalse()
    {
        // Arrange
        var targetItemId = MenuItemId.CreateUnique();
        var differentItemId = MenuItemId.CreateUnique();
        var itemIds = new List<MenuItemId> { targetItemId };
        var appliesTo = AppliesTo.CreateForSpecificItems(itemIds).Value;
        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = appliesTo.AppliesToItem(differentItemId, categoryId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void AppliesToItem_WithSpecificCategoriesScope_WhenCategoryMatches_ShouldReturnTrue()
    {
        // Arrange
        var targetCategoryId = MenuCategoryId.CreateUnique();
        var otherCategoryId = MenuCategoryId.CreateUnique();
        var categoryIds = new List<MenuCategoryId> { targetCategoryId, otherCategoryId };
        var appliesTo = AppliesTo.CreateForSpecificCategories(categoryIds).Value;
        var menuItemId = MenuItemId.CreateUnique();

        // Act
        var result = appliesTo.AppliesToItem(menuItemId, targetCategoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithSpecificCategoriesScope_WhenCategoryDoesNotMatch_ShouldReturnFalse()
    {
        // Arrange
        var targetCategoryId = MenuCategoryId.CreateUnique();
        var differentCategoryId = MenuCategoryId.CreateUnique();
        var categoryIds = new List<MenuCategoryId> { targetCategoryId };
        var appliesTo = AppliesTo.CreateForSpecificCategories(categoryIds).Value;
        var menuItemId = MenuItemId.CreateUnique();

        // Act
        var result = appliesTo.AppliesToItem(menuItemId, differentCategoryId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equality_WithSameWholeOrderScope_ShouldBeEqual()
    {
        // Arrange
        var appliesTo1 = AppliesTo.CreateForWholeOrder().Value;
        var appliesTo2 = AppliesTo.CreateForWholeOrder().Value;

        // Act & Assert
        appliesTo1.Should().Be(appliesTo2);
        appliesTo1.Equals(appliesTo2).Should().BeTrue();
        appliesTo1.GetHashCode().Should().Be(appliesTo2.GetHashCode());
    }

    [Test]
    public void Equality_WithSameSpecificItems_ShouldBeEqual()
    {
        // Arrange
        var itemIds = new List<MenuItemId>
        {
            MenuItemId.CreateUnique(),
            MenuItemId.CreateUnique()
        };
        var appliesTo1 = AppliesTo.CreateForSpecificItems(itemIds).Value;
        var appliesTo2 = AppliesTo.CreateForSpecificItems(itemIds).Value;

        // Act & Assert
        appliesTo1.Should().Be(appliesTo2);
        appliesTo1.Equals(appliesTo2).Should().BeTrue();
        appliesTo1.GetHashCode().Should().Be(appliesTo2.GetHashCode());
    }

    [Test]
    public void Equality_WithSameSpecificItemsInDifferentOrder_ShouldBeEqual()
    {
        // Arrange
        var itemId1 = MenuItemId.CreateUnique();
        var itemId2 = MenuItemId.CreateUnique();
        var itemIds1 = new List<MenuItemId> { itemId1, itemId2 };
        var itemIds2 = new List<MenuItemId> { itemId2, itemId1 };
        var appliesTo1 = AppliesTo.CreateForSpecificItems(itemIds1).Value;
        var appliesTo2 = AppliesTo.CreateForSpecificItems(itemIds2).Value;

        // Act & Assert
        appliesTo1.Should().Be(appliesTo2);
        appliesTo1.GetHashCode().Should().Be(appliesTo2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentScopes_ShouldNotBeEqual()
    {
        // Arrange
        var wholeOrderAppliesTo = AppliesTo.CreateForWholeOrder().Value;
        var specificItemsAppliesTo = AppliesTo.CreateForSpecificItems([MenuItemId.CreateUnique()]).Value;
        var specificCategoriesAppliesTo = AppliesTo.CreateForSpecificCategories([MenuCategoryId.CreateUnique()]).Value;

        // Act & Assert
        wholeOrderAppliesTo.Should().NotBe(specificItemsAppliesTo);
        wholeOrderAppliesTo.Should().NotBe(specificCategoriesAppliesTo);
        specificItemsAppliesTo.Should().NotBe(specificCategoriesAppliesTo);
    }

    [Test]
    public void Equality_WithDifferentItems_ShouldNotBeEqual()
    {
        // Arrange
        var itemIds1 = new List<MenuItemId> { MenuItemId.CreateUnique() };
        var itemIds2 = new List<MenuItemId> { MenuItemId.CreateUnique() };
        var appliesTo1 = AppliesTo.CreateForSpecificItems(itemIds1).Value;
        var appliesTo2 = AppliesTo.CreateForSpecificItems(itemIds2).Value;

        // Act & Assert
        appliesTo1.Should().NotBe(appliesTo2);
        appliesTo1.Equals(appliesTo2).Should().BeFalse();
    }

    [Test]
    public void Equality_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act & Assert
        appliesTo.Equals(null).Should().BeFalse();
    }

    #endregion
}
