using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.UserAggregate;

[TestFixture]
public class UserAggregateTests
{
    private const string DefaultUserName = "John Doe";
    private const string DefaultUserEmail = "john.doe@example.com";
    private const string DefaultUserPhoneNumber = "123-456-7890";

    // Helper method to create a valid Address for testing
    private static Address CreateValidAddress(string label = "Home")
    {
        return Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", label);
    }

     // Helper method to create a valid PaymentMethod for testing
    private static PaymentMethod CreateValidPaymentMethod(string type = "Card", string tokenizedDetails = "tok_test", bool isDefault = false)
    {
        return PaymentMethod.Create(type, tokenizedDetails, isDefault);
    }


    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeUserCorrectly()
    {
        // Arrange & Act
        var result = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var user = result.Value;

        user.Id.Value.Should().NotBe(Guid.Empty, "because a unique UserId should be generated");
        user.Name.Should().Be(DefaultUserName);
        user.Email.Should().Be(DefaultUserEmail);
        user.PhoneNumber.Should().Be(DefaultUserPhoneNumber);
        user.IsActive.Should().BeTrue("because new users are active by default");
        user.Addresses.Should().BeEmpty();
        user.PaymentMethods.Should().BeEmpty();
    }

    [Test]
    public void Create_WithNullPhoneNumber_ShouldSucceedAndSetPhoneNumberToNull()
    {
        // Arrange & Act
        var result = User.Create(DefaultUserName, DefaultUserEmail, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var user = result.Value;

        user.Name.Should().Be(DefaultUserName);
        user.Email.Should().Be(DefaultUserEmail);
        user.PhoneNumber.Should().BeNull();
        user.IsActive.Should().BeTrue();
    }

    [Test]
    public void AddAddress_WithValidAddress_ShouldAddAddressToAddressesCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
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
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var address1 = CreateValidAddress("Home");
        var address2 = CreateValidAddress("Work");
        user.AddAddress(address1);
        user.AddAddress(address2);
        user.Addresses.Should().HaveCount(2);

        // Act
        var result = user.RemoveAddress(address1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Addresses.Should().ContainSingle();
        user.Addresses.Should().NotContain(address1);
        user.Addresses.Should().Contain(address2);
    }

    [Test]
    public void RemoveAddress_NonExistentAddress_ShouldFailAndReturnAddressNotFound()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var existingAddress = CreateValidAddress("Home");
        user.AddAddress(existingAddress);
        user.Addresses.Should().ContainSingle();
        var nonExistentAddressId = AddressId.CreateUnique(); // Non-existent ID

        // Act
        var result = user.RemoveAddress(nonExistentAddressId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(UserErrors.AddressNotFound(nonExistentAddressId.Value));
        user.Addresses.Should().ContainSingle(); // Address should not have been removed
        user.Addresses.Should().Contain(existingAddress);
    }

    [Test]
    public void AddPaymentMethod_WithValidPaymentMethod_ShouldAddPaymentMethodToCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
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
    public void AddPaymentMethod_WithDefaultPaymentMethod_ShouldUnsetOtherDefaults()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var paymentMethod1 = CreateValidPaymentMethod("Card", "tok_1", true);
        var paymentMethod2 = CreateValidPaymentMethod("PayPal", "tok_2", true);
        user.AddPaymentMethod(paymentMethod1);

        // Act
        var result = user.AddPaymentMethod(paymentMethod2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PaymentMethods.Should().HaveCount(2);
        paymentMethod1.IsDefault.Should().BeFalse();
        paymentMethod2.IsDefault.Should().BeTrue();
    }

    [Test]
    public void RemovePaymentMethod_ExistingPaymentMethod_ShouldRemovePaymentMethodFromCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
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
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var existingPaymentMethod = CreateValidPaymentMethod("Card");
        user.AddPaymentMethod(existingPaymentMethod);
        user.PaymentMethods.Should().ContainSingle();
        var nonExistentPaymentMethodId = PaymentMethodId.CreateUnique(); // Non-existent ID

        // Act
        var result = user.RemovePaymentMethod(nonExistentPaymentMethodId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(UserErrors.PaymentMethodNotFound(nonExistentPaymentMethodId.Value));
        user.PaymentMethods.Should().ContainSingle(); // Payment method should not have been removed
        user.PaymentMethods.Should().Contain(existingPaymentMethod);
    }

    [Test]
    public void SetDefaultPaymentMethod_ExistingPaymentMethod_ShouldSetAsDefaultAndUnsetOthers()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var paymentMethod1 = CreateValidPaymentMethod("Card", "tok_1", true);
        var paymentMethod2 = CreateValidPaymentMethod("PayPal", "tok_2", false);
        user.AddPaymentMethod(paymentMethod1);
        user.AddPaymentMethod(paymentMethod2);

        // Act
        var result = user.SetDefaultPaymentMethod(paymentMethod2.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        paymentMethod1.IsDefault.Should().BeFalse();
        paymentMethod2.IsDefault.Should().BeTrue();
    }

    [Test]
    public void SetDefaultPaymentMethod_NonExistentPaymentMethod_ShouldFailAndReturnPaymentMethodNotFound()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var nonExistentPaymentMethodId = PaymentMethodId.CreateUnique();

        // Act
        var result = user.SetDefaultPaymentMethod(nonExistentPaymentMethodId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(UserErrors.PaymentMethodNotFound(nonExistentPaymentMethodId.Value));
    }

    [Test]
    public void UpdateProfile_WithValidInputs_ShouldUpdateNameAndPhoneNumber()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
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
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
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
    public void UpdateEmail_WithValidEmail_ShouldUpdateEmail()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var newEmail = "newemail@example.com";

        // Act
        var result = user.UpdateEmail(newEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Email.Should().Be(newEmail);
    }

    [Test]
    public void Activate_WhenUserIsInactive_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var userResult = User.Create(userId, DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, false);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.IsActive.Should().BeFalse();

        // Act
        var result = user.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Test]
    public void Deactivate_WhenUserIsActive_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber);
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.IsActive.Should().BeTrue();

        // Act
        var result = user.Deactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    #region Property Immutability Tests

    [Test]
    public void Addresses_ShouldBeReadOnly()
    {
        // Arrange
        var addresses = new List<Address> { CreateValidAddress() };

        // Act
        var user = User.Create(
            UserId.CreateUnique(),
            DefaultUserName,
            DefaultUserEmail,
            DefaultUserPhoneNumber,
            isActive: true,
            addresses: addresses,
            paymentMethods: null).Value;

        // Assert
        // Type check
        var property = typeof(User).GetProperty(nameof(User.Addresses));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<Address>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<Address>)user.Addresses).Add(CreateValidAddress("Work"));
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the user
        addresses.Add(CreateValidAddress("Work"));
        user.Addresses.Should().HaveCount(1);
    }

    [Test]
    public void PaymentMethods_ShouldBeReadOnly()
    {
        // Arrange
        var paymentMethods = new List<PaymentMethod> { CreateValidPaymentMethod() };

        // Act
        var user = User.Create(
            UserId.CreateUnique(),
            DefaultUserName,
            DefaultUserEmail,
            DefaultUserPhoneNumber,
            isActive: true,
            addresses: null,
            paymentMethods: paymentMethods).Value;

        // Assert
        // Type check
        var property = typeof(User).GetProperty(nameof(User.PaymentMethods));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<PaymentMethod>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<PaymentMethod>)user.PaymentMethods).Add(CreateValidPaymentMethod("PayPal"));
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the user
        paymentMethods.Add(CreateValidPaymentMethod("PayPal"));
        user.PaymentMethods.Should().HaveCount(1);
    }

    #endregion
}
