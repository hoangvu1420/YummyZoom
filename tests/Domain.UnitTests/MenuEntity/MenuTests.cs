using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuEntity;

/// <summary>
/// Tests for core Menu entity functionality including creation, updates, and lifecycle management.
/// </summary>
[TestFixture]
public class MenuTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultName = "Test Menu";
    private const string DefaultDescription = "Test Description";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeMenuCorrectly()
    {
        // Arrange & Act
        var result = Menu.Create(
            DefaultRestaurantId,
            DefaultName,
            DefaultDescription);

        // Assert
        result.ShouldBeSuccessful();
        var menu = result.Value;
        
        menu.Id.Value.Should().NotBe(Guid.Empty);
        menu.RestaurantId.Should().Be(DefaultRestaurantId);
        menu.Name.Should().Be(DefaultName);
        menu.Description.Should().Be(DefaultDescription);
        menu.IsEnabled.Should().BeTrue();
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuCreated>()
            .Which.Should().Match<MenuCreated>(e => 
                e.MenuId == menu.Id && e.RestaurantId == DefaultRestaurantId);
    }

    [Test]
    public void Create_WithValidInputsAndDisabled_ShouldCreateDisabledMenu()
    {
        // Arrange & Act
        var result = Menu.Create(
            DefaultRestaurantId,
            DefaultName,
            DefaultDescription,
            isEnabled: false);

        // Assert
        result.ShouldBeSuccessful();
        var menu = result.Value;
        
        menu.IsEnabled.Should().BeFalse();
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuCreated>();
    }

    [Test]
    public void Create_WithNullOrEmptyName_ShouldFail()
    {
        // Act & Assert - Null name
        var nullResult = Menu.Create(DefaultRestaurantId, null!, DefaultDescription);
        nullResult.ShouldBeFailure("Menu.InvalidMenuName");

        // Act & Assert - Empty name
        var emptyResult = Menu.Create(DefaultRestaurantId, "", DefaultDescription);
        emptyResult.ShouldBeFailure("Menu.InvalidMenuName");

        // Act & Assert - Whitespace name
        var whitespaceResult = Menu.Create(DefaultRestaurantId, "   ", DefaultDescription);
        whitespaceResult.ShouldBeFailure("Menu.InvalidMenuName");
    }

    [Test]
    public void Create_WithNullOrEmptyDescription_ShouldFail()
    {
        // Act & Assert - Null description
        var nullResult = Menu.Create(DefaultRestaurantId, DefaultName, null!);
        nullResult.ShouldBeFailure("Menu.InvalidMenuDescription");

        // Act & Assert - Empty description
        var emptyResult = Menu.Create(DefaultRestaurantId, DefaultName, "");
        emptyResult.ShouldBeFailure("Menu.InvalidMenuDescription");

        // Act & Assert - Whitespace description
        var whitespaceResult = Menu.Create(DefaultRestaurantId, DefaultName, "   ");
        whitespaceResult.ShouldBeFailure("Menu.InvalidMenuDescription");
    }

    #endregion

    #region UpdateDetails() Method Tests

    [Test]
    public void UpdateDetails_WithValidInputs_ShouldSucceedAndUpdateProperties()
    {
        // Arrange
        var menu = CreateValidMenu();
        var newName = "Updated Menu Name";
        var newDescription = "Updated Menu Description";

        // Act
        var result = menu.UpdateDetails(newName, newDescription);

        // Assert
        result.ShouldBeSuccessful();
        menu.Name.Should().Be(newName);
        menu.Description.Should().Be(newDescription);
    }

    [Test]
    public void UpdateDetails_WithNullOrEmptyName_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();

        // Act & Assert - Null name
        var nullResult = menu.UpdateDetails(null!, DefaultDescription);
        nullResult.ShouldBeFailure("Menu.InvalidMenuName");

        // Act & Assert - Empty name
        var emptyResult = menu.UpdateDetails("", DefaultDescription);
        emptyResult.ShouldBeFailure("Menu.InvalidMenuName");

        // Act & Assert - Whitespace name
        var whitespaceResult = menu.UpdateDetails("   ", DefaultDescription);
        whitespaceResult.ShouldBeFailure("Menu.InvalidMenuName");
    }

    [Test]
    public void UpdateDetails_WithNullOrEmptyDescription_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();

        // Act & Assert - Null description
        var nullResult = menu.UpdateDetails(DefaultName, null!);
        nullResult.ShouldBeFailure("Menu.InvalidMenuDescription");

        // Act & Assert - Empty description
        var emptyResult = menu.UpdateDetails(DefaultName, "");
        emptyResult.ShouldBeFailure("Menu.InvalidMenuDescription");

        // Act & Assert - Whitespace description
        var whitespaceResult = menu.UpdateDetails(DefaultName, "   ");
        whitespaceResult.ShouldBeFailure("Menu.InvalidMenuDescription");
    }

    [Test]
    public void UpdateDetails_ShouldNotChangeName_WhenDescriptionIsInvalid()
    {
        // Arrange
        var menu = CreateValidMenu();
        var originalName = menu.Name;
        var originalDescription = menu.Description;

        // Act
        var result = menu.UpdateDetails("New Name", "");

        // Assert
        result.ShouldBeFailure("Menu.InvalidMenuDescription");
        menu.Name.Should().Be(originalName);
        menu.Description.Should().Be(originalDescription);
    }

    #endregion

    #region Enable() Method Tests

    [Test]
    public void Enable_WhenDisabled_ShouldEnableAndRaiseDomainEvent()
    {
        // Arrange
        var menu = CreateValidMenu(isEnabled: false);
        menu.ClearDomainEvents();

        // Act
        menu.Enable();

        // Assert
        menu.IsEnabled.Should().BeTrue();
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuEnabled>()
            .Which.Should().Match<MenuEnabled>(e => e.MenuId == menu.Id);
    }

    [Test]
    public void Enable_WhenAlreadyEnabled_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var menu = CreateValidMenu(isEnabled: true);
        menu.ClearDomainEvents();

        // Act
        menu.Enable();

        // Assert
        menu.IsEnabled.Should().BeTrue();
        menu.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Disable() Method Tests

    [Test]
    public void Disable_WhenEnabled_ShouldDisableAndRaiseDomainEvent()
    {
        // Arrange
        var menu = CreateValidMenu(isEnabled: true);
        menu.ClearDomainEvents();

        // Act
        menu.Disable();

        // Assert
        menu.IsEnabled.Should().BeFalse();
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuDisabled>()
            .Which.Should().Match<MenuDisabled>(e => e.MenuId == menu.Id);
    }

    [Test]
    public void Disable_WhenAlreadyDisabled_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var menu = CreateValidMenu(isEnabled: false);
        menu.ClearDomainEvents();

        // Act
        menu.Disable();

        // Assert
        menu.IsEnabled.Should().BeFalse();
        menu.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static Menu CreateValidMenu(bool isEnabled = true)
    {
        return Menu.Create(DefaultRestaurantId, DefaultName, DefaultDescription, isEnabled).Value;
    }

    #endregion
}
