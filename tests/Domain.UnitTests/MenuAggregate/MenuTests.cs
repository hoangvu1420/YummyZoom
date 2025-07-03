using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate;
using YummyZoom.Domain.MenuAggregate.Entities;
using YummyZoom.Domain.MenuAggregate.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuAggregate;

[TestFixture]
public class MenuTests
{
    private const string DefaultName = "Test Menu";
    private const string DefaultDescription = "Test Description";

    private static RestaurantId CreateValidRestaurantId() => RestaurantId.CreateUnique();

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeMenuCorrectly()
    {
        // Arrange & Act
        var menu = MenuErrors.Create(CreateValidRestaurantId(), DefaultName, DefaultDescription);

        // Assert
        menu.Should().NotBeNull();
        menu.Id.Value.Should().NotBe(Guid.Empty);
        menu.Name.Should().Be(DefaultName);
        menu.Description.Should().Be(DefaultDescription);
        menu.IsEnabled.Should().BeTrue();
        menu.Categories.Should().BeEmpty();
        menu.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuCreated));
    }

    [Test]
    public void UpdateDetails_Should_UpdateMenuProperties()
    {
        // Arrange
        var menu = MenuErrors.Create(CreateValidRestaurantId(), DefaultName, DefaultDescription);
        var newName = "New Test Menu";
        var newDescription = "New Test Description";

        // Act
        menu.UpdateDetails(newName, newDescription);

        // Assert
        menu.Name.Should().Be(newName);
        menu.Description.Should().Be(newDescription);
    }

    [Test]
    public void Enable_Should_SetIsEnabledToTrue_And_RaiseMenuEnabledEvent()
    {
        // Arrange
        var menu = MenuErrors.Create(CreateValidRestaurantId(), DefaultName, DefaultDescription, isEnabled: false);

        // Act
        menu.Enable();

        // Assert
        menu.IsEnabled.Should().BeTrue();
        menu.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuEnabled));
    }

    [Test]
    public void Disable_Should_SetIsEnabledToFalse_And_RaiseMenuDisabledEvent()
    {
        // Arrange
        var menu = MenuErrors.Create(CreateValidRestaurantId(), DefaultName, DefaultDescription);

        // Act
        menu.Disable();

        // Assert
        menu.IsEnabled.Should().BeFalse();
        menu.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuDisabled));
    }

    [Test]
    public void CreateMenuCategory_WithValidInputs_ShouldSucceedAndInitializeMenuCategoryCorrectly()
    {
        // Arrange & Act
        var menuCategory = MenuCategory.Create("Test Category", 1);

        // Assert
        menuCategory.Should().NotBeNull();
        menuCategory.Id.Value.Should().NotBe(Guid.Empty);
        menuCategory.Name.Should().Be("Test Category");
        menuCategory.DisplayOrder.Should().Be(1);
        menuCategory.Items.Should().BeEmpty();
    }

    [Test]
    public void CreateMenuItem_WithValidInputs_ShouldSucceedAndInitializeMenuItemCorrectly()
    {
        // Arrange & Act
        var result = MenuItem.Create("Test Item", "Test Description", new Money(10.00m, Currencies.Default));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var menuItem = result.Value;
        menuItem.Id.Value.Should().NotBe(Guid.Empty);
        menuItem.Name.Should().Be("Test Item");
        menuItem.Description.Should().Be("Test Description");
        menuItem.BasePrice.Amount.Should().Be(10.00m);
        menuItem.IsAvailable.Should().BeTrue();
    }

    [Test]
    public void CreateMenuItem_WithNegativePrice_ShouldFail()
    {
        // Arrange & Act
        var result = MenuItem.Create("Test Item", "Test Description", new Money(-5.00m, Currencies.Default));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(YummyZoom.Domain.MenuAggregate.Errors.MenuErrors.NegativeMenuItemPrice);
    }

    [Test]
    public void CreateMenuItem_WithZeroPrice_ShouldFail()
    {
        // Arrange & Act
        var result = MenuItem.Create("Test Item", "Test Description", new Money(0m, Currencies.Default));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(YummyZoom.Domain.MenuAggregate.Errors.MenuErrors.NegativeMenuItemPrice);
    }

    [Test]
    public void MarkAsUnavailable_Should_SetIsAvailableToFalse()
    {
        // Arrange
        var result = MenuItem.Create("Test Item", "Test Description", new Money(10.00m, Currencies.Default));
        result.IsSuccess.Should().BeTrue();
        var menuItem = result.Value;

        // Act
        menuItem.MarkAsUnavailable();

        // Assert
        menuItem.IsAvailable.Should().BeFalse();
    }

    [Test]
    public void MarkAsAvailable_Should_SetIsAvailableToTrue()
    {
        // Arrange
        var result = MenuItem.Create("Test Item", "Test Description", new Money(10.00m, Currencies.Default), isAvailable: false);
        result.IsSuccess.Should().BeTrue();
        var menuItem = result.Value;

        // Act
        menuItem.MarkAsAvailable();

        // Assert
        menuItem.IsAvailable.Should().BeTrue();
    }
}
