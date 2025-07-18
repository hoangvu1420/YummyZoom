using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.ValueObjects;

/// <summary>
/// Represents the unique identifier for a team cart.
/// </summary>
public sealed class TeamCartId : AggregateRootId<Guid>
{
    /// <summary>
    /// Gets or sets the value of the team cart ID.
    /// </summary>
    public override Guid Value { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamCartId"/> class.
    /// </summary>
    /// <param name="value">The GUID value for the team cart ID.</param>
    private TeamCartId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
    private TeamCartId() { }

    /// <summary>
    /// Creates a new unique team cart ID.
    /// </summary>
    /// <returns>A new unique team cart ID.</returns>
    public static TeamCartId CreateUnique() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a team cart ID from an existing GUID value.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    /// <returns>A team cart ID with the specified value.</returns>
    public static TeamCartId Create(Guid value) => new(value);

    /// <summary>
    /// Attempts to create a team cart ID from a string representation of a GUID.
    /// </summary>
    /// <param name="value">The string representation of a GUID.</param>
    /// <returns>A result containing the team cart ID if successful, or an error if the string is not a valid GUID.</returns>
    public static Result<TeamCartId> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TeamCartId>(TeamCartErrors.TeamCartIdEmpty);
        }

        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<TeamCartId>(TeamCartErrors.TeamCartIdInvalidFormat);
        }

        return Result.Success(new TeamCartId(guid));
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
