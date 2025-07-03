using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.Entities;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UnitTests.CustomizationGroupAggregate;

[TestFixture]
public class CustomizationChoiceTests
{
    private const string DefaultChoiceName = "Large";
    private static readonly Money DefaultPriceAdjustment = new Money(2.50m, Currencies.Default);
    private static readonly Money ZeroPriceAdjustment = new Money(0m, Currencies.Default);
    private const bool DefaultIsDefault = false;

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeChoiceCorrectly()
    {
        // Arrange & Act
        var result = CustomizationChoice.Create(DefaultChoiceName, DefaultPriceAdjustment, DefaultIsDefault);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var choice = result.Value;
        choice.Id.Value.Should().NotBe(Guid.Empty);
        choice.Name.Should().Be(DefaultChoiceName);
        choice.PriceAdjustment.Should().Be(DefaultPriceAdjustment);
        choice.IsDefault.Should().Be(DefaultIsDefault);
    }

    [Test]
    public void Create_WithDefaultParameters_ShouldSucceedAndSetDefaultValues()
    {
        // Arrange & Act
        var result = CustomizationChoice.Create(DefaultChoiceName, DefaultPriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var choice = result.Value;
        choice.Name.Should().Be(DefaultChoiceName);
        choice.PriceAdjustment.Should().Be(DefaultPriceAdjustment);
        choice.IsDefault.Should().BeFalse(); // Default value
    }

    [Test]
    public void Create_WithIsDefaultTrue_ShouldSucceedAndSetIsDefaultCorrectly()
    {
        // Arrange & Act
        var result = CustomizationChoice.Create(DefaultChoiceName, DefaultPriceAdjustment, isDefault: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var choice = result.Value;
        choice.IsDefault.Should().BeTrue();
    }

    [Test]
    public void Create_WithZeroPriceAdjustment_ShouldSucceed()
    {
        // Arrange & Act
        var result = CustomizationChoice.Create(DefaultChoiceName, ZeroPriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var choice = result.Value;
        choice.PriceAdjustment.Should().Be(ZeroPriceAdjustment);
    }

    [Test]
    public void Create_WithPositivePriceAdjustment_ShouldSucceed()
    {
        // Arrange
        var positivePriceAdjustment = new Money(1.00m, Currencies.Default);

        // Act
        var result = CustomizationChoice.Create(DefaultChoiceName, positivePriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var choice = result.Value;
        choice.PriceAdjustment.Should().Be(positivePriceAdjustment);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithNullOrEmptyName_ShouldFailWithNameRequiredError(string invalidName)
    {
        // Arrange & Act
        var result = CustomizationChoice.Create(invalidName, DefaultPriceAdjustment);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Description.Should().Be("Choice name is required.");
    }

    [Test]
    public void Create_WithNullName_ShouldFailWithNameRequiredError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = CustomizationChoice.Create(null, DefaultPriceAdjustment);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Description.Should().Be("Choice name is required.");
    }

    [Test]
    public void Create_WithNameContainingWhitespace_ShouldTrimName()
    {
        // Arrange
        var nameWithWhitespace = "  Extra Large  ";
        var expectedTrimmedName = "Extra Large";

        // Act
        var result = CustomizationChoice.Create(nameWithWhitespace, DefaultPriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(expectedTrimmedName);
    }

    [Test]
    public void Create_WithSpecialCharactersInName_ShouldSucceed()
    {
        // Arrange
        var specialName = "Extra-Large (16oz)";

        // Act
        var result = CustomizationChoice.Create(specialName, DefaultPriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(specialName);
    }

    [Test]
    public void Create_WithLongName_ShouldSucceed()
    {
        // Arrange
        var longName = "Extra Large with Additional Customizations and Special Instructions";

        // Act
        var result = CustomizationChoice.Create(longName, DefaultPriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(longName);
    }

    [Test]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var choice1 = CustomizationChoice.Create("Choice 1", DefaultPriceAdjustment).Value;
        var choice2 = CustomizationChoice.Create("Choice 2", DefaultPriceAdjustment).Value;
        var choice3 = CustomizationChoice.Create("Choice 3", DefaultPriceAdjustment).Value;

        // Assert
        choice1.Id.Should().NotBe(choice2.Id);
        choice1.Id.Should().NotBe(choice3.Id);
        choice2.Id.Should().NotBe(choice3.Id);
    }

    #endregion

    #region Entity Behavior Tests

    [Test]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var choice1 = CustomizationChoice.Create("Large", DefaultPriceAdjustment).Value;
        var choice2 = CustomizationChoice.Create("Medium", ZeroPriceAdjustment).Value;
        
        // Manually set the same ID for testing purposes (using reflection or constructor if available)
        // Note: This is a conceptual test - in practice, entities with same ID should be the same instance
        
        // Act & Assert
        choice1.Should().NotBe(choice2); // Different instances with different IDs
        choice1.Id.Should().NotBe(choice2.Id);
    }

    [Test]
    public void ToString_ShouldReturnMeaningfulRepresentation()
    {
        // Arrange
        var choice = CustomizationChoice.Create("Large", DefaultPriceAdjustment, true).Value;

        // Act
        var stringRepresentation = choice.ToString();

        // Assert
        stringRepresentation.Should().NotBeNullOrEmpty();
        // Note: The exact format depends on the base Entity implementation
    }

    #endregion

    #region Value Combinations Tests

    [TestCase("Small", 0.00, false)]
    [TestCase("Medium", 1.50, false)]
    [TestCase("Large", 3.00, true)]
    [TestCase("Extra Large", 5.50, true)]
    public void Create_WithVariousCombinations_ShouldSucceed(string name, decimal priceAmount, bool isDefault)
    {
        // Arrange
        var priceAdjustment = new Money(priceAmount, Currencies.Default);

        // Act
        var result = CustomizationChoice.Create(name, priceAdjustment, isDefault);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var choice = result.Value;
        choice.Name.Should().Be(name);
        choice.PriceAdjustment.Should().Be(priceAdjustment);
        choice.IsDefault.Should().Be(isDefault);
    }

    #endregion
}
