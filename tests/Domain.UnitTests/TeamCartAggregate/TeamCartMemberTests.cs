using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Domain.UnitTests.TeamCartAggregate.TeamCartTestHelpers;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartMemberTests
{
    [Test]
    public void AddMember_WithValidInputs_ShouldSucceedAndAddMember()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();
        var guestUserId = UserId.CreateUnique();
        var guestName = "Guest User";

        // Act
        var result = teamCart.AddMember(guestUserId, guestName);

        // Assert
        result.ShouldBeSuccessful();
        
        // Verify member was added
        var members = teamCart.Members;
        
        members.Should().NotBeNull();
        members.Should().HaveCount(2); // Host + new guest
        
        var guestMember = members.FirstOrDefault(m => m.UserId == guestUserId);
        guestMember.Should().NotBeNull();
        guestMember!.Name.Should().Be(guestName);
        guestMember.Role.Should().Be(MemberRole.Guest);
        
        // Verify domain event
        teamCart.DomainEvents.Should().Contain(e => e.GetType() == typeof(MemberJoined));
        var memberJoinedEvent = teamCart.DomainEvents.OfType<MemberJoined>().Single(e => e.UserId == guestUserId);
        memberJoinedEvent.TeamCartId.Should().Be(teamCart.Id);
        memberJoinedEvent.UserId.Should().Be(guestUserId);
        memberJoinedEvent.Name.Should().Be(guestName);
    }

    [Test]
    public void AddMember_WithExistingMember_ShouldFailWithMemberAlreadyExistsError()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();
        
        // Act - Try to add the host user again
        var result = teamCart.AddMember(DefaultHostUserId, "Another Name");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.MemberAlreadyExists);
    }

    [Test]
    public void AddMember_WithEmptyName_ShouldFailWithInvalidNameError()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();
        var guestUserId = UserId.CreateUnique();
        var emptyName = string.Empty;

        // Act
        var result = teamCart.AddMember(guestUserId, emptyName);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.MemberNameRequired);
    }

    [Test]
    public void AddMember_WhenCartIsNotOpen_ShouldFailWithCannotModifyCartOnceLockedError()
    {
        // Arrange
        var guestUserId = UserId.CreateUnique();
        var guestName = "Guest User";

        // Test when cart is Locked
        var teamCartLocked = CreateValidTeamCart();
        
        // First add an item to the cart so we can lock it
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        teamCartLocked.AddItem(
            DefaultHostUserId, 
            menuItemId, 
            menuCategoryId, 
            "Test Item", 
            new Money(10.99m, "USD"), 
            1).ShouldBeSuccessful();
        teamCartLocked.ClearDomainEvents(); // Clear events from adding item
        
        teamCartLocked.LockForPayment(DefaultHostUserId).ShouldBeSuccessful();
        teamCartLocked.ClearDomainEvents();
        var resultLocked = teamCartLocked.AddMember(guestUserId, guestName);
        resultLocked.ShouldBeFailure();
        resultLocked.Error.Should().Be(TeamCartErrors.CannotModifyCartOnceLocked);

        // Test when cart is ReadyToConfirm
        var teamCartReady = CreateTeamCartReadyForConversion();
        teamCartReady.ClearDomainEvents();
        var resultReady = teamCartReady.AddMember(guestUserId, guestName);
        resultReady.ShouldBeFailure();
        resultReady.Error.Should().Be(TeamCartErrors.CannotModifyCartOnceLocked);

        // Test when cart is Converted
        var teamCartConverted = CreateConvertedTeamCart();
        teamCartConverted.ClearDomainEvents();
        var resultConverted = teamCartConverted.AddMember(guestUserId, guestName);
        resultConverted.ShouldBeFailure();
        resultConverted.Error.Should().Be(TeamCartErrors.CannotModifyCartOnceLocked);

        // Test when cart is Expired
        var teamCartExpired = CreateExpiredTeamCart();
        teamCartExpired.ClearDomainEvents();
        var resultExpired = teamCartExpired.AddMember(guestUserId, guestName);
        resultExpired.ShouldBeFailure();
        resultExpired.Error.Should().Be(TeamCartErrors.CannotModifyCartOnceLocked);
    }
}
