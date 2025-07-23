using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Entities;

/// <summary>
/// Represents a member in a team cart.
/// </summary>
public sealed class TeamCartMember : Entity<TeamCartMemberId>
{
    /// <summary>
    /// Gets the ID of the user associated with this member.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets the display name of the member.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the role of the member in the team cart.
    /// </summary>
    public MemberRole Role { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamCartMember"/> class.
    /// </summary>
    private TeamCartMember(
        TeamCartMemberId id,
        UserId userId,
        string name,
        MemberRole role)
        : base(id)
    {
        UserId = userId;
        Name = name;
        Role = role;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private TeamCartMember() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new team cart member.
    /// </summary>
    public static Result<TeamCartMember> Create(
        UserId userId,
        string name,
        MemberRole role = MemberRole.Guest)
    {
        if (userId is null)
        {
            return Result.Failure<TeamCartMember>(TeamCartErrors.UserIdRequired);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<TeamCartMember>(TeamCartErrors.NameRequired);
        }

        var member = new TeamCartMember(
            TeamCartMemberId.CreateUnique(),
            userId,
            name,
            role);

        return Result.Success(member);
    }
}
