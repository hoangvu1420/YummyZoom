using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UserAggregate.ValueObjects;

public sealed class PaymentMethodId : ValueObject
{
    public Guid Value { get; private set; }

    private PaymentMethodId(Guid value)
    {
        Value = value;
    }

    public static PaymentMethodId CreateUnique()
    {
        return new PaymentMethodId(Guid.NewGuid());
    }

    public static PaymentMethodId Create(Guid value)
    {
        return new PaymentMethodId(value);
    }

    public static Result<PaymentMethodId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            // Assuming UserErrors class will have an InvalidPaymentMethodId error
            return Result.Failure<PaymentMethodId>(UserErrors.InvalidPaymentMethod); // Reusing InvalidPaymentMethod for now, can create a specific one if needed
        }

        return Result.Success(new PaymentMethodId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    // For EF Core
    private PaymentMethodId()
    {
    }
}
