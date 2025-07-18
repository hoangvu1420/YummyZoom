using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.ValueObjects;

/// <summary>
/// Represents the unique identifier for a team cart member.
/// </summary>
public sealed class TeamCartMemberId : ValueObject
{
    /// <summary>
    /// Gets the value of the team cart member ID.
    /// </summary>
    public Guid Value { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamCartMemberId"/> class.
    /// </summary>
    /// <param name="value">The GUID value for the team cart member ID.</param>
    private TeamCartMemberId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
    private TeamCartMemberId() { }

    /// <summary>
    /// Creates a new unique team cart member ID.
    /// </summary>
    /// <returns>A new unique team cart member ID.</returns>
    public static TeamCartMemberId CreateUnique() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a team cart member ID from an existing GUID value.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    /// <returns>A team cart member ID with the specified value.</returns>
    public static TeamCartMemberId Create(Guid value) => new(value);

    /// <summary>
    /// Attempts to create a team cart member ID from a string representation of a GUID.
    /// </summary>
    /// <param name="value">The string representation of a GUID.</param>
    /// <returns>A result containing the team cart member ID if successful, or an error if the string is not a valid GUID.</returns>
    public static Result<TeamCartMemberId> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TeamCartMemberId>(TeamCartErrors.TeamCartMemberIdEmpty);
        }

        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<TeamCartMemberId>(TeamCartErrors.TeamCartMemberIdInvalidFormat);
        }

        return Result.Success(new TeamCartMemberId(guid));
    }

    /// <summary>
    /// Gets the equality components for the value object.
    /// </summary>
    /// <returns>An enumerable of objects representing the equality components.</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
