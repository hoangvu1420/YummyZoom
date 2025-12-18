using YummyZoom.Application.CustomizationGroups.Commands.AddCustomizationChoice;
using YummyZoom.Application.CustomizationGroups.Commands.CreateCustomizationGroup;
using YummyZoom.Application.CustomizationGroups.Commands.RemoveCustomizationChoice;
using YummyZoom.Application.CustomizationGroups.Commands.ReorderCustomizationChoices;
using YummyZoom.Application.CustomizationGroups.Commands.UpdateCustomizationChoice;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.CustomizationGroups.Commands;

public class CustomizationChoiceCommandsTests : BaseTestFixture
{
    private Guid _groupId;

    [SetUp]
    public async Task SetUp()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var createGroupCommand = new CreateCustomizationGroupCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupName: "Options",
            MinSelections: 0,
            MaxSelections: 5);

        var result = await SendAsync(createGroupCommand);
        _groupId = result.Value;
    }

    [Test]
    public async Task AddCustomizationChoice_WithValidData_ShouldSucceed()
    {
        // Arrange
        var command = new AddCustomizationChoiceCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: _groupId,
            Name: "Extra Cheese",
            PriceAmount: 1.50m,
            PriceCurrency: "USD",
            IsDefault: false,
            DisplayOrder: 1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Should().NotBe(Guid.Empty);

        var group = await FindAsync<CustomizationGroup>(CustomizationGroupId.Create(_groupId));
        group.Should().NotBeNull();

        var choice = group!.Choices.FirstOrDefault(c => c.Id.Value == result.Value);
        choice.Should().NotBeNull();
        choice!.Name.Should().Be("Extra Cheese");
        choice.PriceAdjustment.Amount.Should().Be(1.50m);
    }

    [Test]
    public async Task UpdateCustomizationChoice_WithValidData_ShouldSucceed()
    {
        // Arrange: Add a choice first
        var addCommand = new AddCustomizationChoiceCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: _groupId,
            Name: "Old Name",
            PriceAmount: 1.00m,
            PriceCurrency: "USD",
            IsDefault: false,
            DisplayOrder: 1);
        var addResult = await SendAsync(addCommand);
        var choiceId = addResult.Value;

        var updateCommand = new UpdateCustomizationChoiceCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: _groupId,
            ChoiceId: choiceId,
            Name: "New Name",
            PriceAmount: 2.00m,
            PriceCurrency: "USD",
            IsDefault: true,
            DisplayOrder: 2);

        // Act
        var result = await SendAsync(updateCommand);

        // Assert
        result.ShouldBeSuccessful();

        var group = await FindAsync<CustomizationGroup>(CustomizationGroupId.Create(_groupId));
        var choice = group!.Choices.First(c => c.Id.Value == choiceId);

        choice.Name.Should().Be("New Name");
        choice.PriceAdjustment.Amount.Should().Be(2.00m);
        choice.IsDefault.Should().BeTrue();
        choice.DisplayOrder.Should().Be(2);
    }

    [Test]
    public async Task RemoveCustomizationChoice_ShouldSucceed()
    {
        // Arrange: Add a choice first
        var addCommand = new AddCustomizationChoiceCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: _groupId,
            Name: "To Remove",
            PriceAmount: 0m,
            PriceCurrency: "USD",
            IsDefault: false,
            DisplayOrder: 1);
        var addResult = await SendAsync(addCommand);
        var choiceId = addResult.Value;

        var removeCommand = new RemoveCustomizationChoiceCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: _groupId,
            ChoiceId: choiceId);

        // Act
        var result = await SendAsync(removeCommand);

        // Assert
        result.ShouldBeSuccessful();

        var group = await FindAsync<CustomizationGroup>(CustomizationGroupId.Create(_groupId));
        group!.Choices.Should().NotContain(c => c.Id.Value == choiceId);
    }

    [Test]
    public async Task ReorderCustomizationChoices_ShouldUpdateDisplayOrders()
    {
        // Arrange: Add two choices
        var addCommand1 = new AddCustomizationChoiceCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: _groupId,
            Name: "Choice 1",
            PriceAmount: 0m,
            PriceCurrency: "USD",
            IsDefault: false,
            DisplayOrder: 1);
        var result1 = await SendAsync(addCommand1);
        var choiceId1 = result1.Value;

        var addCommand2 = new AddCustomizationChoiceCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: _groupId,
            Name: "Choice 2",
            PriceAmount: 0m,
            PriceCurrency: "USD",
            IsDefault: false,
            DisplayOrder: 2);
        var result2 = await SendAsync(addCommand2);
        var choiceId2 = result2.Value;

        var reorderCommand = new ReorderCustomizationChoicesCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            GroupId: _groupId,
            ChoiceOrders: new List<ChoiceOrderDto>
            {
                new(choiceId1, 2),
                new(choiceId2, 1)
            });

        // Act
        var result = await SendAsync(reorderCommand);

        // Assert
        result.ShouldBeSuccessful();

        var group = await FindAsync<CustomizationGroup>(CustomizationGroupId.Create(_groupId));
        var c1 = group!.Choices.First(c => c.Id.Value == choiceId1);
        var c2 = group!.Choices.First(c => c.Id.Value == choiceId2);

        c1.DisplayOrder.Should().Be(2);
        c2.DisplayOrder.Should().Be(1);
    }
}
