using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate.Entities;

[TestFixture]
public class TeamCartMemberTests
{
    private readonly UserId _userId = UserId.CreateUnique();
    private const string _validName = "Test User";
    
    [Test]
    public void Create_WithValidInputs_ShouldCreateMemberWithCorrectProperties()
    {
        // Act
        var result = TeamCartMember.Create(_userId, _validName, MemberRole.Guest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var member = result.Value;
        
        member.Id.Value.Should().NotBe(Guid.Empty);
        member.UserId.Should().Be(_userId);
        member.Name.Should().Be(_validName);
        member.Role.Should().Be(MemberRole.Guest);
    }

    [Test]
    public void Create_WithEmptyName_ShouldFailWithInvalidNameError()
    {
        // Arrange
        var emptyName = string.Empty;

        // Act
        var result = TeamCartMember.Create(_userId, emptyName, MemberRole.Guest);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.NameRequired);
    }

    [Test]
    public void Create_WithNullUserId_ShouldFailWithInvalidUserIdError()
    {
        // Act
        var result = TeamCartMember.Create(null!, _validName, MemberRole.Guest);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.UserIdRequired);
    }
}
