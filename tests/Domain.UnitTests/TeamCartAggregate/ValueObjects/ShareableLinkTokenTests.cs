using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate.ValueObjects;

[TestFixture]
public class ShareableLinkTokenTests
{
    [Test]
    public void CreateUnique_WithValidDuration_ShouldCreateTokenWithCorrectExpiration()
    {
        // Arrange
        var validityHours = 24;
        var expectedExpiration = DateTime.UtcNow.AddHours(validityHours);

        // Act
        var token = ShareableLinkToken.CreateUnique(TimeSpan.FromHours(validityHours));

        // Assert
        token.Should().NotBeNull();
        token.Value.Should().NotBeNullOrEmpty();
        token.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
        token.IsExpired.Should().BeFalse();
    }

    [Test]
    public void CreateUnique_WithDefaultDuration_ShouldCreateTokenWithDefaultExpiration()
    {
        // Arrange
        var expectedExpiration = DateTime.UtcNow.AddHours(24); // Default is 24 hours

        // Act
        var token = ShareableLinkToken.CreateUnique(TimeSpan.FromHours(24));

        // Assert
        token.Should().NotBeNull();
        token.Value.Should().NotBeNullOrEmpty();
        token.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void IsExpired_WithFutureExpiration_ShouldReturnFalse()
    {
        // Arrange
        var token = ShareableLinkToken.CreateUnique(TimeSpan.FromHours(24)); // 24 hours in the future

        // Act & Assert
        token.IsExpired.Should().BeFalse();
    }

    [Test]
    public void IsExpired_WithPastExpiration_ShouldReturnTrue()
    {
        // Arrange - Use reflection to create a token with past expiration
        var pastExpiration = DateTime.UtcNow.AddHours(-1);
        var tokenValue = "test-token-value";
        
        var constructor = typeof(ShareableLinkToken).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(DateTime) },
            null);
            
        var token = constructor?.Invoke(new object[] { tokenValue, pastExpiration }) as ShareableLinkToken;

        // Act & Assert
        token.Should().NotBeNull();
        token!.IsExpired.Should().BeTrue();
    }

    [Test]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var tokenValue = "same-token-value";
        var expiresAt = DateTime.UtcNow.AddHours(24);
        
        var constructor = typeof(ShareableLinkToken).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(DateTime) },
            null);
            
        var token1 = constructor?.Invoke(new object[] { tokenValue, expiresAt }) as ShareableLinkToken;
        var token2 = constructor?.Invoke(new object[] { tokenValue, expiresAt }) as ShareableLinkToken;

        // Act & Assert
        token1.Should().Be(token2);
        token1!.GetHashCode().Should().Be(token2!.GetHashCode());
    }

    [Test]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddHours(24);
        
        var constructor = typeof(ShareableLinkToken).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(DateTime) },
            null);
            
        var token1 = constructor?.Invoke(new object[] { "token-value-1", expiresAt }) as ShareableLinkToken;
        var token2 = constructor?.Invoke(new object[] { "token-value-2", expiresAt }) as ShareableLinkToken;

        // Act & Assert
        token1.Should().NotBe(token2);
        token1!.GetHashCode().Should().NotBe(token2!.GetHashCode());
    }
}