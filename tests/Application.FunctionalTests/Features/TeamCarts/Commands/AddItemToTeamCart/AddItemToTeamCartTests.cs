using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Application.MenuItems.Commands.AssignCustomizationGroupToMenuItem;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Common.ValueObjects;

using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.AddItemToTeamCart;

public class AddItemToTeamCartTests : BaseTestFixture
{
    [Test]
    public async Task AddItem_HappyPath_AsGuest_WithCustomization_ShouldPersist()
    {
        // Host creates the cart
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var createCmd = new CreateTeamCartCommand(restaurantId, "Alice Host");
        var createResult = await SendAsync(createCmd);
        createResult.IsSuccess.Should().BeTrue();

        var teamCartId = createResult.Value.TeamCartId;
        var shareToken = createResult.Value.ShareToken;

        // Guest joins
        var guestUserId = await CreateUserAsync("guest-additem@example.com", "Password123!");
        SetUserId(guestUserId);
        var joinCmd = new JoinTeamCartCommand(teamCartId, shareToken, "Bob Guest");
        var joinResult = await SendAsync(joinCmd);
        joinResult.IsSuccess.Should().BeTrue();

        // Add item with one customization
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var addCmd = new AddItemToTeamCartCommand(
            TeamCartId: teamCartId,
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

        // Verify persisted
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cart = await db.TeamCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == TeamCartId.Create(teamCartId));

        cart.Should().NotBeNull();
        cart!.Items.Should().Contain(i => i.Quantity == 2 && i.AddedByUserId.Value == guestUserId);
    }

    [Test]
    public async Task AddItem_InvalidQuantity_ShouldFailValidation()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var createCmd = new CreateTeamCartCommand(restaurantId, "Host");
        var createResult = await SendAsync(createCmd);
        createResult.IsSuccess.Should().BeTrue();

        var teamCartId = createResult.Value.TeamCartId;
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);

        var addCmd = new AddItemToTeamCartCommand(
            TeamCartId: teamCartId,
            MenuItemId: burgerId,
            Quantity: 0
        );

        await FluentActions.Invoking(() => SendAsync(addCmd))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task AddItem_MenuItemFromDifferentRestaurant_ShouldFail()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var createCmd = new CreateTeamCartCommand(restaurantId, "Host");
        var createResult = await SendAsync(createCmd);
        createResult.IsSuccess.Should().BeTrue();

        var teamCartId = createResult.Value.TeamCartId;

        // Create second restaurant + menu item
        var (otherRestaurantId, otherItemId) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();

        var addCmd = new AddItemToTeamCartCommand(
            TeamCartId: teamCartId,
            MenuItemId: otherItemId,
            Quantity: 1
        );

        var result = await SendAsync(addCmd);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddItem_RequiredCustomizationGroupMissing_ShouldFail()
    {
        // Assign required group (min 1) to Classic Burger as restaurant staff
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

        // Switch to a customer, create cart and try to add without required selection
        await RunAsDefaultUserAsync();
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();

        var add = await SendAsync(new AddItemToTeamCartCommand(
            TeamCartId: create.Value.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 1));
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

        // Customer adds item selecting both choices (exceeds max)
        await RunAsDefaultUserAsync();
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();

        var ketchupId = group.Choices.First(c => c.Name == "Ketchup").Id.Value;
        var mayoId = group.Choices.First(c => c.Name == "Mayo").Id.Value;

        var add = await SendAsync(new AddItemToTeamCartCommand(
            TeamCartId: create.Value.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 1,
            SelectedCustomizations: new[]
            {
                new AddItemToTeamCartCustomizationSelection(group.Id.Value, ketchupId),
                new AddItemToTeamCartCustomizationSelection(group.Id.Value, mayoId)
            }));
        add.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task AddItem_CustomizationGroupNotApplied_ShouldFail()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var unassignedGroupId = TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value;
        var someChoiceId = TestDataFactory.CustomizationChoice_BriocheBunId!.Value;

        var add = await SendAsync(new AddItemToTeamCartCommand(
            TeamCartId: create.Value.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 1,
            SelectedCustomizations: new[]
            {
                new AddItemToTeamCartCustomizationSelection(unassignedGroupId, someChoiceId)
            }));

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

        // Customer attempts to add the same choice twice
        await RunAsDefaultUserAsync();
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();

        var picklesId = group.Choices.First(c => c.Name == "Pickles").Id.Value;

        var add = await SendAsync(new AddItemToTeamCartCommand(
            TeamCartId: create.Value.TeamCartId,
            MenuItemId: burgerId,
            Quantity: 1,
            SelectedCustomizations: new[]
            {
                new AddItemToTeamCartCustomizationSelection(group.Id.Value, picklesId),
                new AddItemToTeamCartCustomizationSelection(group.Id.Value, picklesId)
            }));

        add.IsFailure.Should().BeTrue();
    }
}
