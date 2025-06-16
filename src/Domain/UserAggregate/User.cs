using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UserAggregate;

public sealed class User : AggregateRoot<UserId, Guid>
{
    private readonly List<Address> _addresses = [];
    private readonly List<PaymentMethod> _paymentMethods = [];

    public string Name { get; private set; }
    public string Email { get; private set; } // Unique identifier for login
    public string? PhoneNumber { get; private set; } // Optional
    public bool IsActive { get; private set; }

    public IReadOnlyList<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyList<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

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
        _addresses = addresses;
        _paymentMethods = paymentMethods;
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
        user.AddDomainEvent(new UserCreated((UserId)user.Id));

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
        paymentMethod.SetAsDefault(true);
        return Result.Success();
    }

    public Result UpdateProfile(string name, string? phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        return Result.Success();
    }

    public Result UpdateEmail(string email)
    {
        // Email is an identifier, so it's handled separately from regular profile updates
        Email = email;
        return Result.Success();
    }

    public Result Activate()
    {
        IsActive = true;
        return Result.Success();
    }

    public Result Deactivate()
    {
        IsActive = false;
        return Result.Success();
    }

#pragma warning disable CS8618
    // For EF Core
    private User()
    {
    }
#pragma warning restore CS8618
}
