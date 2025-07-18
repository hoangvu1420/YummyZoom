using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.ValueObjects;

/// <summary>
/// Represents a unique identifier for a team cart item.
/// </summary>
public sealed class TeamCartItemId : ValueObject
{
    /// <summary>
    /// Gets the value of the team cart item ID.
    /// </summary>
    public Guid Value { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamCartItemId"/> class.
    /// </summary>
    /// <param name="value">The unique identifier value.</param>
    private TeamCartItemId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private TeamCartItemId() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new unique team cart item ID.
    /// </summary>
    /// <returns>A new unique <see cref="TeamCartItemId"/>.</returns>
    public static TeamCartItemId CreateUnique() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a team cart item ID from a specified value.
    /// </summary>
    /// <param name="value">The GUID value for the team cart item ID.</param>
    /// <returns>A <see cref="TeamCartItemId"/> with the specified value.</returns>
    public static TeamCartItemId Create(Guid value) => new(value);

    /// <summary>
    /// Creates a team cart item ID from a string representation.
    /// </summary>
    /// <param name="value">The string representation of the GUID.</param>
    /// <returns>A result containing the <see cref="TeamCartItemId"/> if successful, or an error if parsing fails.</returns>
    public static Result<TeamCartItemId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<TeamCartItemId>(TeamCartErrors.TeamCartItemIdInvalidFormat);
        }

        return Result.Success(new TeamCartItemId(guid));
    }

    /// <summary>
    /// Gets the equality components for value object comparison.
    /// </summary>
    /// <returns>An enumerable of objects used for equality comparison.</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
