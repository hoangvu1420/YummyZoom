using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Serialization.JsonOptions;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

/// <summary>
/// Extension methods for configuring JSONB collections in EF Core entity configurations.
/// Provides reusable conversion and comparison logic for IReadOnlyList properties.
/// </summary>
public static class EfJsonbExtensions
{
    /// <summary>
    /// Configures a property as a JSONB column with automatic JSON conversion and change tracking.
    /// Uses the shared DomainJson.Options which includes automatic handling of strongly-typed IDs.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    /// <param name="propertyBuilder">The property builder for the IReadOnlyList property</param>
    /// <param name="options">Optional JsonSerializerOptions. If null, uses DomainJson.Options</param>
    /// <returns>The property builder for method chaining</returns>
    public static PropertyBuilder<IReadOnlyList<T>> HasJsonbListConversion<T>(
        this PropertyBuilder<IReadOnlyList<T>> propertyBuilder,
        JsonSerializerOptions? options = null)
    {
        options ??= DomainJson.Options;

        return propertyBuilder
            .HasColumnType("jsonb")
            .HasConversion(
                // Serialize to JSON string
                collection => JsonSerializer.Serialize(collection, options),
                // Deserialize from JSON string
                json => (IReadOnlyList<T>)(JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>()),
                // Value comparer for change tracking
                new ValueComparer<IReadOnlyList<T>>(
                    // Equality comparison
                    (left, right) => left!.SequenceEqual(right!),
                    // Hash code generation
                    collection => collection.Aggregate(0, (hash, item) => HashCode.Combine(hash, item == null ? 0 : item.GetHashCode())),
                    // Snapshot creation (deep copy)
                    collection => collection.ToList()
                )
            );
    }
}
