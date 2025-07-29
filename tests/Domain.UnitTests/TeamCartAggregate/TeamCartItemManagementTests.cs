using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Domain.UnitTests.TeamCartAggregate.TeamCartTestHelpers;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartItemManagementTests
{
    private static readonly UserId DefaultGuestUserId = UserId.CreateUnique();
    private static readonly MenuItemId DefaultMenuItemId = MenuItemId.CreateUnique();
    private static readonly MenuCategoryId DefaultMenuCategoryId = MenuCategoryId.CreateUnique();
    private const string DefaultItemName = "Margherita Pizza";
    private static readonly Money DefaultBasePrice = new Money(12.99m, "USD");
    private const int DefaultQuantity = 2;

    private TeamCart CreateTeamCartWithMembers()
    {
        var teamCart = CreateValidTeamCart();

        // Add a guest member
        teamCart.AddMember(DefaultGuestUserId, DefaultGuestName).ShouldBeSuccessful();
        
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
        result.ShouldBeSuccessful();
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
        result.ShouldBeSuccessful();
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
        result.ShouldBeSuccessful();
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
        result1.ShouldBeSuccessful();
        result2.ShouldBeSuccessful();
        teamCart.Items.Should().HaveCount(2);
        
        teamCart.Items.Should().Contain(item => item.Snapshot_ItemName == "Pizza");
        teamCart.Items.Should().Contain(item => item.Snapshot_ItemName == "Burger");
    }

    [Test]
    public void AddItem_WhenCartIsNotOpen_ShouldFailWithCannotModifyCartOnceLockedError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMembers();
        
        // First add an item to the cart so we can lock it
        var addResult = teamCart.AddItem(
            DefaultHostUserId, DefaultMenuItemId, DefaultMenuCategoryId, DefaultItemName, DefaultBasePrice, DefaultQuantity);
        addResult.ShouldBeSuccessful();
        teamCart.ClearDomainEvents(); // Clear events from adding item
        
        // Test when cart is Locked
        var lockResult = teamCart.LockForPayment(DefaultHostUserId);
        Console.WriteLine($"LockForPayment result: IsSuccess={lockResult.IsSuccess}, Error={(lockResult.IsFailure ? lockResult.Error.ToString() : "None")}");
        Console.WriteLine($"TeamCart Status: {teamCart.Status}");
        Console.WriteLine($"TeamCart Items Count: {teamCart.Items.Count}");
        Console.WriteLine($"TeamCart Host: {teamCart.HostUserId}");
        Console.WriteLine($"DefaultHostUserId: {DefaultHostUserId}");
        
        lockResult.ShouldBeSuccessful();
        teamCart.ClearDomainEvents(); // Clear events from locking
        
        // Try to add another item after locking
        var menuItemId2 = MenuItemId.CreateUnique();
        var resultLocked = teamCart.AddItem(
            DefaultHostUserId, menuItemId2, DefaultMenuCategoryId, "Another Item", DefaultBasePrice, DefaultQuantity);
        resultLocked.ShouldBeFailure(TeamCartErrors.CannotModifyCartOnceLocked.Code);
        teamCart.Items.Should().HaveCount(1); // Still has the original item
        teamCart.DomainEvents.Should().BeEmpty();

        // Test when cart is ReadyToConfirm
        var teamCartReady = CreateTeamCartReadyForConversion();
        teamCartReady.ClearDomainEvents();
        var resultReady = teamCartReady.AddItem(
            DefaultHostUserId, DefaultMenuItemId, DefaultMenuCategoryId, DefaultItemName, DefaultBasePrice, DefaultQuantity);
        resultReady.ShouldBeFailure();
        resultReady.Error.Should().Be(TeamCartErrors.CannotModifyCartOnceLocked);
        teamCartReady.Items.Should().NotBeEmpty(); // Items should exist from setup
        teamCartReady.DomainEvents.Should().BeEmpty();

        // Test when cart is Converted
        var teamCartConverted = CreateConvertedTeamCart();
        teamCartConverted.ClearDomainEvents();
        var resultConverted = teamCartConverted.AddItem(
            DefaultHostUserId, DefaultMenuItemId, DefaultMenuCategoryId, DefaultItemName, DefaultBasePrice, DefaultQuantity);
        resultConverted.ShouldBeFailure();
        resultConverted.Error.Should().Be(TeamCartErrors.CannotModifyCartOnceLocked);
        teamCartConverted.Items.Should().NotBeEmpty(); // Items should exist from setup
        teamCartConverted.DomainEvents.Should().BeEmpty();

        // Test when cart is Expired
        var teamCartExpired = CreateExpiredTeamCart();
        teamCartExpired.ClearDomainEvents();
        var resultExpired = teamCartExpired.AddItem(
            DefaultHostUserId, DefaultMenuItemId, DefaultMenuCategoryId, DefaultItemName, DefaultBasePrice, DefaultQuantity);
        resultExpired.ShouldBeFailure();
        resultExpired.ShouldBeFailure(TeamCartErrors.CannotModifyCartOnceLocked.Code);
        teamCartExpired.Items.Should().BeEmpty(); // No items added to expired cart initially
        teamCartExpired.DomainEvents.Should().BeEmpty();
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
        result.ShouldBeFailure(TeamCartErrors.UserNotMember.Code);
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
        result.ShouldBeFailure(TeamCartErrors.InvalidQuantity.Code);
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
        result.ShouldBeFailure(TeamCartErrors.MenuItemRequired.Code);
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
        result.ShouldBeFailure(TeamCartErrors.ItemNameRequired.Code);
        teamCart.Items.Should().BeEmpty();
        teamCart.DomainEvents.Should().BeEmpty();
    }
}
