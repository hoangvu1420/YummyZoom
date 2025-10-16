using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetRestaurantPublicInfoQueryTests : BaseTestFixture
{
    [Test]
    public async Task Success_ReturnsBasicDtoAndParsesCuisineTags()
    {
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        restaurant.Should().NotBeNull();

        // Seed CuisineTags as JSONB text via raw SQL easiest path: update EF entity shadow not available → update columns through raw SQL
        // Instead, update simple scalar fields we can: mark IsDeleted=false already default; set City/Area via domain replace methods not present.
        // Use direct connection via scope to set JSONB CuisineTags and city/area columns on Restaurants table.
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = restaurant!.Id.Value;
        var phone = "+1";
        var background = string.Empty;
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Restaurants"" 
            SET 
                ""Location_City"" = {"Metro"}, 
                ""Location_State"" = {"ST"}, 
                ""Location_ZipCode"" = {"00000"}, 
                ""Location_Country"" = {"US"}, 
                ""Location_Street"" = {"1 Test"}, 
                ""BackgroundImageUrl"" = {background}, 
                ""CuisineType"" = {"Multi"}, 
                ""BusinessHours"" = {"9-5"}, 
                ""ContactInfo_PhoneNumber"" = {phone}, 
                ""ContactInfo_Email"" = {"a@b.c"}
            WHERE ""Id"" = {id}");

        var result = await SendAsync(new GetRestaurantPublicInfoQuery(id));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.RestaurantId.Should().Be(id);
        dto.Name.Should().NotBeNullOrWhiteSpace();
        dto.CuisineTags.Should().BeEmpty();
        dto.Address.City.Should().Be("Metro");
    }

    [Test]
    public async Task NotFound_WhenDeleted()
    {
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        restaurant.Should().NotBeNull();

        // Soft-delete directly via SQL because default restaurant is verified
        // and domain rules prevent deletion without explicit confirmation
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var id = restaurant!.Id.Value;
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants"" 
                SET ""IsDeleted"" = {true}, ""DeletedOn"" = {DateTimeOffset.UtcNow}
                WHERE ""Id"" = {id}");
        }

        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetRestaurantPublicInfoErrors.NotFound);
    }

    [Test]
    public async Task NotFound_WhenMissing()
    {
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(Guid.NewGuid()));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetRestaurantPublicInfoErrors.NotFound);
    }

    [Test]
    public async Task Validation_EmptyRestaurantId_ShouldThrow()
    {
        var act = async () => await SendAsync(new GetRestaurantPublicInfoQuery(Guid.Empty));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Caching_ReturnsCachedValue_OnSecondCallEvenAfterDbChange()
    {
        await Testing.ResetState();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        // First call (miss -> populate cache)
        var first = await Testing.SendAndUnwrapAsync(new GetRestaurantPublicInfoQuery(restaurantId));

        // Mutate DB directly to simulate a change after first read
        var updatedName = first.Name + "_UPDATED";
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Restaurants\" SET \"Name\" = {updatedName} WHERE \"Id\" = {restaurantId}");
        }

        // Second call should hit cache and not reflect update
        var second = await Testing.SendAndUnwrapAsync(new GetRestaurantPublicInfoQuery(restaurantId));
        second.Name.Should().Be(first.Name);
        second.Name.Should().NotBe(updatedName);
    }

    [Test]
    public async Task DistanceKm_WhenLatLngProvided_CalculatesDistance()
    {
        await Testing.ResetState(); // Ensure clean state
        
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        restaurant.Should().NotBeNull();

        // Set known coordinates for the restaurant (San Francisco coordinates as example)
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var id = restaurant!.Id.Value;
            var lat = 37.7749;
            var lng = -122.4194;
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants"" 
                SET ""Geo_Latitude"" = {lat}, ""Geo_Longitude"" = {lng}
                WHERE ""Id"" = {id}");
        }

        // Query with nearby coordinates (slightly different location)
        var userLat = 37.7849; // ~1.1 km north
        var userLng = -122.4194;
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value, userLat, userLng));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.DistanceKm.Should().NotBeNull();
        dto.DistanceKm.Should().BeGreaterThan(0);
        dto.DistanceKm.Should().BeLessThan(2); // Should be around 1.1 km
    }

    [Test]
    public async Task DistanceKm_WhenLatLngNotProvided_ReturnsNull()
    {
        await Testing.ResetState(); // Ensure clean state
        
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        restaurant.Should().NotBeNull();

        // Set coordinates for the restaurant
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var id = restaurant!.Id.Value;
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants"" 
                SET ""Geo_Latitude"" = {37.7749}, ""Geo_Longitude"" = {-122.4194}
                WHERE ""Id"" = {id}");
        }

        // Query without lat/lng
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.DistanceKm.Should().BeNull();
    }

    [Test]
    public async Task DistanceKm_WhenRestaurantHasNoCoordinates_ReturnsNull()
    {
        await Testing.ResetState(); // Clear cache
        
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        restaurant.Should().NotBeNull();

        // Ensure restaurant has no coordinates
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var id = restaurant!.Id.Value;
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants"" 
                SET ""Geo_Latitude"" = NULL, ""Geo_Longitude"" = NULL
                WHERE ""Id"" = {id}");
        }

        // Query with user coordinates - when lat/lng provided, cache is bypassed
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value, 37.7749, -122.4194));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.DistanceKm.Should().BeNull(); // Can't calculate distance without restaurant coordinates
    }

    [Test]
    public async Task Caching_BypassesCache_WhenLatLngProvided()
    {
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        restaurant.Should().NotBeNull();

        // Set coordinates for the restaurant
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var restaurantId = restaurant!.Id.Value;
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants"" 
                SET ""Geo_Latitude"" = {37.7749}, ""Geo_Longitude"" = {-122.4194}
                WHERE ""Id"" = {restaurantId}");
        }

        var queryRestaurantId = restaurant.Id.Value;
        
        // First call without lat/lng (should cache)
        var withoutDistance = await SendAndUnwrapAsync(new GetRestaurantPublicInfoQuery(queryRestaurantId));
        withoutDistance.DistanceKm.Should().BeNull();

        // Second call with lat/lng (should bypass cache and calculate distance)
        var withDistance = await SendAndUnwrapAsync(new GetRestaurantPublicInfoQuery(queryRestaurantId, 37.7849, -122.4194));
        withDistance.DistanceKm.Should().NotBeNull();
        withDistance.DistanceKm.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Validation_InvalidLatitude_ShouldThrow()
    {
        var act = async () => await SendAsync(new GetRestaurantPublicInfoQuery(Testing.TestData.DefaultRestaurantId, 91, -122.4194));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Validation_InvalidLongitude_ShouldThrow()
    {
        var act = async () => await SendAsync(new GetRestaurantPublicInfoQuery(Testing.TestData.DefaultRestaurantId, 37.7749, 181));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Validation_OnlyLatProvided_ShouldThrow()
    {
        var act = async () => await SendAsync(new GetRestaurantPublicInfoQuery(Testing.TestData.DefaultRestaurantId, 37.7749, null));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Validation_OnlyLngProvided_ShouldThrow()
    {
        var act = async () => await SendAsync(new GetRestaurantPublicInfoQuery(Testing.TestData.DefaultRestaurantId, null, -122.4194));
        await act.Should().ThrowAsync<ValidationException>();
    }

    #region New Comprehensive Tests for Enhanced Features

    [Test]
    public async Task Success_ReturnsCompleteAddressInformation()
    {
        // Arrange
        var (restaurant, _) = await CreateTestRestaurantWithFullDetailsAsync();

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.Address.Should().NotBeNull();
        dto.Address.Street.Should().Be("123 Culinary Boulevard");
        dto.Address.City.Should().Be("Foodville");
        dto.Address.State.Should().Be("CA");
        dto.Address.ZipCode.Should().Be("90210");
        dto.Address.Country.Should().Be("USA");
    }

    [Test]
    public async Task Success_ReturnsContactInformation()
    {
        // Arrange
        var (restaurant, _) = await CreateTestRestaurantWithFullDetailsAsync();

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.ContactInfo.Should().NotBeNull();
        dto.ContactInfo.PhoneNumber.Should().Be("+1-555-0123");
        dto.ContactInfo.Email.Should().Be("contact@tastybites.com");
    }

    [Test]
    public async Task Success_ReturnsBusinessHours()
    {
        // Arrange
        var (restaurant, _) = await CreateTestRestaurantWithFullDetailsAsync();

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.BusinessHours.Should().Be("09:00-22:00");
    }

    [Test]
    public async Task Success_ReturnsVerificationStatus()
    {
        // Arrange
        var (restaurant, _) = await CreateTestRestaurantWithFullDetailsAsync();

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.IsVerified.Should().BeTrue();
    }

    [Test]
    public async Task Success_ReturnsEstablishedDate()
    {
        // Arrange
        var (restaurant, _) = await CreateTestRestaurantWithFullDetailsAsync();

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.EstablishedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Test]
    public async Task Success_ReturnsDescriptionAndCuisineType()
    {
        // Arrange
        var (restaurant, _) = await CreateTestRestaurantWithFullDetailsAsync();

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.Description.Should().Be("A delightful fusion restaurant offering the best of international cuisine");
        dto.CuisineType.Should().Be("Fusion");
    }

    [Test]
    public async Task CuisineTags_ReturnsEmptyArray_WhenNoMenuItems()
    {
        // Arrange
        var (restaurant, _) = await CreateTestRestaurantWithFullDetailsAsync();
        // No menu items created

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.CuisineTags.Should().NotBeNull();
        dto.CuisineTags.Should().BeEmpty();
    }

    [Test]
    public async Task CuisineTags_ReturnsUniqueCuisineTags_FromMenuItems()
    {
        // Arrange
        var (restaurant, tagIds) = await CreateTestRestaurantWithFullDetailsAsync();
        await CreateMenuItemsWithCuisineTagsAsync(restaurant.Id.Value, tagIds);

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.CuisineTags.Should().NotBeNull();
        dto.CuisineTags.Should().HaveCount(3);
        dto.CuisineTags.Should().Contain("Italian");
        dto.CuisineTags.Should().Contain("Japanese");
        dto.CuisineTags.Should().Contain("Mexican");
        dto.CuisineTags.Should().BeInAscendingOrder(); // Tags should be alphabetically sorted
    }

    [Test]
    public async Task CuisineTags_OnlyIncludesCuisineCategory_ExcludesOtherTagTypes()
    {
        // Arrange
        var (restaurant, tagIds) = await CreateTestRestaurantWithFullDetailsAsync();
        await CreateMenuItemsWithMixedTagsAsync(restaurant.Id.Value, tagIds);

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.CuisineTags.Should().NotBeNull();
        // Should only contain Cuisine category tags, not Dietary, SpiceLevel, etc.
        dto.CuisineTags.Should().Contain("Italian");
        dto.CuisineTags.Should().Contain("Japanese");
        dto.CuisineTags.Should().NotContain("Vegetarian"); // Dietary tag
        dto.CuisineTags.Should().NotContain("Spicy"); // SpiceLevel tag
        dto.CuisineTags.Should().NotContain("GlutenFree"); // Dietary tag
    }

    [Test]
    public async Task CuisineTags_DeduplicatesTags_WhenMultipleItemsHaveSameTag()
    {
        // Arrange
        var (restaurant, tagIds) = await CreateTestRestaurantWithFullDetailsAsync();
        // Create multiple items with the same Italian tag
        await CreateMenuItemWithTagsAsync(restaurant.Id.Value, [tagIds["Italian"]]);
        await CreateMenuItemWithTagsAsync(restaurant.Id.Value, [tagIds["Italian"]]);
        await CreateMenuItemWithTagsAsync(restaurant.Id.Value, [tagIds["Italian"]]);

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.CuisineTags.Should().NotBeNull();
        dto.CuisineTags.Should().HaveCount(1);
        dto.CuisineTags.Should().Contain("Italian");
    }

    [Test]
    public async Task CuisineTags_ExcludesDeletedMenuItems()
    {
        // Arrange
        var (restaurant, tagIds) = await CreateTestRestaurantWithFullDetailsAsync();
        var menuItemId = await CreateMenuItemWithTagsAsync(restaurant.Id.Value, [tagIds["Italian"]]);
        
        // Soft delete the menu item
        var menuItem = await FindAsync<Domain.MenuItemAggregate.MenuItem>(MenuItemId.Create(menuItemId));
        menuItem!.IsDeleted = true;
        await UpdateAsync(menuItem);

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.CuisineTags.Should().NotBeNull();
        dto.CuisineTags.Should().BeEmpty(); // Deleted items should not contribute tags
    }

    [Test]
    public async Task CuisineTags_ExcludesDeletedTags()
    {
        // Arrange
        var (restaurant, tagIds) = await CreateTestRestaurantWithFullDetailsAsync();
        await CreateMenuItemWithTagsAsync(restaurant.Id.Value, [tagIds["Italian"]]);
        
        // Soft delete the tag
        var tag = await FindAsync<Domain.TagEntity.Tag>(Domain.TagEntity.ValueObjects.TagId.Create(tagIds["Italian"]));
        tag!.IsDeleted = true;
        await UpdateAsync(tag);

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.CuisineTags.Should().NotBeNull();
        dto.CuisineTags.Should().BeEmpty(); // Deleted tags should not appear
    }

    [Test]
    public async Task Success_ReturnsBackgroundImageUrl()
    {
        // Arrange
        var (restaurant, _) = await CreateTestRestaurantWithFullDetailsAsync();

        // Act
        var result = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));

        // Assert
        result.ShouldBeSuccessful();
        var dto = result.Value;
        
        dto.BackgroundImageUrl.Should().Be("https://example.com/bg.jpg");
    }

    [Test]
    public async Task CuisineTags_BypassesCache_WhenMenuItemsChange()
    {
        // Arrange
        var (restaurant, tagIds) = await CreateTestRestaurantWithFullDetailsAsync();
        
        // First call - no cuisine tags
        var firstResult = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));
        firstResult.Value.CuisineTags.Should().BeEmpty();

        // Add menu items with cuisine tags
        await CreateMenuItemWithTagsAsync(restaurant.Id.Value, [tagIds["Italian"]]);
        await ResetState(); // Clear cache to ensure fresh read

        // Second call - should now have cuisine tags
        var secondResult = await SendAsync(new GetRestaurantPublicInfoQuery(restaurant.Id.Value));
        
        // Assert
        secondResult.ShouldBeSuccessful();
        secondResult.Value.CuisineTags.Should().Contain("Italian");
    }

    #endregion

    #region Helper Methods

    private async Task<(Restaurant restaurant, Dictionary<string, Guid> tagIds)> CreateTestRestaurantWithFullDetailsAsync()
    {
        // Create various tags
        var italianTag = Domain.TagEntity.Tag.Create("Italian", Domain.TagEntity.Enums.TagCategory.Cuisine, "Italian cuisine").Value;
        var japaneseTag = Domain.TagEntity.Tag.Create("Japanese", Domain.TagEntity.Enums.TagCategory.Cuisine, "Japanese cuisine").Value;
        var mexicanTag = Domain.TagEntity.Tag.Create("Mexican", Domain.TagEntity.Enums.TagCategory.Cuisine, "Mexican cuisine").Value;
        var vegetarianTag = Domain.TagEntity.Tag.Create("Vegetarian", Domain.TagEntity.Enums.TagCategory.Dietary, "Vegetarian food").Value;
        var spicyTag = Domain.TagEntity.Tag.Create("Spicy", Domain.TagEntity.Enums.TagCategory.SpiceLevel, "Spicy food").Value;
        var glutenFreeTag = Domain.TagEntity.Tag.Create("GlutenFree", Domain.TagEntity.Enums.TagCategory.Dietary, "Gluten-free food").Value;

        await AddAsync(italianTag);
        await AddAsync(japaneseTag);
        await AddAsync(mexicanTag);
        await AddAsync(vegetarianTag);
        await AddAsync(spicyTag);
        await AddAsync(glutenFreeTag);

        var tagIds = new Dictionary<string, Guid>
        {
            ["Italian"] = italianTag.Id.Value,
            ["Japanese"] = japaneseTag.Id.Value,
            ["Mexican"] = mexicanTag.Id.Value,
            ["Vegetarian"] = vegetarianTag.Id.Value,
            ["Spicy"] = spicyTag.Id.Value,
            ["GlutenFree"] = glutenFreeTag.Id.Value
        };

        // Create restaurant with full details
        var addressResult = Domain.RestaurantAggregate.ValueObjects.Address.Create(
            "123 Culinary Boulevard",
            "Foodville",
            "CA",
            "90210",
            "USA");

        var contactInfoResult = ContactInfo.Create("+1-555-0123", "contact@tastybites.com");
        var businessHoursResult = BusinessHours.Create("09:00-22:00");
        var geoCoordinatesResult = GeoCoordinates.Create(34.0522, -118.2437);

        var restaurant = Restaurant.Create(
            "Tasty Bites Restaurant",
            logoUrl: "https://example.com/logo.jpg",
            backgroundImageUrl: "https://example.com/bg.jpg",
            "A delightful fusion restaurant offering the best of international cuisine",
            "Fusion",
            addressResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value,
            geoCoordinatesResult.Value).Value;

        restaurant.Verify();
        await AddAsync(restaurant);

        return (restaurant, tagIds);
    }

    private async Task CreateMenuItemsWithCuisineTagsAsync(Guid restaurantId, Dictionary<string, Guid> tagIds)
    {
        // Create menu items with different cuisine tags
        await CreateMenuItemWithTagsAsync(restaurantId, [tagIds["Italian"]]);
        await CreateMenuItemWithTagsAsync(restaurantId, [tagIds["Japanese"]]);
        await CreateMenuItemWithTagsAsync(restaurantId, [tagIds["Mexican"]]);
        await CreateMenuItemWithTagsAsync(restaurantId, [tagIds["Italian"], tagIds["Japanese"]]); // Item with multiple tags
    }

    private async Task CreateMenuItemsWithMixedTagsAsync(Guid restaurantId, Dictionary<string, Guid> tagIds)
    {
        // Create menu items with a mix of cuisine and non-cuisine tags
        await CreateMenuItemWithTagsAsync(restaurantId, [tagIds["Italian"], tagIds["Vegetarian"]]);
        await CreateMenuItemWithTagsAsync(restaurantId, [tagIds["Japanese"], tagIds["Spicy"], tagIds["GlutenFree"]]);
    }

    private async Task<Guid> CreateMenuItemWithTagsAsync(Guid restaurantId, List<Guid> tagIds)
    {
        // Create or reuse menu and category
        var menu = Domain.MenuEntity.Menu.Create(
            RestaurantId.Create(restaurantId),
            "Main Menu",
            "Our primary menu").Value;
        await AddAsync(menu);

        var category = Domain.MenuEntity.MenuCategory.Create(
            menu.Id,
            "Main Dishes",
            1).Value;
        await AddAsync(category);

        var price = new Money(19.99m, "USD");
        var menuItem = Domain.MenuItemAggregate.MenuItem.Create(
            RestaurantId.Create(restaurantId),
            category.Id,
            $"Dish {Guid.NewGuid().ToString()[..8]}",
            "A delicious dish",
            price,
            imageUrl: null,
            isAvailable: true,
            dietaryTagIds: tagIds.Select(Domain.TagEntity.ValueObjects.TagId.Create).ToList()).Value;

        await AddAsync(menuItem);
        return menuItem.Id.Value;
    }

    #endregion
}
