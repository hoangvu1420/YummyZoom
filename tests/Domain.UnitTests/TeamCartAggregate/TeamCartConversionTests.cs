using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartConversionTests : TeamCartTestHelpers
{
    #region MarkAsConverted() Method Tests

    [Test]
    public void MarkAsConverted_FromReadyToConfirm_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyForConversion();

        // Act
        var result = teamCart.MarkAsConverted();

        // Assert
        result.ShouldBeSuccessful();
        teamCart.Status.Should().Be(TeamCartStatus.Converted);
    }

    [Test]
    public void MarkAsConverted_FromInvalidStatus_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();

        // Act
        var result = teamCart.MarkAsConverted();

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    [Test]
    public void MarkAsConverted_FromOpen_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();
        teamCart.Status.Should().Be(TeamCartStatus.Open);

        // Act
        var result = teamCart.MarkAsConverted();

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    [Test]
    public void MarkAsConverted_FromAwaitingPayments_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithPartialPayment();

        // Act
        var result = teamCart.MarkAsConverted();

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    [Test]
    public void MarkAsConverted_FromExpired_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var teamCart = CreateExpiredTeamCart();

        // Act
        var result = teamCart.MarkAsConverted();

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    [Test]
    public void MarkAsConverted_FromAlreadyConverted_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyForConversion();
        teamCart.MarkAsConverted().ShouldBeSuccessful();

        // Act
        var result = teamCart.MarkAsConverted();

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    #endregion

    #region State Validation Tests

    [Test]
    public void MarkAsConverted_ShouldMaintainAllOtherProperties()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyForConversion();
        var originalId = teamCart.Id;
        var originalHostUserId = teamCart.HostUserId;
        var originalRestaurantId = teamCart.RestaurantId;
        var originalMembers = teamCart.Members;
        var originalItems = teamCart.Items;
        var originalPayments = teamCart.MemberPayments;
        var originalDeadline = teamCart.Deadline;
        var originalCreatedAt = teamCart.CreatedAt;
        var originalExpiresAt = teamCart.ExpiresAt;
        var originalShareToken = teamCart.ShareToken;

        // Act
        teamCart.MarkAsConverted();

        // Assert
        teamCart.Id.Should().Be(originalId);
        teamCart.HostUserId.Should().Be(originalHostUserId);
        teamCart.RestaurantId.Should().Be(originalRestaurantId);
        teamCart.Members.Should().BeEquivalentTo(originalMembers);
        teamCart.Items.Should().BeEquivalentTo(originalItems);
        teamCart.MemberPayments.Should().BeEquivalentTo(originalPayments);
        teamCart.Deadline.Should().Be(originalDeadline);
        teamCart.CreatedAt.Should().Be(originalCreatedAt);
        teamCart.ExpiresAt.Should().Be(originalExpiresAt);
        teamCart.ShareToken.Should().Be(originalShareToken);
    }

    [Test]
    public void MarkAsConverted_ShouldUpdateStatusToConverted()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyForConversion();
        teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Act
        teamCart.MarkAsConverted();

        // Assert
        teamCart.Status.Should().Be(TeamCartStatus.Converted);
    }

    #endregion

    #region Domain Event Tests

    [Test]
    public void MarkAsConverted_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyForConversion();
        var initialEventCount = teamCart.DomainEvents.Count;

        // Act
        teamCart.MarkAsConverted();

        // Assert
        // The MarkAsConverted method should not raise events directly
        // Events are now handled by the TeamCartConversionService
        teamCart.DomainEvents.Count.Should().Be(initialEventCount);
    }

    #endregion
}
