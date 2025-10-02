using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.UserAggregate.ValueObjects; // For PaymentMethodId

namespace YummyZoom.Domain.UserAggregate.Entities;

public sealed class PaymentMethod : Entity<PaymentMethodId>, IAuditableEntity
{
    public string Type { get; private set; }
    public string TokenizedDetails { get; private set; }
    public bool IsDefault { get; private set; }

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    private PaymentMethod(
        PaymentMethodId id,
        string type,
        string tokenizedDetails,
        bool isDefault)
        : base(id)
    {
        Type = type;
        TokenizedDetails = tokenizedDetails;
        IsDefault = isDefault;
    }

    public static PaymentMethod Create(
        string type,
        string tokenizedDetails,
        bool isDefault)
    {
        // Basic validation (more complex validation handled in User aggregate)
        // Assuming type and tokenizedDetails are not null/empty is handled in Application layer
        return new PaymentMethod(
            PaymentMethodId.CreateUnique(),
            type,
            tokenizedDetails,
            isDefault);
    }

    // Factory method to create a PaymentMethod with an existing ID (e.g., for persistence)
    public static PaymentMethod Create(
        PaymentMethodId id,
        string type,
        string tokenizedDetails,
        bool isDefault)
    {
        // Basic validation (more complex validation handled in User aggregate)
        // Assuming type and tokenizedDetails are not null/empty is handled in Application layer
        return new PaymentMethod(
            id,
            type,
            tokenizedDetails,
            isDefault);
    }


    // Methods to update payment method details or set as default can be added here
    // These methods should return Result and contain relevant business logic/invariants
    public void SetAsDefault(bool isDefault = true)
    {
        IsDefault = isDefault;
    }

#pragma warning disable CS8618
    // For EF Core
    private PaymentMethod()
    {
    }
#pragma warning restore CS8618
}
