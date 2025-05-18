using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Domain.UserAggregate;

public sealed class User : AggregateRoot<UserId, Guid>
{
    private readonly List<RoleAssignment> _userRoles = [];
    private readonly List<Address> _addresses = [];
    private readonly List<PaymentMethod> _paymentMethods = [];

    public string Name { get; private set; }
    public string Email { get; private set; } // Unique identifier for login
    public string? PhoneNumber { get; private set; } // Optional

    public IReadOnlyList<RoleAssignment> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyList<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyList<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    private User(
        UserId id,
        string name,
        string email,
        string? phoneNumber,
        List<RoleAssignment> userRoles,
        List<Address> addresses,
        List<PaymentMethod> paymentMethods)
        : base(id)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        _userRoles = userRoles ?? [];
        _addresses = addresses;
        _paymentMethods = paymentMethods;
    }

    public static Result<User> Create(
        string name,
        string email,
        string? phoneNumber,
        List<RoleAssignment>? userRoles = null) 
    {
        // If userRoles is null or empty, create a default Customer role
        if (userRoles == null || userRoles.Count == 0)
        {
            var customerRoleResult = RoleAssignment.Create(Roles.Customer);
            if (customerRoleResult.IsFailure)
            {
                return Result.Failure<User>(customerRoleResult.Error);
            }
            
            userRoles = [customerRoleResult.Value];
        }

        var user = new User(
            UserId.CreateUnique(),
            name,
            email,
            phoneNumber,
            userRoles,
            [],
            []);

        // Add domain event
        // user.AddDomainEvent(new UserCreated(user));

        return Result.Success(user);
    }
    
    public static Result<User> Create(
        UserId id,
        string name,
        string email,
        string? phoneNumber,
        List<RoleAssignment>? userRoles = null) 
    {
        // If userRoles is null or empty, create a default Customer role
        if (userRoles == null || userRoles.Count == 0)
        {
            var customerRoleResult = RoleAssignment.Create(Roles.Customer);
            if (customerRoleResult.IsFailure)
            {
                return Result.Failure<User>(customerRoleResult.Error);
            }
            
            userRoles = [customerRoleResult.Value];
        }

        var user = new User(
            id,
            name,
            email,
            phoneNumber,
            userRoles,
            [],
            []);

        // Add domain event
        // user.AddDomainEvent(new UserCreated(user));

        return Result.Success(user);
    }

    public Result AddAddress(Address address)
    {
        _addresses.Add(address);
        return Result.Success();
    }

    public Result RemoveAddress(Address address)
    {
        // Remove address based on value equality
        var removed = _addresses.Remove(address);

        if (!removed)
        {
            // Need a more specific error here if Address equality is not sufficient
            // For now, reusing a general error or assuming success if address exists
            // return Result.Failure(UserErrors.AddressNotFound(...)); // If Address had an ID
        }

        return Result.Success();
    }

    public Result AddPaymentMethod(PaymentMethod paymentMethod)
    {
        // Assuming basic validity check is sufficient at this level for now
        // The PaymentMethod object is assumed to be valid and non-null by the time it reaches the domain.

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

    public Result UpdateProfile(string name, string? phoneNumber)
    {
        // No domain-specific invariants to check for profile update at this level.
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

    public Result AddRole(RoleAssignment roleAssignment) 
    {
        // Check if the role assignment already exists based on Value Object equality.
        if (_userRoles.Contains(roleAssignment))
        {
            // Role assignment already exists, consider this a success or return a specific error
            // For now, we'll treat adding an existing role assignment as a successful no-op.
            return Result.Success();
        }

        _userRoles.Add(roleAssignment);
        // AddDomainEvent(new RoleAssignmentAddedToUserEvent((UserId)Id, roleAssignment)); 
        return Result.Success();
    }

    public Result RemoveRole(
        string roleName,
        string? targetEntityId = null,
        string? targetEntityType = null) // Change parameter types
    {
        // Create a RoleAssignment object to use for comparison
        var roleAssignmentToRemoveResult = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);

        if (roleAssignmentToRemoveResult.IsFailure)
        {
            // Handle invalid input for creating RoleAssignment
            return Result.Failure(roleAssignmentToRemoveResult.Error);
        }

        var roleAssignmentToRemove = roleAssignmentToRemoveResult.Value;

        // Invariant: A user must have at least one RoleAssignment.
        if (_userRoles.Count == 1 && _userRoles.Contains(roleAssignmentToRemove))
        {
            return Result.Failure(UserErrors.CannotRemoveLastRole);
        }

        // Find and remove the role assignment
        var removed = _userRoles.Remove(roleAssignmentToRemove);

        if (!removed)
        {
            // Role assignment not found
            return Result.Failure(UserErrors.RoleNotFound(roleName)); 
        }

        // AddDomainEvent(new RoleAssignmentRemovedFromUserEvent((UserId)Id, roleAssignmentToRemove)); 
        return Result.Success();
    }


#pragma warning disable CS8618
    // For EF Core
    private User()
    {
    }
#pragma warning restore CS8618
}
