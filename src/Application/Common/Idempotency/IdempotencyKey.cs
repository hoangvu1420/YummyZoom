using System.Text.RegularExpressions;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Idempotency;

/// <summary>
/// Represents an idempotency key used to ensure operations are executed only once.
/// Must be a valid UUID v4 format.
/// </summary>
public sealed class IdempotencyKey : IEquatable<IdempotencyKey>
{
    private static readonly Regex UuidV4Regex = new(
        @"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Value { get; }

    private IdempotencyKey(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an IdempotencyKey from a string value.
    /// </summary>
    /// <param name="value">The idempotency key value (must be a valid UUID v4)</param>
    /// <returns>A Result containing the IdempotencyKey or an error</returns>
    public static Result<IdempotencyKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<IdempotencyKey>(IdempotencyErrors.KeyRequired);
        }

        if (!UuidV4Regex.IsMatch(value))
        {
            return Result.Failure<IdempotencyKey>(IdempotencyErrors.InvalidKeyFormat);
        }

        return Result.Success(new IdempotencyKey(value.ToLowerInvariant()));
    }

    /// <summary>
    /// Generates a new random IdempotencyKey.
    /// </summary>
    public static IdempotencyKey Generate()
    {
        return new IdempotencyKey(Guid.NewGuid().ToString());
    }

    public bool Equals(IdempotencyKey? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as IdempotencyKey);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString() => Value;

    public static bool operator ==(IdempotencyKey? left, IdempotencyKey? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(IdempotencyKey? left, IdempotencyKey? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Error definitions for IdempotencyKey operations.
/// </summary>
public static class IdempotencyErrors
{
    public static readonly Error KeyRequired = Error.Validation(
        "IdempotencyKey.Required",
        "Idempotency key is required for this operation.");

    public static readonly Error InvalidKeyFormat = Error.Validation(
        "IdempotencyKey.InvalidFormat",
        "Idempotency key must be a valid UUID v4 format.");

    public static readonly Error DuplicateRequest = Error.Conflict(
        "IdempotencyKey.DuplicateRequest",
        "A request with this idempotency key is already being processed or has been completed.");
}
