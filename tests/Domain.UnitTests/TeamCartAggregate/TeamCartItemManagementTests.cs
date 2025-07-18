using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartItemManagementTests
{
    private static readonly UserId DefaultHostUserId = UserId.CreateUnique();
    private static readonly UserId DefaultGuestUserId = UserId.CreateUnique();
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private static readonly MenuItemId DefaultMenuItemId = MenuItemId.CreateUnique();
    private static readonly MenuCategoryId DefaultMenuCategoryId = MenuCategoryId.CreateUnique();
    private const string DefaultHostName = "Host User";
    private const string DefaultGuestName = "Guest User";
    private const string DefaultItemName = "Margherita Pizza";
    private static readonly Money DefaultBasePrice = new Money(12.99m, "USD");
    private const int DefaultQuantity = 2;

    private TeamCart CreateTeamCartWithMembers()
    {
        var teamCart = TeamCart.Create(
            DefaultHostUserId,
            DefaultRestaurantId,
            DefaultHostName).Value;

        // Add a guest member
        teamCart.AddMember(DefaultGuestUserId, DefaultGuestName);
        
        // Clear domain events from setup
        teamCart.ClearDomainEvents();
        
        return teamCart;
    }

    [Test]
    public void AddItem_WithValidInputsAsHost_ShouldSucceedAndRaiseDomainEvent()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        teamCart.Items.Should().HaveCount(1);
        
        var addedItem = teamCart.Items.First();
        addedItem.AddedByUserId.Should().Be(DefaultHostUserId);
        addedItem.Snapshot_MenuItemId.Should().Be(DefaultMenuItemId);
        addedItem.Snapshot_ItemName.Should().Be(DefaultItemName);
        addedItem.Quantity.Should().Be(DefaultQuantity);

        // Verify domain event
        teamCart.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(ItemAddedToTeamCart));
        var itemAddedEvent = teamCart.DomainEvents.OfType<ItemAddedToTeamCart>().Single();
        itemAddedEvent.TeamCartId.Should().Be(teamCart.Id);
        itemAddedEvent.TeamCartItemId.Should().Be(addedItem.Id);
        itemAddedEvent.AddedByUserId.Should().Be(DefaultHostUserId);
        itemAddedEvent.MenuItemId.Should().Be(DefaultMenuItemId);
        itemAddedEvent.Quantity.Should().Be(DefaultQuantity);
    }

    [Test]
    public void AddItem_WithValidInputsAsGuest_ShouldSucceedAndRaiseDomainEvent()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();

        // Act
        var result = teamCart.AddItem(
            DefaultGuestUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        teamCart.Items.Should().HaveCount(1);
        
        var addedItem = teamCart.Items.First();
        addedItem.AddedByUserId.Should().Be(DefaultGuestUserId);

        // Verify domain event
        teamCart.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(ItemAddedToTeamCart));
        var itemAddedEvent = teamCart.DomainEvents.OfType<ItemAddedToTeamCart>().Single();
        itemAddedEvent.AddedByUserId.Should().Be(DefaultGuestUserId);
    }

    [Test]
    public void AddItem_WithCustomizations_ShouldSucceedAndIncludeCustomizations()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();
        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Size", "Large", new Money(2.00m, "USD")).Value,
            TeamCartItemCustomization.Create("Toppings", "Extra Cheese", new Money(1.50m, "USD")).Value
        };

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity,
            customizations);

        // Assert
        result.IsSuccess.Should().BeTrue();
        teamCart.Items.Should().HaveCount(1);
        
        var addedItem = teamCart.Items.First();
        addedItem.SelectedCustomizations.Should().HaveCount(2);
        addedItem.SelectedCustomizations.Should().Contain(customizations);
    }

    [Test]
    public void AddItem_MultipleItems_ShouldAccumulateItems()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();

        // Act
        var result1 = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            "Pizza",
            DefaultBasePrice,
            1);

        var result2 = teamCart.AddItem(
            DefaultGuestUserId,
            MenuItemId.CreateUnique(),
            DefaultMenuCategoryId,
            "Burger",
            new Money(8.99m, "USD"),
            2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        teamCart.Items.Should().HaveCount(2);
        
        teamCart.Items.Should().Contain(item => item.Snapshot_ItemName == "Pizza");
        teamCart.Items.Should().Contain(item => item.Snapshot_ItemName == "Burger");
    }

    [Test]
    public void AddItem_WhenCartIsNotOpen_ShouldFailWithCannotAddItemsError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();
        // Force cart to expired status
        teamCart.MarkAsExpired();
        teamCart.ClearDomainEvents();

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.CannotAddItemsToClosedCart);
        teamCart.Items.Should().BeEmpty();
        teamCart.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void AddItem_ByNonMember_ShouldFailWithUserNotMemberError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();
        var nonMemberUserId = UserId.CreateUnique();

        // Act
        var result = teamCart.AddItem(
            nonMemberUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.UserNotMember);
        teamCart.Items.Should().BeEmpty();
        teamCart.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void AddItem_WithInvalidQuantity_ShouldFailWithInvalidQuantityError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            0); // Invalid quantity

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.InvalidQuantity);
        teamCart.Items.Should().BeEmpty();
        teamCart.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void AddItem_WithNullMenuItemId_ShouldFailWithMenuItemRequiredError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            null!,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.MenuItemRequired);
        teamCart.Items.Should().BeEmpty();
        teamCart.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void AddItem_WithEmptyItemName_ShouldFailWithItemNameRequiredError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            "", // Empty item name
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.ItemNameRequired);
        teamCart.Items.Should().BeEmpty();
        teamCart.DomainEvents.Should().BeEmpty();
    }
}
