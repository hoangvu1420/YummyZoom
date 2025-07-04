using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate.ValueObjects;

[TestFixture]
public class OrderItemCustomizationTests
{
    private const string DefaultGroupName = "Size Options";
    private const string DefaultChoiceName = "Large";
    private static readonly Money DefaultPriceAdjustment = new Money(2.50m, Currencies.Default);

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = OrderItemCustomization.Create(
            DefaultGroupName,
            DefaultChoiceName,
            DefaultPriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var customization = result.Value;
        
        customization.Snapshot_CustomizationGroupName.Should().Be(DefaultGroupName);
        customization.Snapshot_ChoiceName.Should().Be(DefaultChoiceName);
        customization.Snapshot_ChoicePriceAdjustmentAtOrder.Should().Be(DefaultPriceAdjustment);
    }

    [Test]
    public void Create_WithZeroPriceAdjustment_ShouldSucceed()
    {
        // Arrange
        var zeroPriceAdjustment = Money.Zero(Currencies.Default);

        // Act
        var result = OrderItemCustomization.Create(
            DefaultGroupName,
            DefaultChoiceName,
            zeroPriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Snapshot_ChoicePriceAdjustmentAtOrder.Should().Be(zeroPriceAdjustment);
    }

    [Test]
    public void Create_WithNegativePriceAdjustment_ShouldSucceed()
    {
        // Arrange
        var negativePriceAdjustment = new Money(-1.50m, Currencies.Default);

        // Act
        var result = OrderItemCustomization.Create(
            DefaultGroupName,
            DefaultChoiceName,
            negativePriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Snapshot_ChoicePriceAdjustmentAtOrder.Should().Be(negativePriceAdjustment);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithEmptyGroupName_ShouldFailWithInvalidError(string invalidGroupName)
    {
        // Arrange & Act
        var result = OrderItemCustomization.Create(
            invalidGroupName,
            DefaultChoiceName,
            DefaultPriceAdjustment);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemCustomizationInvalid);
    }

    [Test]
    public void Create_WithNullGroupName_ShouldFailWithInvalidError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = OrderItemCustomization.Create(
            null,
            DefaultChoiceName,
            DefaultPriceAdjustment);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemCustomizationInvalid);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithEmptyChoiceName_ShouldFailWithInvalidError(string invalidChoiceName)
    {
        // Arrange & Act
        var result = OrderItemCustomization.Create(
            DefaultGroupName,
            invalidChoiceName,
            DefaultPriceAdjustment);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemCustomizationInvalid);
    }

    [Test]
    public void Create_WithNullChoiceName_ShouldFailWithInvalidError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = OrderItemCustomization.Create(
            DefaultGroupName,
            null,
            DefaultPriceAdjustment);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemCustomizationInvalid);
    }

    [Test]
    public void Create_WithBothNamesEmpty_ShouldFailWithInvalidError()
    {
        // Arrange & Act
        var result = OrderItemCustomization.Create(
            "",
            "",
            DefaultPriceAdjustment);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemCustomizationInvalid);
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var customization1 = OrderItemCustomization.Create(
            DefaultGroupName,
            DefaultChoiceName,
            DefaultPriceAdjustment).Value;

        var customization2 = OrderItemCustomization.Create(
            DefaultGroupName,
            DefaultChoiceName,
            DefaultPriceAdjustment).Value;

        // Act & Assert
        customization1.Should().Be(customization2);
        customization1.Equals(customization2).Should().BeTrue();
        (customization1 == customization2).Should().BeTrue();
        (customization1 != customization2).Should().BeFalse();
    }

    [Test]
    public void Equality_WithDifferentGroupNames_ShouldNotBeEqual()
    {
        // Arrange
        var customization1 = OrderItemCustomization.Create(
            "Group 1",
            DefaultChoiceName,
            DefaultPriceAdjustment).Value;

        var customization2 = OrderItemCustomization.Create(
            "Group 2",
            DefaultChoiceName,
            DefaultPriceAdjustment).Value;

        // Act & Assert
        customization1.Should().NotBe(customization2);
        customization1.Equals(customization2).Should().BeFalse();
        (customization1 == customization2).Should().BeFalse();
        (customization1 != customization2).Should().BeTrue();
    }

    [Test]
    public void Equality_WithDifferentChoiceNames_ShouldNotBeEqual()
    {
        // Arrange
        var customization1 = OrderItemCustomization.Create(
            DefaultGroupName,
            "Choice 1",
            DefaultPriceAdjustment).Value;

        var customization2 = OrderItemCustomization.Create(
            DefaultGroupName,
            "Choice 2",
            DefaultPriceAdjustment).Value;

        // Act & Assert
        customization1.Should().NotBe(customization2);
    }

    [Test]
    public void Equality_WithDifferentPriceAdjustments_ShouldNotBeEqual()
    {
        // Arrange
        var customization1 = OrderItemCustomization.Create(
            DefaultGroupName,
            DefaultChoiceName,
            new Money(1.00m, Currencies.Default)).Value;

        var customization2 = OrderItemCustomization.Create(
            DefaultGroupName,
            DefaultChoiceName,
            new Money(2.00m, Currencies.Default)).Value;

        // Act & Assert
        customization1.Should().NotBe(customization2);
    }

    [Test]
    public void Equality_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var customization = OrderItemCustomization.Create(
            DefaultGroupName,
            DefaultChoiceName,
            DefaultPriceAdjustment).Value;

        // Act & Assert
        customization.Equals(null).Should().BeFalse();
        (customization is null).Should().BeFalse();
    }

    #endregion
}
