using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

[TestFixture]
public class InitiateOrderCustomizationTests : InitiateOrderTestBase
{
    private Guid ClassicBurgerId => Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

    private OrderItemDto BuildBurgerWithAddOns(params Guid[] choiceIds)
    {
        var customization = new OrderItemCustomizationRequestDto(
            TestDataFactory.CustomizationGroup_BurgerAddOnsId,
            choiceIds.ToList());
        return new OrderItemDto(ClassicBurgerId, 1, new List<OrderItemCustomizationRequestDto> { customization });
    }

    [Test]
    public async Task InitiateOrder_WithValidCustomizations_ShouldIncludeSnapshotsAndAdjustPricing()
    {
        // Arrange
        var item = BuildBurgerWithAddOns(TestDataFactory.CustomizationChoice_ExtraCheeseId, TestDataFactory.CustomizationChoice_BaconId);
        var command = InitiateOrderTestHelper.BuildValidCommand(menuItemIds: new List<Guid> { ClassicBurgerId }) with
        {
            Items = new List<OrderItemDto> { item }
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        var order = await FindAsync<Order>(result.Value.OrderId);
        order.Should().NotBeNull();
        order!.OrderItems.Count.Should().Be(1);
        var orderItem = order.OrderItems[0];
        orderItem.SelectedCustomizations.Count.Should().Be(2);
        var customizationNames = orderItem.SelectedCustomizations.Select(c => c.Snapshot_ChoiceName).ToList();
        customizationNames.Should().Contain(new[] { "Extra Cheese", "Bacon" });

        // Financial: base + 1.50 + 2.00
        var basePrice = orderItem.Snapshot_BasePriceAtOrder.Amount;
        orderItem.LineItemTotal.Amount.Should().Be(basePrice + 1.50m + 2.00m);
        order.Subtotal.Amount.Should().Be(orderItem.LineItemTotal.Amount);
    }

    [Test]
    public async Task InitiateOrder_WithoutCustomizations_ForCustomizableItem_ShouldSucceed()
    {
        // Arrange
        var item = new OrderItemDto(ClassicBurgerId, 1); // no customizations
        var command = InitiateOrderTestHelper.BuildValidCommand(menuItemIds: new List<Guid> { ClassicBurgerId }) with
        {
            Items = new List<OrderItemDto> { item }
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        var order = await FindAsync<Order>(result.Value.OrderId);
        var orderItem = order!.OrderItems[0];
        orderItem.SelectedCustomizations.Should().BeEmpty();
    }

    [Test]
    public async Task InitiateOrder_WithCustomizationGroupNotAssigned_ShouldFail()
    {
        // Arrange: use required bun type group which was not assigned
        Assert.That(TestDataFactory.CustomizationGroup_RequiredBunTypeId, Is.Not.Null);
        var unassignedGroupId = TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value;
        var item = new OrderItemDto(ClassicBurgerId, 1, new List<OrderItemCustomizationRequestDto>{
            new(unassignedGroupId, new List<Guid>{ TestDataFactory.CustomizationChoice_BriocheBunId!.Value })
        });
        var command = InitiateOrderTestHelper.BuildValidCommand(menuItemIds: new List<Guid> { ClassicBurgerId }) with { Items = new List<OrderItemDto> { item } };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.CustomizationGroupNotAssignedToMenuItem(unassignedGroupId));
    }

    [Test]
    public async Task InitiateOrder_WithInvalidChoiceId_ShouldFail()
    {
        // Arrange: invalid extra random GUID combined with valid group
        var invalidChoiceId = Guid.NewGuid();
        // Sanity: ensure burger has assigned add-ons group
        var burgerEntity = await FindAsync<MenuItem>(MenuItemId.Create(ClassicBurgerId));
        burgerEntity!.AppliedCustomizations.Should().NotBeEmpty("seed should assign Burger Add-ons group");
        
        var item = new OrderItemDto(ClassicBurgerId, 1, new List<OrderItemCustomizationRequestDto>{
            new(TestDataFactory.CustomizationGroup_BurgerAddOnsId, new List<Guid>{ invalidChoiceId })
        });
        var command = InitiateOrderTestHelper.BuildValidCommand(menuItemIds: new List<Guid> { ClassicBurgerId }) with { Items = new List<OrderItemDto> { item } };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be(InitiateOrderErrors.CustomizationChoiceNotFound(invalidChoiceId).Code);
    }

    [Test]
    public async Task InitiateOrder_WithTooFewSelections_ShouldFail()
    {
        // Arrange: create a temporary required group that requires 2+ selections and assign it for this test
        var restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
        
        // Create a new customization group that requires at least 2 selections
        var requiredGroupResult = CustomizationGroup.Create(
            restaurantId, 
            "Required Toppings", 
            minSelections: 2, 
            maxSelections: 3);
        requiredGroupResult.IsSuccess.Should().BeTrue();
        var requiredGroup = requiredGroupResult.Value;
        
        // Add some choices to this group
        requiredGroup.AddChoice("Choice 1", new Money(1.00m, Currencies.USD), false, 1);
        requiredGroup.AddChoice("Choice 2", new Money(1.50m, Currencies.USD), false, 2);
        requiredGroup.AddChoice("Choice 3", new Money(2.00m, Currencies.USD), false, 3);
        await AddAsync(requiredGroup);
        
        // Assign this group to the burger so selection count validation triggers
        var burger = await FindAsync<MenuItem>(MenuItemId.Create(ClassicBurgerId));
        var applied = AppliedCustomization.Create(requiredGroup.Id, "Required Toppings", 1);
        var assignResult = burger!.AssignCustomizationGroup(applied);
        if (assignResult.IsSuccess)
        {
            await UpdateAsync(burger);
        }
        
        // Provide only 1 choice when min required is 2 - this should fail domain validation
        var singleChoiceId = requiredGroup.Choices.First().Id.Value;
        var item = new OrderItemDto(ClassicBurgerId, 1, new List<OrderItemCustomizationRequestDto> { 
            new(requiredGroup.Id.Value, new List<Guid> { singleChoiceId }) 
        });
        var command = InitiateOrderTestHelper.BuildValidCommand(menuItemIds: new List<Guid> { ClassicBurgerId }) with { Items = new List<OrderItemDto> { item } };
        
        // Act
        var result = await SendAsync(command);
        
        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.CustomizationGroupSelectionCountInvalid(requiredGroup.Id.Value, 2, 3));
    }

    [Test]
    public async Task InitiateOrder_GroupFromDifferentRestaurant_ShouldFail_AsNotFound()
    {
        // Arrange create second restaurant and a customization group assigned there
        var (otherRestaurantId, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        // Create a customization group for other restaurant
        var restaurantIdVO = RestaurantId.Create(otherRestaurantId);
        var otherGroupResult = CustomizationGroup.Create(
            restaurantIdVO, "Other Group", 0, 1);
        otherGroupResult.IsSuccess.Should().BeTrue();
        var otherGroup = otherGroupResult.Value;
        otherGroup.AddChoice("Other Choice", new Money(0.25m, DefaultTestData.Currency.Default), false, 1);
        await AddAsync(otherGroup);

        var item = new OrderItemDto(ClassicBurgerId, 1, new List<OrderItemCustomizationRequestDto>{
            new(otherGroup.Id.Value, new List<Guid>{ otherGroup.Choices.First().Id.Value })
        });
        var command = InitiateOrderTestHelper.BuildValidCommand(menuItemIds: new List<Guid> { ClassicBurgerId }) with { Items = new List<OrderItemDto> { item } };

        var result = await SendAsync(command);

        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.CustomizationGroupNotAssignedToMenuItem(otherGroup.Id.Value));
    }
}
