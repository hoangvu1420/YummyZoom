using YummyZoom.Domain.TagEntity.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TagEntity.ValueObjects;

public sealed class TagId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private TagId(Guid value)
    {
        Value = value;
    }

    public static TagId CreateUnique()
    {
        return new TagId(Guid.NewGuid());
    }

    public static TagId Create(Guid value)
    {
        return new TagId(value);
    }
    
    public static Result<TagId> Create(string value)
    {
        return !Guid.TryParse(value, out var guid) 
            ? Result.Failure<TagId>(TagErrors.InvalidTagId) 
            : Result.Success(new TagId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private TagId() { }
#pragma warning restore CS8618
}
