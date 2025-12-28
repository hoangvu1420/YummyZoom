using YummyZoom.Domain.RestaurantAggregate.Errors;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.RestaurantAggregate.ValueObjects;

/// <summary>
/// Tests for BusinessHours value object creation, validation, and equality.
/// </summary>
[TestFixture]
public class BusinessHoursTests
{
    private const string ValidBusinessHours = "09:00-22:00";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidHours_ShouldSucceedAndInitializeBusinessHoursCorrectly()
    {
        // Arrange & Act
        var result = BusinessHours.Create(ValidBusinessHours);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var businessHours = result.Value;

        businessHours.Should().NotBeNull();
        businessHours.Hours.Should().Be(ValidBusinessHours);
    }

    [Test]
    public void Create_WithNullOrEmptyHours_ShouldFailWithBusinessHoursFormatRequiredError()
    {
        // Arrange & Act
        var result = BusinessHours.Create(string.Empty);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(RestaurantErrors.BusinessHoursFormatIsRequired());
    }

    [Test]
    public void Create_WithNullHours_ShouldFailWithBusinessHoursFormatRequiredError()
    {
        // Arrange & Act
        var result = BusinessHours.Create(null!);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(RestaurantErrors.BusinessHoursFormatIsRequired());
    }

    [Test]
    public void Create_WithWhitespaceOnlyHours_ShouldFailWithBusinessHoursFormatRequiredError()
    {
        // Arrange & Act
        var result = BusinessHours.Create("   ");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(RestaurantErrors.BusinessHoursFormatIsRequired());
    }

    [Test]
    public void Create_WithTooLongHours_ShouldFailWithBusinessHoursFormatTooLongError()
    {
        // Arrange
        var tooLongHours = new string('A', 201); // 201 characters, exceeds max length of 200

        // Act
        var result = BusinessHours.Create(tooLongHours);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(RestaurantErrors.BusinessHoursFormatTooLong(200));
    }

    [Test]
    public void Create_WithMaxLengthHours_ShouldSucceed()
    {
        // Arrange
        var paddingLength = 200 - ValidBusinessHours.Length;
        var maxLengthHours = ValidBusinessHours + new string(' ', paddingLength); // Exactly 200 characters

        // Act
        var result = BusinessHours.Create(maxLengthHours);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Hours.Should().Be(ValidBusinessHours);
    }

    [Test]
    public void Create_WithWhitespaceAroundInput_ShouldTrimAndSucceed()
    {
        // Arrange
        var hoursWithWhitespace = "  " + ValidBusinessHours + "  ";

        // Act
        var result = BusinessHours.Create(hoursWithWhitespace);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var businessHours = result.Value;

        businessHours.Hours.Should().Be(ValidBusinessHours);
    }

    [Test]
    [TestCase("00:00-23:59")]
    [TestCase("09:30-17:45")]
    [TestCase("06:15-14:30")]
    [TestCase("18:00-23:00")]
    [TestCase("12:00-13:00")]
    public void Create_WithVariousValidFormats_ShouldSucceed(string hours)
    {
        // Arrange & Act
        var result = BusinessHours.Create(hours);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Hours.Should().Be(hours);
    }

    [Test]
    [TestCase("9-5")]
    [TestCase("Monday to Friday: 8:00 - 17:00")]
    [TestCase("24/7")]
    [TestCase("Closed on Sundays")]
    [TestCase("Mon-Fri: 9-5, Sat: 10-3, Sun: Closed")]
    [TestCase("25:00-17:00")]
    [TestCase("09:60-17:00")]
    [TestCase("17:00-09:00")]
    [TestCase("9:00 AM - 5:00 PM")]
    public void Create_WithInvalidFormats_ShouldFail(string hours)
    {
        // Arrange & Act
        var result = BusinessHours.Create(hours);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Restaurant.BusinessHours.InvalidFormat");
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var businessHours1 = BusinessHours.Create(ValidBusinessHours).Value;
        var businessHours2 = BusinessHours.Create(ValidBusinessHours).Value;

        // Act & Assert
        businessHours1.Should().Be(businessHours2);
        businessHours1.Equals(businessHours2).Should().BeTrue();
        (businessHours1 == businessHours2).Should().BeTrue();
        (businessHours1 != businessHours2).Should().BeFalse();
    }

    [Test]
    public void Equals_WithDifferentHours_ShouldReturnFalse()
    {
        // Arrange
        var businessHours1 = BusinessHours.Create(ValidBusinessHours).Value;
        var businessHours2 = BusinessHours.Create("10:00-21:00").Value;

        // Act & Assert
        businessHours1.Should().NotBe(businessHours2);
        businessHours1.Equals(businessHours2).Should().BeFalse();
        (businessHours1 == businessHours2).Should().BeFalse();
        (businessHours1 != businessHours2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var businessHours = BusinessHours.Create(ValidBusinessHours).Value;

        // Act & Assert
        businessHours.Equals(null).Should().BeFalse();
    }

    [Test]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var businessHours1 = BusinessHours.Create(ValidBusinessHours).Value;
        var businessHours2 = BusinessHours.Create(ValidBusinessHours).Value;

        // Act & Assert
        businessHours1.GetHashCode().Should().Be(businessHours2.GetHashCode());
    }

    [Test]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var businessHours1 = BusinessHours.Create(ValidBusinessHours).Value;
        var businessHours2 = BusinessHours.Create("10:00-21:00").Value;

        // Act & Assert
        businessHours1.GetHashCode().Should().NotBe(businessHours2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Test]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var businessHours = BusinessHours.Create(ValidBusinessHours).Value;

        // Act
        var result = businessHours.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        // Since ValueObject doesn't override ToString by default, we just check it returns something
    }

    #endregion
}
