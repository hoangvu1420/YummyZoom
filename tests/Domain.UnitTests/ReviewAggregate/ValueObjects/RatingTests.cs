using YummyZoom.Domain.ReviewAggregate.Errors;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.ReviewAggregate.ValueObjects;

[TestFixture]
public class RatingTests
{
    #region Create() Method Tests - Valid Values

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Create_WithValidRating_ShouldSucceedAndReturnCorrectValue(int validRating)
    {
        // Act
        var result = Rating.Create(validRating);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Value.Should().Be(validRating);
    }

    #endregion

    #region Create() Method Tests - Invalid Values

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-5)]
    [TestCase(6)]
    [TestCase(10)]
    [TestCase(100)]
    public void Create_WithInvalidRating_ShouldFailWithInvalidRatingError(int invalidRating)
    {
        // Act
        var result = Rating.Create(invalidRating);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(ReviewErrors.InvalidRating);
    }

    [Test]
    public void Create_WithMinimumBoundaryViolation_ShouldFailWithInvalidRatingError()
    {
        // Arrange
        const int belowMinimum = 0;

        // Act
        var result = Rating.Create(belowMinimum);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(ReviewErrors.InvalidRating);
    }

    [Test]
    public void Create_WithMaximumBoundaryViolation_ShouldFailWithInvalidRatingError()
    {
        // Arrange
        const int aboveMaximum = 6;

        // Act
        var result = Rating.Create(aboveMaximum);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(ReviewErrors.InvalidRating);
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var rating1 = Rating.Create(4).Value;
        var rating2 = Rating.Create(4).Value;

        // Act & Assert
        rating1.Equals(rating2).Should().BeTrue();
        (rating1 == rating2).Should().BeTrue();
        (rating1 != rating2).Should().BeFalse();
    }

    [Test]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var rating1 = Rating.Create(3).Value;
        var rating2 = Rating.Create(5).Value;

        // Act & Assert
        rating1.Equals(rating2).Should().BeFalse();
        (rating1 == rating2).Should().BeFalse();
        (rating1 != rating2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var rating = Rating.Create(4).Value;

        // Act & Assert
        rating.Equals(null).Should().BeFalse();
        (rating is null).Should().BeFalse();
        (rating is not null).Should().BeTrue();
    }

    [Test]
    public void GetHashCode_WithSameValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var rating1 = Rating.Create(3).Value;
        var rating2 = Rating.Create(3).Value;

        // Act & Assert
        rating1.GetHashCode().Should().Be(rating2.GetHashCode());
    }

    [Test]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var rating1 = Rating.Create(1).Value;
        var rating2 = Rating.Create(5).Value;

        // Act & Assert
        rating1.GetHashCode().Should().NotBe(rating2.GetHashCode());
    }

    #endregion

    #region Business Logic Tests

    [Test]
    public void Rating_ShouldBeImmutable()
    {
        // Arrange
        var rating = Rating.Create(4).Value;
        var originalValue = rating.Value;

        // Act & Assert
        rating.Value.Should().Be(originalValue);
        // Note: Since Value has a private setter, it's already immutable by design
        // This test documents the intended immutability behavior
    }

    [Test]
    public void AllValidRatings_ShouldCreateSuccessfully()
    {
        // Arrange
        var validRatings = new[] { 1, 2, 3, 4, 5 };
        var results = new List<Rating>();

        // Act
        foreach (var validRating in validRatings)
        {
            var result = Rating.Create(validRating);
            result.IsSuccess.Should().BeTrue($"Rating {validRating} should be valid");
            results.Add(result.Value);
        }

        // Assert
        results.Should().HaveCount(5);
        results.Select(r => r.Value).Should().BeEquivalentTo(validRatings);
    }

    #endregion
}
