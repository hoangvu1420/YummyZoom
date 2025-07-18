using FluentAssertions;
using NUnit.Framework;
using System.Reflection;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartMemberTests : TeamCartTestHelpers
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
        result.IsSuccess.Should().BeTrue();
        
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
        result.IsFailure.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.MemberNameRequired);
    }

    [Test]
    public void AddMember_ToExpiredCart_ShouldFailWithCartExpiredError()
    {
        // Arrange
        var expiredTeamCart = CreateExpiredTeamCart();
        var guestUserId = UserId.CreateUnique();

        // Act
        var result = expiredTeamCart.AddMember(guestUserId, "Guest User");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.CannotAddMembersToClosedCart);
    }
}
