using YummyZoom.Application.CustomizationGroups.Queries.GetCustomizationGroupDetails;
using YummyZoom.Application.CustomizationGroups.Queries.GetCustomizationGroups;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.CustomizationGroups.Queries;

using static Testing;

public class CustomizationGroupsQueriesTests : BaseTestFixture
{
    [Test]
    public async Task GetCustomizationGroups_ShouldReturnSummaryList()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        // Create a few groups directly in the domain
        // Create a few groups directly in the domain - use unique names to avoid collision with default/seeded data
        var group1 = CustomizationGroup.Create(restaurantId, "New Size", 1, 1).Value;
        group1.AddChoice("Medium", new Money(0, "USD"), true, 1);
        group1.AddChoice("Large", new Money(2, "USD"), false, 2);

        var group2 = CustomizationGroup.Create(restaurantId, "New Toppings", 0, 5).Value;
        group2.AddChoice("Cheese", new Money(1, "USD"), false, 1);

        var group3 = CustomizationGroup.Create(restaurantId, "DeletedGroup", 1, 1).Value;
        group3.MarkAsDeleted(DateTimeOffset.UtcNow);

        // Add to DB
        await AddAsync(group1);
        await AddAsync(group2);
        await AddAsync(group3);

        var query = new GetCustomizationGroupsQuery(restaurantId.Value);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCountGreaterThanOrEqualTo(2); // Can contain default seeded groups too

        var g1Dto = result.Value.FirstOrDefault(g => g.Id == group1.Id.Value);
        g1Dto.Should().NotBeNull();
        g1Dto!.Name.Should().Be("New Size");
        g1Dto.ChoiceCount.Should().Be(2);

        var g2Dto = result.Value.FirstOrDefault(g => g.Id == group2.Id.Value);
        g2Dto.Should().NotBeNull();
        g2Dto!.Name.Should().Be("New Toppings");
        g2Dto.ChoiceCount.Should().Be(1);
    }

    [Test]
    public async Task GetCustomizationGroupDetails_ShouldReturnFullDetails()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        var group = CustomizationGroup.Create(restaurantId, "Spiciness", 1, 1).Value;
        group.AddChoice("Mild", new Money(0, "USD"), true, 1);
        group.AddChoice("Hot", new Money(0, "USD"), false, 2);

        await AddAsync(group);

        var query = new GetCustomizationGroupDetailsQuery(restaurantId.Value, group.Id.Value);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var details = result.Value;
        details.Name.Should().Be("Spiciness");
        details.MinSelections.Should().Be(1);
        details.MaxSelections.Should().Be(1);
        details.Choices.Should().HaveCount(2);

        details.Choices.Should().Contain(c => c.Name == "Mild" && c.IsDefault && c.DisplayOrder == 1);
        details.Choices.Should().Contain(c => c.Name == "Hot" && !c.IsDefault && c.DisplayOrder == 2);
    }

    [Test]
    public async Task GetCustomizationGroupDetails_ShouldFail_WhenGroupDeleted()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        var group = CustomizationGroup.Create(restaurantId, "Old Group", 1, 1).Value;
        group.MarkAsDeleted(DateTimeOffset.UtcNow);

        await AddAsync(group);

        var query = new GetCustomizationGroupDetailsQuery(restaurantId.Value, group.Id.Value);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(CustomizationGroupErrors.NotFound.Code);
    }
}
