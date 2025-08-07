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
public class TeamCartExpirationTests
{
    [Test]
    public void IsExpired_WithFutureDeadline_ShouldReturnFalse()
    {
        // Arrange
        var teamCart = CreateValidTeamCart(); // Uses DefaultDeadline which is in the future

        // Act & Assert
        teamCart.IsExpired().Should().BeFalse();
    }

    [Test]
    public void IsExpired_WithPastDeadline_ShouldReturnTrue()
    {
        // Arrange
        var expiredTeamCart = CreateExpiredTeamCart();

        // Act & Assert
        expiredTeamCart.IsExpired().Should().BeTrue();
    }

    [Test]
    public void MarkAsExpired_WithValidCart_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();

        // Act
        var result = teamCart.MarkAsExpired();

        // Assert
        result.ShouldBeSuccessful();
        teamCart.Status.Should().Be(TeamCartStatus.Expired);
        
        // Verify domain event
        teamCart.DomainEvents.Should().Contain(e => e.GetType() == typeof(TeamCartExpired));
        var expiredEvent = teamCart.DomainEvents.OfType<TeamCartExpired>().Single();
        expiredEvent.TeamCartId.Should().Be(teamCart.Id);
    }

    [Test]
    public void SetDeadline_AsHost_ShouldSucceedAndUpdateDeadline()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();
        var newDeadline = DateTime.UtcNow.AddHours(5);

        // Act
        var result = teamCart.SetDeadline(DefaultHostUserId, newDeadline);

        // Assert
        result.IsSuccess.Should().BeTrue();
        teamCart.Deadline.Should().Be(newDeadline);
        // ExpiresAt is not updated by SetDeadline, it's set only during creation
        // and remains at the default value (24 hours from creation)
    }

    [Test]
    public void SetDeadline_AsGuest_ShouldFailWithNotHostError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithGuest();
        var guestUserId = UserId.CreateUnique(); // A user who is not the host
        var newDeadline = DateTime.UtcNow.AddHours(5);

        // Act
        var result = teamCart.SetDeadline(guestUserId, newDeadline);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(TeamCartErrors.OnlyHostCanSetDeadline);
    }

    [Test]
    public void SetDeadline_WithPastDeadline_ShouldFailWithInvalidDeadlineError()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();
        var pastDeadline = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = teamCart.SetDeadline(DefaultHostUserId, pastDeadline);

        // Assert
        result.ShouldBeFailure(TeamCartErrors.DeadlineInPast.Code);
    }

    [Test]
    public void SetDeadline_WhenCartIsNotOpen_ShouldFail()
    {
        // Arrange
        var teamCart = CreateTeamCartWithGuest();
        
        // First add an item to the cart so we can lock it
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        teamCart.AddItem(
            DefaultHostUserId, 
            menuItemId, 
            menuCategoryId, 
            "Test Item", 
            new Money(10.99m, "USD"), 
            1).ShouldBeSuccessful();
        teamCart.ClearDomainEvents(); // Clear events from adding item
        
        teamCart.LockForPayment(DefaultHostUserId).ShouldBeSuccessful(); // Transition to Locked status
        teamCart.ClearDomainEvents(); // Clear events from locking

        var newDeadline = DateTime.UtcNow.AddHours(5);

        // Act
        var result = teamCart.SetDeadline(DefaultHostUserId, newDeadline);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotModifyCartOnceLocked);
    }
}
