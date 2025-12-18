using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.CustomizationGroups.Commands.CreateCustomizationGroup;
using YummyZoom.Application.CustomizationGroups.Commands.DeleteCustomizationGroup;
using YummyZoom.Application.CustomizationGroups.Commands.UpdateCustomizationGroup;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.CustomizationGroups.Commands;

public class CustomizationGroupCommandsTests : BaseTestFixture
{
    [Test]
    public async Task CreateCustomizationGroup_WithValidData_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateCustomizationGroupCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupName: "Spice Level",
            MinSelections: 1,
            MaxSelections: 1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Should().NotBe(Guid.Empty);

        var group = await FindAsync<CustomizationGroup>(CustomizationGroupId.Create(result.Value));
        group.Should().NotBeNull();
        group!.GroupName.Should().Be("Spice Level");
        group.MinSelections.Should().Be(1);
        group.MaxSelections.Should().Be(1);
        group.RestaurantId.Value.Should().Be(Testing.TestData.DefaultRestaurantId);
    }

    [Test]
    public async Task UpdateCustomizationGroup_WithValidData_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Create a group first
        var createCommand = new CreateCustomizationGroupCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupName: "Original Name",
            MinSelections: 1,
            MaxSelections: 1);
        var createResult = await SendAsync(createCommand);
        var groupId = createResult.Value;

        var updateCommand = new UpdateCustomizationGroupCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: groupId,
            GroupName: "Updated Name",
            MinSelections: 0,
            MaxSelections: 2);

        // Act
        var result = await SendAsync(updateCommand);

        // Assert
        result.ShouldBeSuccessful();

        var group = await FindAsync<CustomizationGroup>(CustomizationGroupId.Create(groupId));
        group.Should().NotBeNull();
        group!.GroupName.Should().Be("Updated Name");
        group.MinSelections.Should().Be(0);
        group.MaxSelections.Should().Be(2);
    }

    [Test]
    public async Task DeleteCustomizationGroup_ShouldSoftDelete()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Create a group first
        var createCommand = new CreateCustomizationGroupCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupName: "To Be Deleted",
            MinSelections: 1,
            MaxSelections: 1);
        var createResult = await SendAsync(createCommand);
        var groupId = createResult.Value;

        var deleteCommand = new DeleteCustomizationGroupCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: groupId);

        // Act
        var result = await SendAsync(deleteCommand);

        // Assert
        result.ShouldBeSuccessful();

        // Use a custom scope and context to ignore query filters (specifically soft-delete filter)
        using var scope = Testing.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var group = await context.CustomizationGroups
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == CustomizationGroupId.Create(groupId));

        group.Should().NotBeNull();
        group!.IsDeleted.Should().BeTrue();
    }
}
