using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.ValueObjects;

/// <summary>
/// Represents a unique identifier for a member payment in a team cart.
/// </summary>
public sealed class MemberPaymentId : ValueObject
{
    /// <summary>
    /// Gets the value of the member payment ID.
    /// </summary>
    public Guid Value { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberPaymentId"/> class.
    /// </summary>
    /// <param name="value">The unique identifier value.</param>
    private MemberPaymentId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private MemberPaymentId() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new unique member payment ID.
    /// </summary>
    /// <returns>A new unique <see cref="MemberPaymentId"/>.</returns>
    public static MemberPaymentId CreateUnique() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a member payment ID from a specified value.
    /// </summary>
    /// <param name="value">The GUID value for the member payment ID.</param>
    /// <returns>A <see cref="MemberPaymentId"/> with the specified value.</returns>
    public static MemberPaymentId Create(Guid value) => new(value);

    /// <summary>
    /// Creates a member payment ID from a string representation.
    /// </summary>
    /// <param name="value">The string representation of the GUID.</param>
    /// <returns>A result containing the <see cref="MemberPaymentId"/> if successful, or an error if parsing fails.</returns>
    public static Result<MemberPaymentId> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<MemberPaymentId>(TeamCartErrors.MemberPaymentIdEmpty);
        }

        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<MemberPaymentId>(TeamCartErrors.MemberPaymentIdInvalidFormat);
        }

        return Result.Success(new MemberPaymentId(guid));
    }

    /// <summary>
    /// Returns the equality components for value object comparison.
    /// </summary>
    /// <returns>The components used for equality comparison.</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Returns a string representation of the member payment ID.
    /// </summary>
    /// <returns>The string representation of the member payment ID.</returns>
    public override string ToString() => Value.ToString();
}
