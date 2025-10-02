using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CouponAggregate.ValueObjects;

public sealed class CouponId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private CouponId(Guid value)
    {
        Value = value;
    }

    public static CouponId CreateUnique()
    {
        return new CouponId(Guid.NewGuid());
    }

    public static CouponId Create(Guid value)
    {
        return new CouponId(value);
    }

    public static Result<CouponId> Create(string value)
    {
        return !Guid.TryParse(value, out var guid)
            ? Result.Failure<CouponId>(CouponErrors.InvalidCouponId(value))
            : Result.Success(new CouponId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private CouponId() { }
#pragma warning restore CS8618
}
