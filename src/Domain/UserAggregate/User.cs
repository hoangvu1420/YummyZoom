using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UserAggregate;

public sealed class User : AggregateRoot<UserId, Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private readonly List<Address> _addresses = [];
    private readonly List<PaymentMethod> _paymentMethods = [];

    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyList<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyList<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Properties from ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    private User(
        UserId id,
        string name,
        string email,
        string? phoneNumber,
        bool isActive,
        List<Address> addresses,
        List<PaymentMethod> paymentMethods)
        : base(id)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        IsActive = isActive;
        _addresses = new List<Address>(addresses);
        _paymentMethods = new List<PaymentMethod>(paymentMethods);
    }

    public static Result<User> Create(
        string name,
        string email,
        string? phoneNumber = null)
    {
        var user = new User(
            UserId.CreateUnique(),
            name,
            email,
            phoneNumber,
            isActive: true, // New users are active by default
            [],
            []);

        // Add domain event
        user.AddDomainEvent(new UserCreated(user.Id));

        return Result.Success(user);
    }

    public static Result<User> Create(
        UserId id,
        string name,
        string email,
        string? phoneNumber,
        bool isActive,
        List<Address>? addresses = null,
        List<PaymentMethod>? paymentMethods = null)
    {
        var user = new User(
            id,
            name,
            email,
            phoneNumber,
            isActive,
            addresses ?? [],
            paymentMethods ?? []);

        // Add domain event if needed
        // user.AddDomainEvent(new UserCreated(user));

        return Result.Success(user);
    }

    public Result AddAddress(Address address)
    {
        _addresses.Add(address);

        // Raise domain event
        AddDomainEvent(new UserAddressAdded(Id, address));

        return Result.Success();
    }

    public Result RemoveAddress(AddressId addressId)
    {
        var addressToRemove = _addresses.FirstOrDefault(a => a.Id.Value == addressId.Value);

        if (addressToRemove is null)
        {
            return Result.Failure(UserErrors.AddressNotFound(addressId.Value));
        }

        _addresses.Remove(addressToRemove);

        // Raise domain event
        AddDomainEvent(new UserAddressRemoved(Id, addressId));

        return Result.Success();
    }

    public Result AddPaymentMethod(PaymentMethod paymentMethod)
    {
        // If this is set as default, unset all other defaults
        if (paymentMethod.IsDefault)
        {
            foreach (var pm in _paymentMethods)
            {
                pm.SetAsDefault(false);
            }
        }

        _paymentMethods.Add(paymentMethod);

        // Raise domain event
        AddDomainEvent(new UserPaymentMethodAdded(Id, paymentMethod));

        return Result.Success();
    }

    public Result RemovePaymentMethod(PaymentMethodId paymentMethodId)
    {
        var paymentMethodToRemove = _paymentMethods.FirstOrDefault(pm => pm.Id.Value == paymentMethodId.Value);

        if (paymentMethodToRemove is null)
        {
            return Result.Failure(UserErrors.PaymentMethodNotFound(paymentMethodId.Value));
        }

        _paymentMethods.Remove(paymentMethodToRemove);

        // Raise domain event
        AddDomainEvent(new UserPaymentMethodRemoved(Id, paymentMethodId));

        return Result.Success();
    }

    public Result SetDefaultPaymentMethod(PaymentMethodId paymentMethodId)
    {
        var paymentMethod = _paymentMethods.FirstOrDefault(pm => pm.Id.Value == paymentMethodId.Value);

        if (paymentMethod is null)
        {
            return Result.Failure(UserErrors.PaymentMethodNotFound(paymentMethodId.Value));
        }

        // Unset all other defaults
        foreach (var pm in _paymentMethods)
        {
            pm.SetAsDefault(false);
        }

        // Set the specified one as default
        paymentMethod.SetAsDefault();

        // Raise domain event
        AddDomainEvent(new UserDefaultPaymentMethodChanged(Id, paymentMethodId));

        return Result.Success();
    }

    public Result UpdateProfile(string name, string? phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;

        // Raise domain event
        AddDomainEvent(new UserProfileUpdated(Id, name, phoneNumber));

        return Result.Success();
    }

    public Result UpdateEmail(string email)
    {
        // Store the old email for the event
        var oldEmail = Email;

        // Email is an identifier, so it's handled separately from regular profile updates
        Email = email;

        // Raise domain event
        AddDomainEvent(new UserEmailChanged(Id, oldEmail, email));

        return Result.Success();
    }

    public Result Activate()
    {
        IsActive = true;

        // Raise domain event
        AddDomainEvent(new UserActivated(Id));

        return Result.Success();
    }

    public Result Deactivate()
    {
        IsActive = false;

        // Raise domain event
        AddDomainEvent(new UserDeactivated(Id));

        return Result.Success();
    }

    public Result MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy = null)
    {
        if (IsDeleted)
        {
            // Optionally, handle re-deleting an already deleted entity.
            return Result.Success();
        }

        IsDeleted = true;
        DeletedOn = deletedOn; // Explicitly set the timestamp here.
        DeletedBy = deletedBy;

        AddDomainEvent(new UserDeleted(Id));
        return Result.Success();
    }

    public Result Restore()
    {
        if (!IsDeleted)
        {
            // Already restored
            return Result.Success();
        }

        IsDeleted = false;
        DeletedOn = null;
        DeletedBy = null;

        AddDomainEvent(new UserRestored(Id));
        return Result.Success();
    }

#pragma warning disable CS8618
    // For EF Core
    private User()
    {
    }
#pragma warning restore CS8618
}
