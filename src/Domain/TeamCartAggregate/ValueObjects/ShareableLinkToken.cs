using System.Security.Cryptography;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Domain.TeamCartAggregate.ValueObjects;

/// <summary>
/// Represents a shareable token used for joining a team cart.
/// </summary>
public sealed class ShareableLinkToken : ValueObject
{
    /// <summary>
    /// Gets the token value.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Gets the expiration time of the token.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareableLinkToken"/> class.
    /// </summary>
    /// <param name="value">The token value.</param>
    /// <param name="expiresAt">The expiration time of the token.</param>
    private ShareableLinkToken(string value, DateTime expiresAt)
    {
        Value = value;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private ShareableLinkToken() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new unique shareable link token valid for the specified duration.
    /// </summary>
    /// <param name="validFor">The duration for which the token is valid.</param>
    /// <returns>A new unique shareable link token.</returns>
    public static ShareableLinkToken CreateUnique(TimeSpan validFor)
    {
        // Generate a random token (8 characters is sufficient for this use case)
        var tokenBytes = RandomNumberGenerator.GetBytes(6); // 6 bytes = 8 chars in base64
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("/", "_")  // Make URL-safe
            .Replace("+", "-")
            .Substring(0, 8);   // Take first 8 chars for a short, readable token

        return new ShareableLinkToken(token, DateTime.UtcNow.Add(validFor));
    }

    /// <summary>
    /// Checks if the token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Gets the equality components for the value object.
    /// </summary>
    /// <returns>An enumerable of objects representing the equality components.</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return ExpiresAt;
    }
}