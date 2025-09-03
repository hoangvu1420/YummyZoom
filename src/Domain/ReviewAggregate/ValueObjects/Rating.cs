using System.Text.Json.Serialization;
using YummyZoom.Domain.ReviewAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.ReviewAggregate.ValueObjects;

public sealed class Rating : ValueObject
{
    public int Value { get; private set; }

    [JsonConstructor]
    private Rating(int value)
    {
        Value = value;
    }

    public static Result<Rating> Create(int value)
    {
        if (value < 1 || value > 5)
        {
            return Result.Failure<Rating>(ReviewErrors.InvalidRating);
        }

        return Result.Success(new Rating(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private Rating()
    {
    }
#pragma warning restore CS8618
}
