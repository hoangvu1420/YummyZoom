using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Domain.UnitTests.MenuItemAggregate;

[TestFixture]
public class MenuItemAvailabilityTests : MenuItemTestHelpers
{
    [Test]
    public void MarkAsUnavailable_WhenAvailable_ShouldMakeUnavailableAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem(isAvailable: true);
        item.ClearDomainEvents();

        // Act
        item.MarkAsUnavailable();

        // Assert
        item.IsAvailable.Should().BeFalse();
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAvailabilityChanged>()
            .Which.Should().Match<MenuItemAvailabilityChanged>(e => 
                e.MenuItemId == item.Id && e.IsAvailable == false);
    }

    [Test]
    public void MarkAsUnavailable_WhenAlreadyUnavailable_ShouldNotRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem(isAvailable: false);
        item.ClearDomainEvents();

        // Act
        item.MarkAsUnavailable();

        // Assert
        item.IsAvailable.Should().BeFalse();
        item.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void MarkAsAvailable_WhenUnavailable_ShouldMakeAvailableAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem(isAvailable: false);
        item.ClearDomainEvents();

        // Act
        item.MarkAsAvailable();

        // Assert
        item.IsAvailable.Should().BeTrue();
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAvailabilityChanged>()
            .Which.Should().Match<MenuItemAvailabilityChanged>(e => 
                e.MenuItemId == item.Id && e.IsAvailable == true);
    }

    [Test]
    public void MarkAsAvailable_WhenAlreadyAvailable_ShouldNotRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem(isAvailable: true);
        item.ClearDomainEvents();

        // Act
        item.MarkAsAvailable();

        // Assert
        item.IsAvailable.Should().BeTrue();
        item.DomainEvents.Should().BeEmpty();
    }
}

[TestFixture]
public class MenuItemCategoryAssignmentTests : MenuItemTestHelpers
{
    [Test]
    public void AssignToCategory_WithDifferentCategory_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var oldCategoryId = item.MenuCategoryId;
        var newCategoryId = MenuCategoryId.CreateUnique();
        item.ClearDomainEvents();

        // Act
        var result = item.AssignToCategory(newCategoryId);

        // Assert
        result.ShouldBeSuccessful();
        item.MenuCategoryId.Should().Be(newCategoryId);
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAssignedToCategory>()
            .Which.Should().Match<MenuItemAssignedToCategory>(e => 
                e.MenuItemId == item.Id && 
                e.OldCategoryId == oldCategoryId && 
                e.NewCategoryId == newCategoryId);
    }

    [Test]
    public void AssignToCategory_WithSameCategory_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var currentCategoryId = item.MenuCategoryId;
        item.ClearDomainEvents();

        // Act
        var result = item.AssignToCategory(currentCategoryId);

        // Assert
        result.ShouldBeSuccessful();
        item.MenuCategoryId.Should().Be(currentCategoryId);
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAssignedToCategory>();
    }
}
