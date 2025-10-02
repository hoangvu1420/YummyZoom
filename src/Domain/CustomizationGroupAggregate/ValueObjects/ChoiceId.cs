using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

public sealed class ChoiceId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private ChoiceId(Guid value)
    {
        Value = value;
    }

    public static ChoiceId CreateUnique()
    {
        return new ChoiceId(Guid.NewGuid());
    }

    public static ChoiceId Create(Guid value)
    {
        return new ChoiceId(value);
    }

    public static Result<ChoiceId> Create(string value)
    {
        return !Guid.TryParse(value, out var guid)
            ? Result.Failure<ChoiceId>(CustomizationGroupErrors.InvalidChoiceId)
            : Result.Success(new ChoiceId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private ChoiceId() { }
#pragma warning restore CS8618
}
