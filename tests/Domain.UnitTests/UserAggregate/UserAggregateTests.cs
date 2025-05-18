using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.UserAggregate;

[TestFixture]
public class UserAggregateTests
{
    private const string DefaultUserName = "John Doe";
    private const string DefaultUserEmail = "john.doe@example.com";
    private const string DefaultUserPhoneNumber = "123-456-7890";
    private const string DefaultRoleName = "Customer";

    // Helper method to create a valid RoleAssignment for testing
    private static RoleAssignment CreateValidRoleAssignment(string roleName = DefaultRoleName, string? targetEntityId = null, string? targetEntityType = null)
    {
        var result = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);
        result.IsSuccess.Should().BeTrue("because the role assignment inputs are valid");
        return result.Value;
    }

    // Helper method to create a valid Address for testing
    private static Address CreateValidAddress(string label = "Home")
    {
        var result = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", label);
        // Address Create method doesn't return Result, assuming valid inputs create valid Address
        return result;
    }

     // Helper method to create a valid PaymentMethod for testing
    private static PaymentMethod CreateValidPaymentMethod(string type = "Card", string tokenizedDetails = "tok_test", bool isDefault = false)
    {
        var result = PaymentMethod.Create(type, tokenizedDetails, isDefault);
        // PaymentMethod Create method doesn't return Result, assuming valid inputs create valid PaymentMethod
        return result;
    }


    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeUserCorrectly()
    {
        // Arrange
        var roles = new List<RoleAssignment> { CreateValidRoleAssignment() };

        // Act
        var result = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, roles);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var user = result.Value;

        user.Id.Value.Should().NotBe(Guid.Empty, "because a unique UserId should be generated");
        user.Name.Should().Be(DefaultUserName);
        user.Email.Should().Be(DefaultUserEmail);
        user.PhoneNumber.Should().Be(DefaultUserPhoneNumber);
        user.UserRoles.Should().ContainSingle();
        user.UserRoles.First().RoleName.Should().Be(DefaultRoleName);
        user.Addresses.Should().BeEmpty();
        user.PaymentMethods.Should().BeEmpty();
    }

    [Test]
    public void Create_WithNullRoles_ShouldFailAndReturnMustHaveAtLeastOneRoleError()
    {
        // Arrange
        List<RoleAssignment> roles = null!; // Use null-forgiving operator or cast

        // Act
        var result = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, roles); // The cast might be needed here depending on compiler

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.MustHaveAtLeastOneRole);
    }

     [Test]
    public void Create_WithEmptyRoles_ShouldFailAndReturnMustHaveAtLeastOneRoleError()
    {
        // Arrange
        var roles = new List<RoleAssignment>();

        // Act
        var result = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, roles);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.MustHaveAtLeastOneRole);
    }

    [Test]
    public void AddAddress_WithValidAddress_ShouldAddAddressToAddressesCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var address = CreateValidAddress();

        // Act
        var result = user.AddAddress(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Addresses.Should().ContainSingle();
        user.Addresses.Should().Contain(address);
    }

    [Test]
    public void RemoveAddress_ExistingAddress_ShouldRemoveAddressFromAddressesCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var address1 = CreateValidAddress("Home");
        var address2 = CreateValidAddress("Work");
        user.AddAddress(address1);
        user.AddAddress(address2);
        user.Addresses.Should().HaveCount(2);

        // Act
        var result = user.RemoveAddress(address1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Addresses.Should().ContainSingle();
        user.Addresses.Should().NotContain(address1);
        user.Addresses.Should().Contain(address2);
    }

    [Test]
    public void RemoveAddress_NonExistentAddress_ShouldReturnSuccess()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var existingAddress = CreateValidAddress("Home");
        user.AddAddress(existingAddress);
        user.Addresses.Should().ContainSingle();
        var nonExistentAddress = CreateValidAddress("Other"); // Different address

        // Act
        var result = user.RemoveAddress(nonExistentAddress);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Based on current domain implementation
        user.Addresses.Should().ContainSingle(); // Address should not have been removed
        user.Addresses.Should().Contain(existingAddress);
    }

    [Test]
    public void AddPaymentMethod_WithValidPaymentMethod_ShouldAddPaymentMethodToCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var paymentMethod = CreateValidPaymentMethod();

        // Act
        var result = user.AddPaymentMethod(paymentMethod);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PaymentMethods.Should().ContainSingle();
        user.PaymentMethods.Should().Contain(paymentMethod);
    }

    [Test]
    public void RemovePaymentMethod_ExistingPaymentMethod_ShouldRemovePaymentMethodFromCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var paymentMethod1 = CreateValidPaymentMethod("Card");
        var paymentMethod2 = CreateValidPaymentMethod("PayPal");
        user.AddPaymentMethod(paymentMethod1);
        user.AddPaymentMethod(paymentMethod2);
        user.PaymentMethods.Should().HaveCount(2);

        // Act
        var result = user.RemovePaymentMethod(paymentMethod1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PaymentMethods.Should().ContainSingle();
        user.PaymentMethods.Should().NotContain(paymentMethod1);
        user.PaymentMethods.Should().Contain(paymentMethod2);
    }

    [Test]
    public void RemovePaymentMethod_NonExistentPaymentMethod_ShouldFailAndReturnPaymentMethodNotFound()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var existingPaymentMethod = CreateValidPaymentMethod("Card");
        user.AddPaymentMethod(existingPaymentMethod);
        user.PaymentMethods.Should().ContainSingle();
        var nonExistentPaymentMethodId = PaymentMethodId.CreateUnique(); // Non-existent ID

        // Act
        var result = user.RemovePaymentMethod(nonExistentPaymentMethodId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.PaymentMethodNotFound(nonExistentPaymentMethodId.Value));
        user.PaymentMethods.Should().ContainSingle(); // Payment method should not have been removed
        user.PaymentMethods.Should().Contain(existingPaymentMethod);
    }

    [Test]
    public void UpdateProfile_WithValidInputs_ShouldUpdateNameAndPhoneNumber()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var newName = "Jane Doe";
        var newPhoneNumber = "987-654-3210";

        // Act
        var result = user.UpdateProfile(newName, newPhoneNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Name.Should().Be(newName);
        user.PhoneNumber.Should().Be(newPhoneNumber);
    }

    [Test]
    public void UpdateProfile_WithNullPhoneNumber_ShouldUpdateNameAndSetPhoneNumberToNull()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var newName = "Jane Doe";
        string? newPhoneNumber = null;

        // Act
        var result = user.UpdateProfile(newName, newPhoneNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Name.Should().Be(newName);
        user.PhoneNumber.Should().BeNull();
    }

    [Test]
    public void AddRole_WithValidRoleAssignment_ShouldAddRoleAssignmentToCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment("Customer") });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var restaurantOwnerRole = CreateValidRoleAssignment("RestaurantOwner", Guid.NewGuid().ToString(), "Restaurant");

        // Act
        var result = user.AddRole(restaurantOwnerRole);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.UserRoles.Should().HaveCount(2);
        user.UserRoles.Should().Contain(restaurantOwnerRole);
    }

    [Test]
    public void AddRole_WithExistingRoleAssignment_ShouldReturnSuccessAndNotAddDuplicate()
    {
        // Arrange
        var customerRole = CreateValidRoleAssignment("Customer");
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { customerRole });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.UserRoles.Should().ContainSingle();

        // Act
        var result = user.AddRole(customerRole); // Add the same role again

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.UserRoles.Should().ContainSingle(); // Should not add a duplicate
        user.UserRoles.Should().Contain(customerRole);
    }

    [Test]
    public void RemoveRole_ExistingRoleAssignment_ShouldRemoveRoleAssignmentFromCollection()
    {
        // Arrange
        var customerRole = CreateValidRoleAssignment("Customer");
        var adminRole = CreateValidRoleAssignment("Admin");
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { customerRole, adminRole });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.UserRoles.Should().HaveCount(2);

        // Act
        var result = user.RemoveRole(adminRole.RoleName, adminRole.TargetEntityId, adminRole.TargetEntityType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.UserRoles.Should().ContainSingle();
        user.UserRoles.Should().NotContain(adminRole);
        user.UserRoles.Should().Contain(customerRole);
    }

    [Test]
    public void RemoveRole_LastRoleAssignment_ShouldFailAndReturnCannotRemoveLastRoleError()
    {
        // Arrange
        var customerRole = CreateValidRoleAssignment("Customer");
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { customerRole });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.UserRoles.Should().ContainSingle();

        // Act
        var result = user.RemoveRole(customerRole.RoleName, customerRole.TargetEntityId, customerRole.TargetEntityType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.CannotRemoveLastRole);
        user.UserRoles.Should().ContainSingle(); // Role should not have been removed
        user.UserRoles.Should().Contain(customerRole);
    }

    [Test]
    public void RemoveRole_NonExistentRoleAssignment_ShouldFailAndReturnRoleNotFound()
    {
        // Arrange
        var customerRole = CreateValidRoleAssignment("Customer");
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { customerRole });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.UserRoles.Should().ContainSingle();
        var nonExistentRole = CreateValidRoleAssignment("Admin"); // Non-existent role

        // Act
        var result = user.RemoveRole(nonExistentRole.RoleName, nonExistentRole.TargetEntityId, nonExistentRole.TargetEntityType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.RoleNotFound(nonExistentRole.RoleName)); // Reusing error for now
        user.UserRoles.Should().ContainSingle(); // Role should not have been removed
        user.UserRoles.Should().Contain(customerRole);
    }
}
