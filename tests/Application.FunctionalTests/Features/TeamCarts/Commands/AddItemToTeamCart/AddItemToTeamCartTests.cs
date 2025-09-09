using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Application.MenuItems.Commands.AssignCustomizationGroupToMenuItem;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.AddItemToTeamCart;

public class AddItemToTeamCartTests : BaseTestFixture
{
    [Test]
    public async Task AddItem_HappyPath_AsGuest_WithCustomization_ShouldPersist()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .WithGuest("Bob Guest")
            .BuildAsync();

        // Act: Add item with customization as guest
        await scenario.ActAsGuest("Bob Guest");
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var addCmd = new AddItemToTeamCartCommand(
            TeamCartId: scenario.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 2,
            SelectedCustomizations: new[]
            {
                new AddItemToTeamCartCustomizationSelection(
                    TestDataFactory.CustomizationGroup_BurgerAddOnsId,
                    TestDataFactory.CustomizationChoice_ExtraCheeseId)
            }
        );

        var addResult = await SendAsync(addCmd);
        addResult.IsSuccess.Should().BeTrue();

        // Assert: Verify persisted
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cart = await db.TeamCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == TeamCartId.Create(scenario.TeamCartId));

        cart.Should().NotBeNull();
        cart!.Items.Should().Contain(i => i.Quantity == 2 && i.AddedByUserId.Value == scenario.GetGuestUserId("Bob Guest"));
    }

    [Test]
    public async Task AddItem_InvalidQuantity_ShouldFailValidation()
    {
        // Arrange: Create team cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Act & Assert: Add item with invalid quantity should fail validation
        await scenario.ActAsHost(); // Ensure we're acting as a team cart member
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var addCmd = new AddItemToTeamCartCommand(
            TeamCartId: scenario.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 0
        );

        await FluentActions.Invoking(() => SendAsync(addCmd))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task AddItem_MenuItemFromDifferentRestaurant_ShouldFail()
    {
        // Arrange: Create team cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Create second restaurant + menu item
        var (otherRestaurantId, otherItemId) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();

        // Act: Try to add item from different restaurant as host
        await scenario.ActAsHost(); // Ensure we're acting as a team cart member
        var addCmd = new AddItemToTeamCartCommand(
            TeamCartId: scenario.TeamCartId,
            MenuItemId: otherItemId,
            Quantity: 1
        );

        var result = await SendAsync(addCmd);

        // Assert: Should fail
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddItem_RequiredCustomizationGroupMissing_ShouldFail()
    {
        // Arrange: Assign required group (min 1) to Classic Burger as restaurant staff
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        await RunAsRestaurantStaffAsync("staff@restaurant.com", restaurantId);

        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var requiredGroupId = TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value;
        var assignCmd = new AssignCustomizationGroupToMenuItemCommand(
            RestaurantId: restaurantId,
            MenuItemId: burgerId,
            CustomizationGroupId: requiredGroupId,
            DisplayTitle: "Bun Type",
            DisplayOrder: null);
        (await SendAsync(assignCmd)).IsSuccess.Should().BeTrue();

        // Create team cart scenario as customer
        var scenario = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Act: Try to add without required selection as host
        await scenario.ActAsHost(); // Ensure we're acting as a team cart member
        var add = await SendAsync(new AddItemToTeamCartCommand(
            TeamCartId: scenario.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 1));

        // Assert: Should fail
        add.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddItem_MaxSelectionsExceeded_ShouldFail()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", restaurantId);

        // Create a group with max 1 selection and two choices
        var groupResult = CustomizationGroup.Create(RestaurantId.Create(restaurantId), "Sauces (max1)", 0, 1);
        groupResult.IsSuccess.Should().BeTrue();
        var group = groupResult.Value;
        group.AddChoice("Ketchup", new Money(0m, DefaultTestData.Currency.Default), isDefault: false, displayOrder: 1);
        group.AddChoice("Mayo", new Money(0m, DefaultTestData.Currency.Default), isDefault: false, displayOrder: 2);
        group.ClearDomainEvents();
        await AddAsync(group);

        // Assign to burger
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var assign = await SendAsync(new AssignCustomizationGroupToMenuItemCommand(
            RestaurantId: restaurantId,
            MenuItemId: burgerId,
            CustomizationGroupId: group.Id.Value,
            DisplayTitle: "Sauces",
            DisplayOrder: null));
        assign.IsSuccess.Should().BeTrue();

        // Create team cart scenario as customer
        var scenario = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .BuildAsync();

        var ketchupId = group.Choices.First(c => c.Name == "Ketchup").Id.Value;
        var mayoId = group.Choices.First(c => c.Name == "Mayo").Id.Value;

        // Act: Customer adds item selecting both choices (exceeds max) as host
        await scenario.ActAsHost(); // Ensure we're acting as a team cart member
        var add = await SendAsync(new AddItemToTeamCartCommand(
            TeamCartId: scenario.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 1,
            SelectedCustomizations: new[]
            {
                new AddItemToTeamCartCustomizationSelection(group.Id.Value, ketchupId),
                new AddItemToTeamCartCustomizationSelection(group.Id.Value, mayoId)
            }));

        // Assert: Should fail
        add.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddItem_CustomizationGroupNotApplied_ShouldFail()
    {
        // Arrange: Create team cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Act: Try to add item with unassigned customization group as host
        await scenario.ActAsHost(); // Ensure we're acting as a team cart member
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var unassignedGroupId = TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value;
        var someChoiceId = TestDataFactory.CustomizationChoice_BriocheBunId!.Value;

        var add = await SendAsync(new AddItemToTeamCartCommand(
            TeamCartId: scenario.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 1,
            SelectedCustomizations: new[]
            {
                new AddItemToTeamCartCustomizationSelection(unassignedGroupId, someChoiceId)
            }));

        // Assert: Should fail
        add.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddItem_DuplicateChoicesInSameGroup_ShouldFail()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", restaurantId);

        // Create a group allowing up to 2 selections
        var groupResult = CustomizationGroup.Create(RestaurantId.Create(restaurantId), "Toppings (dup) max2", 0, 2);
        groupResult.IsSuccess.Should().BeTrue();
        var group = groupResult.Value;
        group.AddChoice("Pickles", new Money(0m, DefaultTestData.Currency.Default), isDefault: false, displayOrder: 1);
        group.ClearDomainEvents();
        await AddAsync(group);

        // Assign to burger
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        (await SendAsync(new AssignCustomizationGroupToMenuItemCommand(
            RestaurantId: restaurantId,
            MenuItemId: burgerId,
            CustomizationGroupId: group.Id.Value,
            DisplayTitle: "Toppings",
            DisplayOrder: null))).IsSuccess.Should().BeTrue();

        // Create team cart scenario as customer
        var scenario = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .BuildAsync();

        var picklesId = group.Choices.First(c => c.Name == "Pickles").Id.Value;

        // Act: Customer attempts to add the same choice twice as host
        await scenario.ActAsHost(); // Ensure we're acting as a team cart member
        var add = await SendAsync(new AddItemToTeamCartCommand(
            TeamCartId: scenario.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 1,
            SelectedCustomizations: new[]
            {
                new AddItemToTeamCartCustomizationSelection(group.Id.Value, picklesId),
                new AddItemToTeamCartCustomizationSelection(group.Id.Value, picklesId)
            }));

        // Assert: Should fail
        add.IsFailure.Should().BeTrue();
    }
}
