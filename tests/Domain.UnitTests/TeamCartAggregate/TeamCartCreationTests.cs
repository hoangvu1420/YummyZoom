using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartCreationTests : TeamCartTestHelpers
{
    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeTeamCartCorrectly()
    {
        // Arrange & Act
        var result = TeamCart.Create(
            DefaultHostUserId,
            DefaultRestaurantId,
            DefaultHostName,
            DefaultDeadline);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var teamCart = result.Value;
        
        teamCart.Id.Value.Should().NotBe(Guid.Empty);
        teamCart.HostUserId.Should().Be(DefaultHostUserId);
        teamCart.RestaurantId.Should().Be(DefaultRestaurantId);
        teamCart.Status.Should().Be(TeamCartStatus.Open);
        teamCart.ShareToken.Should().NotBeNull();
        teamCart.ShareToken.Value.Should().NotBeEmpty();
        teamCart.ShareToken.IsExpired.Should().BeFalse();
        teamCart.Deadline.Should().Be(DefaultDeadline);
        teamCart.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        teamCart.ExpiresAt.Should().BeCloseTo(DefaultDeadline, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        teamCart.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TeamCartCreated));
        var teamCartCreatedEvent = teamCart.DomainEvents.OfType<TeamCartCreated>().Single();
        teamCartCreatedEvent.TeamCartId.Should().Be(teamCart.Id);
        teamCartCreatedEvent.HostId.Should().Be(DefaultHostUserId);
        teamCartCreatedEvent.RestaurantId.Should().Be(DefaultRestaurantId);
    }

    [Test]
    public void Create_WithoutDeadline_ShouldSucceedWithDefaultDeadline()
    {
        // Arrange & Act
        var result = TeamCart.Create(
            DefaultHostUserId,
            DefaultRestaurantId,
            DefaultHostName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var teamCart = result.Value;
        
        // Default deadline should be set to 24 hours from creation
        teamCart.Deadline.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
        teamCart.Deadline.Should().NotBeNull("Deadline should not be null");
        teamCart.ExpiresAt.Should().BeCloseTo(teamCart.Deadline!.Value, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Create_WithPastDeadline_ShouldFailWithInvalidDeadlineError()
    {
        // Arrange
        var pastDeadline = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = TeamCart.Create(
            DefaultHostUserId,
            DefaultRestaurantId,
            DefaultHostName,
            pastDeadline);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.DeadlineInPast);
    }

    [Test]
    public void Create_WithEmptyHostName_ShouldFailWithInvalidNameError()
    {
        // Arrange
        var emptyHostName = string.Empty;

        // Act
        var result = TeamCart.Create(
            DefaultHostUserId,
            DefaultRestaurantId,
            emptyHostName,
            DefaultDeadline);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.HostNameRequired);
    }
}