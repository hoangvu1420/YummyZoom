using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuEntity.ValueObjects;

public sealed class MenuId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private MenuId(Guid value)
    {
        Value = value;
    }

    public static MenuId CreateUnique()
    {
        return new MenuId(Guid.NewGuid());
    }

    public static MenuId Create(Guid value)
    {
        return new MenuId(value);
    }

    public static Result<MenuId> Create(string value)
    {
        return !Guid.TryParse(value, out var guid)
            ? Result.Failure<MenuId>(MenuErrors.InvalidMenuId)
            : Result.Success(new MenuId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private MenuId() { }
#pragma warning restore CS8618
}
