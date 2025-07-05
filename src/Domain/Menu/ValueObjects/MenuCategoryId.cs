
namespace YummyZoom.Domain.Menu.ValueObjects;

public sealed class MenuCategoryId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private MenuCategoryId(Guid value)
    {
        Value = value;
    }

    public static MenuCategoryId CreateUnique()
    {
        return new MenuCategoryId(Guid.NewGuid());
    }

    public static MenuCategoryId Create(Guid value)
    {
        return new MenuCategoryId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private MenuCategoryId() { }
#pragma warning restore CS8618
}
