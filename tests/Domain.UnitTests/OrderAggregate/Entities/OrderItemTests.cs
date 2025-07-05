using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate.Entities;

[TestFixture]
public class OrderItemTests
{
    private static readonly MenuCategoryId DefaultMenuCategoryId = MenuCategoryId.CreateUnique();
    private static readonly MenuItemId DefaultMenuItemId = MenuItemId.CreateUnique();
    private const string DefaultItemName = "Test Pizza";
    private static readonly Money DefaultBasePrice = new Money(15.99m, Currencies.Default);
    private const int DefaultQuantity = 2;
    private static readonly OrderItemCustomization DefaultCustomization = CreateDefaultCustomization();

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var orderItem = result.Value;
        
        orderItem.Id.Value.Should().NotBe(Guid.Empty);
        orderItem.Snapshot_MenuItemId.Should().Be(DefaultMenuItemId);
        orderItem.Snapshot_ItemName.Should().Be(DefaultItemName);
        orderItem.Snapshot_BasePriceAtOrder.Should().Be(DefaultBasePrice);
        orderItem.Quantity.Should().Be(DefaultQuantity);
        orderItem.SelectedCustomizations.Should().BeEmpty();
        
        // Verify calculated line item total
        var expectedLineTotal = new Money(DefaultBasePrice.Amount * DefaultQuantity, Currencies.Default);
        orderItem.LineItemTotal.Should().Be(expectedLineTotal);
    }

    [Test]
    public void Create_WithCustomizations_ShouldSucceedAndIncludeCustomizations()
    {
        // Arrange
        var customizations = new List<OrderItemCustomization> { DefaultCustomization };

        // Act
        var result = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity,
            customizations);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var orderItem = result.Value;
        
        orderItem.SelectedCustomizations.Should().HaveCount(1);
        orderItem.SelectedCustomizations.Should().Contain(DefaultCustomization);
        
        // Verify calculated line item total includes customization price
        var customizationTotal = DefaultCustomization.Snapshot_ChoicePriceAdjustmentAtOrder.Amount;
        var expectedLineTotal = new Money((DefaultBasePrice.Amount + customizationTotal) * DefaultQuantity, Currencies.Default);
        orderItem.LineItemTotal.Should().Be(expectedLineTotal);
    }

    [Test]
    public void Create_WithMultipleCustomizations_ShouldCalculateCorrectTotal()
    {
        // Arrange
        var customization1 = CreateCustomization("Extra Cheese", 2.00m);
        var customization2 = CreateCustomization("Pepperoni", 3.50m);
        var customizations = new List<OrderItemCustomization> { customization1, customization2 };

        // Act
        var result = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity,
            customizations);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var orderItem = result.Value;
        
        orderItem.SelectedCustomizations.Should().HaveCount(2);
        
        // Verify calculated line item total includes all customizations
        var totalCustomizationCost = 2.00m + 3.50m; // Sum of customization prices
        var expectedLineTotal = new Money((DefaultBasePrice.Amount + totalCustomizationCost) * DefaultQuantity, Currencies.Default);
        orderItem.LineItemTotal.Should().Be(expectedLineTotal);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-5)]
    public void Create_WithInvalidQuantity_ShouldFailWithInvalidQuantityError(int invalidQuantity)
    {
        // Arrange & Act
        var result = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            DefaultItemName,
            DefaultBasePrice,
            invalidQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemInvalidQuantity);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithNullOrEmptyItemName_ShouldFailWithInvalidNameError(string invalidName)
    {
        // Arrange & Act
        var result = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            invalidName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemInvalidName);
    }

    [Test]
    public void Create_WithNullItemName_ShouldFailWithInvalidNameError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            null,
            DefaultBasePrice,
            DefaultQuantity);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemInvalidName);
    }

    #endregion

    #region Calculation Tests

    [Test]
    public void LineItemTotal_WithNoCustomizations_ShouldEqualBasePriceTimesQuantity()
    {
        // Arrange
        var basePrice = new Money(12.50m, Currencies.Default);
        var quantity = 3;

        // Act
        var orderItem = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            DefaultItemName,
            basePrice,
            quantity).Value;

        // Assert
        var expectedTotal = new Money(basePrice.Amount * quantity, Currencies.Default);
        orderItem.LineItemTotal.Should().Be(expectedTotal);
    }

    [Test]
    public void LineItemTotal_WithCustomizations_ShouldIncludeCustomizationCosts()
    {
        // Arrange
        var basePrice = new Money(10.00m, Currencies.Default);
        var quantity = 2;
        var customization = CreateCustomization("Large Size", 3.00m);
        var customizations = new List<OrderItemCustomization> { customization };

        // Act
        var orderItem = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            DefaultItemName,
            basePrice,
            quantity,
            customizations).Value;

        // Assert
        // (10.00 + 3.00) * 2 = 26.00
        var expectedTotal = new Money((basePrice.Amount + 3.00m) * quantity, Currencies.Default);
        orderItem.LineItemTotal.Should().Be(expectedTotal);
    }

    [Test]
    public void LineItemTotal_WithNegativeCustomizationAdjustment_ShouldCalculateCorrectly()
    {
        // Arrange
        var basePrice = new Money(15.00m, Currencies.Default);
        var quantity = 1;
        var discount = CreateCustomization("Student Discount", -2.00m);
        var customizations = new List<OrderItemCustomization> { discount };

        // Act
        var orderItem = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            DefaultItemName,
            basePrice,
            quantity,
            customizations).Value;

        // Assert
        // (15.00 - 2.00) * 1 = 13.00
        var expectedTotal = new Money((basePrice.Amount - 2.00m) * quantity, Currencies.Default);
        orderItem.LineItemTotal.Should().Be(expectedTotal);
    }

    #endregion

    #region Helper Methods

    private static OrderItemCustomization CreateDefaultCustomization()
    {
        return CreateCustomization("Extra Cheese", 2.50m);
    }

    private static OrderItemCustomization CreateCustomization(string choiceName, decimal priceAdjustment)
    {
        return OrderItemCustomization.Create(
            "Toppings",
            choiceName,
            new Money(priceAdjustment, Currencies.Default)).Value;
    }

    #endregion

    #region Property Immutability Tests

    [Test]
    public void SelectedCustomizations_ShouldBeReadOnly()
    {
        // Arrange
        var customizations = new List<OrderItemCustomization> { DefaultCustomization };

        // Act
        var orderItem = OrderItem.Create(
            DefaultMenuCategoryId,
            DefaultMenuItemId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity,
            customizations).Value;

        // Assert
        // Type check
        var property = typeof(OrderItem).GetProperty(nameof(OrderItem.SelectedCustomizations));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<OrderItemCustomization>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<OrderItemCustomization>)orderItem.SelectedCustomizations).Add(
            CreateCustomization("Extra Sauce", 1.50m));
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the order item
        customizations.Add(CreateCustomization("Extra Sauce", 1.50m));
        orderItem.SelectedCustomizations.Should().HaveCount(1);
    }

    #endregion
}
