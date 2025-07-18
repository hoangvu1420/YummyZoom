using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartExpirationTests : TeamCartTestHelpers
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
        result.IsSuccess.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.DeadlineInPast);
    }

    [Test]
    public void SetDeadline_OnExpiredCart_ShouldFailWithCartExpiredError()
    {
        // Arrange
        var expiredTeamCart = CreateExpiredTeamCart();
        var newDeadline = DateTime.UtcNow.AddHours(5);

        // Act
        var result = expiredTeamCart.SetDeadline(DefaultHostUserId, newDeadline);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.CannotModifyClosedCart);
    }
}