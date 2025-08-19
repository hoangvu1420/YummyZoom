using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Serialization;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Persistence;

/// <summary>
/// Tests for MenuItem JSON serialization to verify the new shared JSON infrastructure works correctly.
/// Focuses on testing JSONB column content, converter functionality, and database storage format.
/// </summary>
public class MenuItemJsonSerializationTests : BaseTestFixture
{
    private RestaurantId _restaurantId = null!;
    private MenuCategoryId _menuCategoryId = null!;

    [SetUp]
    public void SetUp()
    {
        _restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
        _menuCategoryId = MenuCategoryId.Create(Testing.TestData.GetMenuCategoryId("Main Dishes"));
    }

    #region JSON Structure Validation Tests

    [Test]
    public async Task DietaryTagIds_ShouldSerializeAsGuidArray()
    {
        // Arrange
        var dietaryTags = new List<TagId>
        {
            TagId.Create(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            TagId.Create(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            TagId.Create(Guid.Parse("33333333-3333-3333-3333-333333333333"))
        };
        var menuItem = CreateMenuItemWithDietaryTags(dietaryTags);

        // Act
        await AddAsync(menuItem);

        // Assert
        var jsonContent = await GetJsonbColumnContent(menuItem.Id, "DietaryTagIds");
        jsonContent.Should().NotBeNullOrEmpty();

        // Verify JSON structure - should be an array of GUIDs
        var jsonArray = JsonDocument.Parse(jsonContent).RootElement;
        jsonArray.ValueKind.Should().Be(JsonValueKind.Array);
        jsonArray.GetArrayLength().Should().Be(3);

        // Verify each element is a valid GUID string
        var guids = jsonArray.EnumerateArray()
            .Select(element => Guid.Parse(element.GetString()!))
            .ToList();

        guids.Should().Contain(dietaryTags.Select(t => t.Value));
    }

    [Test]
    public async Task AppliedCustomizations_ShouldSerializeWithAllProperties()
    {
        // Arrange
        var customizations = new List<AppliedCustomization>
        {
            AppliedCustomization.Create(
                CustomizationGroupId.Create(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                "Cheese Options",
                1),
            AppliedCustomization.Create(
                CustomizationGroupId.Create(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
                "Sauce Selection",
                2)
        };
        var menuItem = CreateMenuItemWithCustomizations(customizations);

        // Act
        await AddAsync(menuItem);

        // Assert
        var jsonContent = await GetJsonbColumnContent(menuItem.Id, "AppliedCustomizations");
        jsonContent.Should().NotBeNullOrEmpty();

        // Parse and verify JSON structure
        var jsonArray = JsonDocument.Parse(jsonContent).RootElement;
        jsonArray.ValueKind.Should().Be(JsonValueKind.Array);
        jsonArray.GetArrayLength().Should().Be(2);

        // Verify first customization object structure
        var firstCustomization = jsonArray[0];
        firstCustomization.GetProperty("customizationGroupId").GetString()
            .Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        firstCustomization.GetProperty("displayTitle").GetString()
            .Should().Be("Cheese Options");
        firstCustomization.GetProperty("displayOrder").GetInt32()
            .Should().Be(1);

        // Verify second customization object structure
        var secondCustomization = jsonArray[1];
        secondCustomization.GetProperty("customizationGroupId").GetString()
            .Should().Be("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        secondCustomization.GetProperty("displayTitle").GetString()
            .Should().Be("Sauce Selection");
        secondCustomization.GetProperty("displayOrder").GetInt32()
            .Should().Be(2);
    }

    [Test]
    public async Task EmptyCollections_ShouldSerializeAsEmptyArrays()
    {
        // Arrange
        var menuItem = CreateBasicMenuItem();

        // Act
        await AddAsync(menuItem);

        // Assert
        var dietaryTagsJson = await GetJsonbColumnContent(menuItem.Id, "DietaryTagIds");
        var customizationsJson = await GetJsonbColumnContent(menuItem.Id, "AppliedCustomizations");

        // Both should be empty arrays, not null
        dietaryTagsJson.Should().Be("[]");
        customizationsJson.Should().Be("[]");
    }

    #endregion

    #region Converter Functionality Tests

    [Test]
    public async Task TagId_ShouldUseAggregateRootIdConverter()
    {
        // Arrange
        var knownGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var tagId = TagId.Create(knownGuid);
        var dietaryTags = new List<TagId> { tagId };
        var menuItem = CreateMenuItemWithDietaryTags(dietaryTags);

        // Act
        await AddAsync(menuItem);

        // Assert
        var jsonContent = await GetJsonbColumnContent(menuItem.Id, "DietaryTagIds");
        
        // Verify that the TagId was serialized as its underlying GUID value
        jsonContent.Should().Contain("12345678-1234-1234-1234-123456789abc");
        
        // Verify round-trip: retrieve and check the TagId
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved!.DietaryTagIds.First().Value.Should().Be(knownGuid);
    }

    [Test]
    public async Task CustomizationGroupId_ShouldUseAggregateRootIdConverter()
    {
        // Arrange
        var knownGuid = Guid.Parse("87654321-4321-4321-4321-fedcba987654");
        var customizationGroupId = CustomizationGroupId.Create(knownGuid);
        var customization = AppliedCustomization.Create(customizationGroupId, "Test Group", 1);
        var menuItem = CreateMenuItemWithCustomizations(new List<AppliedCustomization> { customization });

        // Act
        await AddAsync(menuItem);

        // Assert
        var jsonContent = await GetJsonbColumnContent(menuItem.Id, "AppliedCustomizations");
        
        // Verify that the CustomizationGroupId was serialized as its underlying GUID value
        jsonContent.Should().Contain("87654321-4321-4321-4321-fedcba987654");
        
        // Verify round-trip: retrieve and check the CustomizationGroupId
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved!.AppliedCustomizations.First().CustomizationGroupId.Value.Should().Be(knownGuid);
    }

    [Test]
    public async Task AppliedCustomization_ShouldUseJsonConstructor()
    {
        // Arrange
        var customization = AppliedCustomization.Create(
            CustomizationGroupId.CreateUnique(),
            "Json Constructor Test",
            42);
        var menuItem = CreateMenuItemWithCustomizations(new List<AppliedCustomization> { customization });

        // Act
        await AddAsync(menuItem);

        // Assert - Verify that deserialization works correctly (proving JsonConstructor is used)
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        
        var retrievedCustomization = retrieved!.AppliedCustomizations.First();
        retrievedCustomization.DisplayTitle.Should().Be("Json Constructor Test");
        retrievedCustomization.DisplayOrder.Should().Be(42);
        retrievedCustomization.CustomizationGroupId.Should().Be(customization.CustomizationGroupId);
    }

    #endregion

    #region Database Storage Verification Tests

    [Test]
    public async Task MenuItem_JsonbColumns_ShouldContainValidJson()
    {
        // Arrange
        var dietaryTags = CreateTestDietaryTags();
        var customizations = CreateTestAppliedCustomizations();
        var menuItem = CreateMenuItemWithBothCollections(dietaryTags, customizations);

        // Act
        await AddAsync(menuItem);

        // Assert
        var dietaryTagsJson = await GetJsonbColumnContent(menuItem.Id, "DietaryTagIds");
        var customizationsJson = await GetJsonbColumnContent(menuItem.Id, "AppliedCustomizations");

        // Verify both are valid JSON
        Action parseDietaryTags = () => JsonDocument.Parse(dietaryTagsJson);
        Action parseCustomizations = () => JsonDocument.Parse(customizationsJson);

        parseDietaryTags.Should().NotThrow("DietaryTagIds should contain valid JSON");
        parseCustomizations.Should().NotThrow("AppliedCustomizations should contain valid JSON");

        // Verify they're not just empty objects
        dietaryTagsJson.Should().NotBe("{}");
        customizationsJson.Should().NotBe("{}");
    }

    [Test]
    public async Task MenuItem_JsonbColumns_ShouldBeQueryable()
    {
        // Arrange
        var specificGuid = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var specificTag = TagId.Create(specificGuid);
        var menuItem = CreateMenuItemWithDietaryTags(new List<TagId> { specificTag });

        // Act
        await AddAsync(menuItem);

        // Assert - Use raw SQL to verify we can query the JSONB content
        var foundMenuItemId = await QueryJsonbContent(specificGuid);
        foundMenuItemId.Should().Be(menuItem.Id.Value);
    }

    [Test]
    public void DomainJson_Options_ShouldBeUsedForSerialization()
    {
        // Arrange
        var customization = AppliedCustomization.Create(
            CustomizationGroupId.CreateUnique(),
            "Test Serialization Options",
            1);

        // Act - Serialize using DomainJson.Options
        var serialized = JsonSerializer.Serialize(customization, DomainJson.Options);
        var deserialized = JsonSerializer.Deserialize<AppliedCustomization>(serialized, DomainJson.Options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.DisplayTitle.Should().Be(customization.DisplayTitle);
        deserialized.DisplayOrder.Should().Be(customization.DisplayOrder);
        deserialized.CustomizationGroupId.Should().Be(customization.CustomizationGroupId);
    }

    #endregion

    #region Helper Methods

    private MenuItem CreateBasicMenuItem()
    {
        var result = MenuItem.Create(
            _restaurantId,
            _menuCategoryId,
            "JSON Test Menu Item",
            "A test menu item for JSON serialization testing",
            new Money(15.99m, "USD"));

        result.ShouldBeSuccessful();
        return result.Value;
    }

    private MenuItem CreateMenuItemWithDietaryTags(List<TagId> dietaryTags)
    {
        var result = MenuItem.Create(
            _restaurantId,
            _menuCategoryId,
            "JSON Test Menu Item with Tags",
            "A test menu item with dietary tags for JSON testing",
            new Money(15.99m, "USD"),
            dietaryTagIds: dietaryTags);

        result.ShouldBeSuccessful();
        return result.Value;
    }

    private MenuItem CreateMenuItemWithCustomizations(List<AppliedCustomization> customizations)
    {
        var result = MenuItem.Create(
            _restaurantId,
            _menuCategoryId,
            "JSON Test Menu Item with Customizations",
            "A test menu item with applied customizations for JSON testing",
            new Money(15.99m, "USD"),
            appliedCustomizations: customizations);

        result.ShouldBeSuccessful();
        return result.Value;
    }

    private MenuItem CreateMenuItemWithBothCollections(List<TagId> dietaryTags, List<AppliedCustomization> customizations)
    {
        var result = MenuItem.Create(
            _restaurantId,
            _menuCategoryId,
            "JSON Test Menu Item with Both Collections",
            "A test menu item with both collections for JSON testing",
            new Money(15.99m, "USD"),
            dietaryTagIds: dietaryTags,
            appliedCustomizations: customizations);

        result.ShouldBeSuccessful();
        return result.Value;
    }

    private static List<TagId> CreateTestDietaryTags()
    {
        return new List<TagId>
        {
            TagId.CreateUnique(),
            TagId.CreateUnique()
        };
    }

    private static List<AppliedCustomization> CreateTestAppliedCustomizations()
    {
        return new List<AppliedCustomization>
        {
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "JSON Test Group 1", 1),
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "JSON Test Group 2", 2)
        };
    }

    /// <summary>
    /// Gets the raw JSONB content from the database for a specific column.
    /// </summary>
    private async Task<string> GetJsonbColumnContent(MenuItemId menuItemId, string columnName)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var sql = $@"
            SELECT ""{columnName}""::text 
            FROM ""MenuItems"" 
            WHERE ""Id"" = @menuItemId";

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@menuItemId", menuItemId.Value));

        if (command.Connection!.State != ConnectionState.Open)
            await command.Connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Queries JSONB content to find a menu item by a specific tag ID.
    /// </summary>
    private async Task<Guid> QueryJsonbContent(Guid tagId)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sql = @"
            SELECT ""Id"" 
            FROM ""MenuItems"" 
            WHERE ""DietaryTagIds"" @> @tagIdJson::jsonb";

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@tagIdJson", $"[\"{tagId}\"]"));

        if (command.Connection!.State != ConnectionState.Open)
            await command.Connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();
        return (Guid)result!;
    }

    #endregion
}
