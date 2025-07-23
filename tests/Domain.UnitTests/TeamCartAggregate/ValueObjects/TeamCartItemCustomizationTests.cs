using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate.ValueObjects;

[TestFixture]
public class TeamCartItemCustomizationTests
{
    private const string ValidCustomizationGroupName = "Size Options";
    private const string ValidChoiceName = "Large";
    private static readonly Money ValidPriceAdjustment = new Money(2.50m, "USD");

    [Test]
    public void Create_WithValidInputs_ShouldSucceed()
    {
        // Act
        var result = TeamCartItemCustomization.Create(
            ValidCustomizationGroupName,
            ValidChoiceName,
            ValidPriceAdjustment);

        // Assert
        result.ShouldBeSuccessful();
        var customization = result.Value;
        customization.Snapshot_CustomizationGroupName.Should().Be(ValidCustomizationGroupName);
        customization.Snapshot_ChoiceName.Should().Be(ValidChoiceName);
        customization.Snapshot_ChoicePriceAdjustmentAtOrder.Should().Be(ValidPriceAdjustment);
    }

    [Test]
    public void Create_WithEmptyCustomizationGroupName_ShouldFailWithInvalidCustomizationError()
    {
        // Act
        var result = TeamCartItemCustomization.Create(
            "",
            ValidChoiceName,
            ValidPriceAdjustment);

        // Assert
        result.ShouldBeFailure(TeamCartErrors.InvalidCustomization.Code);
    }

    [Test]
    public void Create_WithNullCustomizationGroupName_ShouldFailWithInvalidCustomizationError()
    {
        // Act
        var result = TeamCartItemCustomization.Create(
            null!,
            ValidChoiceName,
            ValidPriceAdjustment);

        // Assert
        result.ShouldBeFailure(TeamCartErrors.InvalidCustomization.Code);
    }

    [Test]
    public void Create_WithWhitespaceCustomizationGroupName_ShouldFailWithInvalidCustomizationError()
    {
        // Act
        var result = TeamCartItemCustomization.Create(
            "   ",
            ValidChoiceName,
            ValidPriceAdjustment);

        // Assert
        result.ShouldBeFailure(TeamCartErrors.InvalidCustomization.Code);
    }

    [Test]
    public void Create_WithEmptyChoiceName_ShouldFailWithInvalidCustomizationError()
    {
        // Act
        var result = TeamCartItemCustomization.Create(
            ValidCustomizationGroupName,
            "",
            ValidPriceAdjustment);

        // Assert
        result.ShouldBeFailure(TeamCartErrors.InvalidCustomization.Code);
    }

    [Test]
    public void Create_WithNullChoiceName_ShouldFailWithInvalidCustomizationError()
    {
        // Act
        var result = TeamCartItemCustomization.Create(
            ValidCustomizationGroupName,
            null!,
            ValidPriceAdjustment);

        // Assert
        result.ShouldBeFailure(TeamCartErrors.InvalidCustomization.Code);
    }

    [Test]
    public void Create_WithWhitespaceChoiceName_ShouldFailWithInvalidCustomizationError()
    {
        // Act
        var result = TeamCartItemCustomization.Create(
            ValidCustomizationGroupName,
            "   ",
            ValidPriceAdjustment);

        // Assert
        result.ShouldBeFailure(TeamCartErrors.InvalidCustomization.Code);
    }

    [Test]
    public void ToOrderItemCustomization_ShouldConvertCorrectly()
    {
        // Arrange
        var customization = TeamCartItemCustomization.Create(
            ValidCustomizationGroupName,
            ValidChoiceName,
            ValidPriceAdjustment).Value;

        // Act
        var orderCustomization = customization.ToOrderItemCustomization();

        // Assert
        orderCustomization.Snapshot_CustomizationGroupName.Should().Be(ValidCustomizationGroupName);
        orderCustomization.Snapshot_ChoiceName.Should().Be(ValidChoiceName);
        orderCustomization.Snapshot_ChoicePriceAdjustmentAtOrder.Should().Be(ValidPriceAdjustment);
    }

    [Test]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var customization1 = TeamCartItemCustomization.Create(
            ValidCustomizationGroupName,
            ValidChoiceName,
            ValidPriceAdjustment).Value;

        var customization2 = TeamCartItemCustomization.Create(
            ValidCustomizationGroupName,
            ValidChoiceName,
            ValidPriceAdjustment).Value;

        // Assert
        customization1.Should().Be(customization2);
        customization1.GetHashCode().Should().Be(customization2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var customization1 = TeamCartItemCustomization.Create(
            ValidCustomizationGroupName,
            ValidChoiceName,
            ValidPriceAdjustment).Value;

        var customization2 = TeamCartItemCustomization.Create(
            "Different Group",
            ValidChoiceName,
            ValidPriceAdjustment).Value;

        // Assert
        customization1.Should().NotBe(customization2);
    }
}
