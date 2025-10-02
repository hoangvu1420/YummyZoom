using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
using YummyZoom.Domain.CustomizationGroupAggregate.Events;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CustomizationGroupAggregate;

/// <summary>
/// Tests for CustomizationGroup choice management functionality including adding, removing, and updating choices.
/// </summary>
[TestFixture]
public class CustomizationGroupChoiceManagementTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultGroupName = "Size Options";
    private const int DefaultMinSelections = 1;
    private const int DefaultMaxSelections = 1;
    private static readonly Money DefaultPrice = new Money(1.00m, Currencies.Default);
    private static readonly Money PriceAdjustment = new Money(2.50m, Currencies.Default);

    #region AddChoice() Method Tests

    [Test]
    public void AddChoice_WithValidChoice_ShouldSucceedAndAddChoice()
    {
        // Arrange
        var group = CreateValidGroup();

        // Act
        var result = group.AddChoice("Large", PriceAdjustment, false, 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().ContainSingle();
        var addedChoice = group.Choices.Single();
        addedChoice.Name.Should().Be("Large");
        addedChoice.PriceAdjustment.Should().Be(PriceAdjustment);
        addedChoice.IsDefault.Should().BeFalse();
        addedChoice.DisplayOrder.Should().Be(1);

        group.DomainEvents.Should().Contain(e => e.GetType() == typeof(CustomizationChoiceAdded));

        var choiceAddedEvent = group.DomainEvents.OfType<CustomizationChoiceAdded>().Single();
        choiceAddedEvent.CustomizationGroupId.Should().Be((CustomizationGroupId)group.Id);
        choiceAddedEvent.ChoiceId.Should().Be(addedChoice.Id);
        choiceAddedEvent.Name.Should().Be("Large");
    }

    [Test]
    public void AddChoice_WithDuplicateChoiceName_ShouldFailWithChoiceNameNotUniqueError()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Large", DefaultPrice, false, 1);

        // Act
        var result = group.AddChoice("Large", PriceAdjustment, false, 2); // Same name

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CustomizationGroupErrors.ChoiceNameNotUnique);
        group.Choices.Should().ContainSingle(); // Only the first choice should remain
        group.Choices.Single().Name.Should().Be("Large");
        group.Choices.Single().PriceAdjustment.Should().Be(DefaultPrice);
    }

    [Test]
    public void AddChoice_MultipleValidChoices_ShouldSucceedAndAddAllChoices()
    {
        // Arrange
        var group = CreateValidGroup();

        // Act
        var result1 = group.AddChoice("Small", DefaultPrice, false, 1);
        var result2 = group.AddChoice("Medium", PriceAdjustment, false, 2);
        var result3 = group.AddChoice("Large", new Money(5.00m, Currencies.Default), false, 3);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        group.Choices.Should().HaveCount(3);
        group.Choices.Should().Contain(c => c.Name == "Small");
        group.Choices.Should().Contain(c => c.Name == "Medium");
        group.Choices.Should().Contain(c => c.Name == "Large");
    }

    [Test]
    public void AddChoice_WithDuplicateDisplayOrder_ShouldSucceedAndAllowTies()
    {
        // Arrange
        var group = CreateValidGroup();

        // Act
        var result1 = group.AddChoice("Small", DefaultPrice, false, 1);
        var result2 = group.AddChoice("Large", PriceAdjustment, false, 1); // Same display order

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue(); // Ties are allowed
        group.Choices.Should().HaveCount(2);
        group.Choices.Should().Contain(c => c.Name == "Small");
        group.Choices.Should().Contain(c => c.Name == "Large");

        // Verify ordering: both have same display order, so sorted by name
        var orderedChoices = group.Choices.ToList();
        orderedChoices[0].Name.Should().Be("Large"); // "Large" comes before "Small" alphabetically
        orderedChoices[1].Name.Should().Be("Small");
    }

    #endregion

    #region RemoveChoice() Method Tests

    [Test]
    public void RemoveChoice_WithValidChoiceId_ShouldSucceedAndRemoveChoice()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Large", PriceAdjustment, false, 1);
        var addedChoice = group.Choices.Single();

        // Act
        var result = group.RemoveChoice(addedChoice.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().BeEmpty();
        group.DomainEvents.Should().Contain(e => e.GetType() == typeof(CustomizationChoiceRemoved));

        var choiceRemovedEvent = group.DomainEvents.OfType<CustomizationChoiceRemoved>().Single();
        choiceRemovedEvent.CustomizationGroupId.Should().Be((CustomizationGroupId)group.Id);
        choiceRemovedEvent.ChoiceId.Should().Be(addedChoice.Id);
        choiceRemovedEvent.Name.Should().Be(addedChoice.Name);
    }

    [Test]
    public void RemoveChoice_WithInvalidChoiceId_ShouldFailWithInvalidChoiceIdError()
    {
        // Arrange
        var group = CreateValidGroup();
        var nonExistentChoiceId = ChoiceId.CreateUnique();

        // Act
        var result = group.RemoveChoice(nonExistentChoiceId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidChoiceId);
    }

    [Test]
    public void RemoveChoice_WithMultipleChoices_ShouldRemoveOnlySpecifiedChoice()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Large", PriceAdjustment, false, 2);
        var smallChoice = group.Choices.First(c => c.Name == "Small");
        var largeChoice = group.Choices.First(c => c.Name == "Large");

        // Act
        var result = group.RemoveChoice(smallChoice.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().ContainSingle();
        group.Choices.Should().Contain(largeChoice);
        group.Choices.Should().NotContain(smallChoice);
    }

    #endregion

    #region UpdateChoice() Method Tests

    [Test]
    public void UpdateChoice_WithValidInputs_ShouldSucceedAndUpdateChoice()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Small", DefaultPrice, false, 1);
        var originalChoice = group.Choices.First(c => c.Name == "Small");
        var originalChoiceId = originalChoice.Id;
        var newName = "Extra Large";
        var newPriceAdjustment = new Money(3.00m, Currencies.Default);
        var newIsDefault = true;

        // Act
        var result = group.UpdateChoice(originalChoiceId, newName, newPriceAdjustment, newIsDefault);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().ContainSingle();
        var updatedChoice = group.Choices.Single();
        updatedChoice.Name.Should().Be(newName);
        updatedChoice.PriceAdjustment.Should().Be(newPriceAdjustment);
        updatedChoice.IsDefault.Should().Be(newIsDefault);
        // Note: The choice will have a new ID due to re-creation for immutability

        group.DomainEvents.Should().Contain(e => e.GetType() == typeof(CustomizationChoiceUpdated));
        var choiceUpdatedEvent = group.DomainEvents.OfType<CustomizationChoiceUpdated>().Single();
        choiceUpdatedEvent.CustomizationGroupId.Should().Be((CustomizationGroupId)group.Id);
        choiceUpdatedEvent.ChoiceId.Should().Be(originalChoiceId);
        choiceUpdatedEvent.NewName.Should().Be(newName);
        choiceUpdatedEvent.NewPriceAdjustment.Should().Be(newPriceAdjustment);
        choiceUpdatedEvent.IsDefault.Should().Be(newIsDefault);
        choiceUpdatedEvent.DisplayOrder.Should().Be(1); // Should remain unchanged
    }

    [Test]
    public void UpdateChoice_WithInvalidChoiceId_ShouldFailWithInvalidChoiceIdError()
    {
        // Arrange
        var group = CreateValidGroup();
        var nonExistentChoiceId = ChoiceId.CreateUnique();

        // Act
        var result = group.UpdateChoice(nonExistentChoiceId, "New Name", DefaultPrice, false);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidChoiceId);
    }

    [Test]
    public void UpdateChoice_WithDuplicateName_ShouldFailWithChoiceNameNotUniqueError()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Large", PriceAdjustment, false, 2);

        var choice1 = group.Choices.First(c => c.Name == "Small");
        var choice2 = group.Choices.First(c => c.Name == "Large");

        // Act - Try to update choice2 to have the same name as choice1
        var result = group.UpdateChoice(choice2.Id, "Small", DefaultPrice, false);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CustomizationGroupErrors.ChoiceNameNotUnique);
        // Verify the original choice remains unchanged
        var unchangedChoice = group.Choices.First(c => c.Name == "Large");
        unchangedChoice.Name.Should().Be("Large");
    }

    [Test]
    public void UpdateChoice_WithSameName_ShouldSucceedAndUpdateOtherProperties()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Large", DefaultPrice, false, 1);
        var newPriceAdjustment = new Money(4.00m, Currencies.Default);
        var originalChoice = group.Choices.First(c => c.Name == "Large");

        // Act - Update with same name but different price and default flag
        var result = group.UpdateChoice(originalChoice.Id, "Large", newPriceAdjustment, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedChoice = group.Choices.Single();
        updatedChoice.Name.Should().Be("Large");
        updatedChoice.PriceAdjustment.Should().Be(newPriceAdjustment);
        updatedChoice.IsDefault.Should().BeTrue();
    }

    [Test]
    public void UpdateChoice_WithNewDisplayOrder_ShouldSucceedAndUpdateDisplayOrder()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Large", DefaultPrice, false, 1);
        const int newDisplayOrder = 5;
        var originalChoice = group.Choices.First(c => c.Name == "Large");

        // Act
        var result = group.UpdateChoice(originalChoice.Id, "Large", DefaultPrice, false, newDisplayOrder);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedChoice = group.Choices.Single();
        updatedChoice.DisplayOrder.Should().Be(newDisplayOrder);

        var choiceUpdatedEvent = group.DomainEvents.OfType<CustomizationChoiceUpdated>().Single();
        choiceUpdatedEvent.DisplayOrder.Should().Be(newDisplayOrder);
    }

    [Test]
    public void UpdateChoice_WithNullDisplayOrder_ShouldSucceedAndKeepExistingDisplayOrder()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Large", DefaultPrice, false, 3);
        var originalChoice = group.Choices.First(c => c.Name == "Large");

        // Act - Pass null for display order
        var result = group.UpdateChoice(originalChoice.Id, "Extra Large", DefaultPrice, true, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedChoice = group.Choices.Single();
        updatedChoice.DisplayOrder.Should().Be(3); // Should remain unchanged
        updatedChoice.Name.Should().Be("Extra Large");
        updatedChoice.IsDefault.Should().BeTrue();
    }

    [Test]
    public void UpdateChoice_WithDuplicateDisplayOrder_ShouldSucceedAndAllowTies()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Large", PriceAdjustment, false, 2);

        var choice1 = group.Choices.First(c => c.Name == "Small");
        var choice2 = group.Choices.First(c => c.Name == "Large");

        // Act - Update choice2 to have the same display order as choice1
        var result = group.UpdateChoice(choice2.Id, "Large", PriceAdjustment, false, 1);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Ties are allowed

        // Verify both choices have the same display order
        var updatedChoice1 = group.Choices.First(c => c.Name == "Small");
        var updatedChoice2 = group.Choices.First(c => c.Name == "Large");

        updatedChoice1.DisplayOrder.Should().Be(1);
        updatedChoice2.DisplayOrder.Should().Be(1);

        // Verify ordering: both have same display order, so sorted by name
        var orderedChoices = group.Choices.ToList();
        orderedChoices[0].Name.Should().Be("Large"); // "Large" comes before "Small" alphabetically
        orderedChoices[1].Name.Should().Be("Small");
    }

    #endregion

    #region AddChoiceWithAutoOrder() Method Tests

    [Test]
    public void AddChoiceWithAutoOrder_FirstChoice_ShouldSucceedAndAssignOrderOne()
    {
        // Arrange
        var group = CreateValidGroup();
        const string choiceName = "Large";

        // Act
        var result = group.AddChoiceWithAutoOrder(choiceName, PriceAdjustment, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().ContainSingle();
        var addedChoice = group.Choices.Single();
        addedChoice.Name.Should().Be(choiceName);
        addedChoice.DisplayOrder.Should().Be(1);
        addedChoice.PriceAdjustment.Should().Be(PriceAdjustment);
        addedChoice.IsDefault.Should().BeFalse();

        group.DomainEvents.Should().Contain(e => e.GetType() == typeof(CustomizationChoiceAdded));
    }

    [Test]
    public void AddChoiceWithAutoOrder_MultipleChoices_ShouldSucceedAndAssignIncrementalOrders()
    {
        // Arrange
        var group = CreateValidGroup();

        // Act
        var result1 = group.AddChoiceWithAutoOrder("Small", DefaultPrice);
        var result2 = group.AddChoiceWithAutoOrder("Medium", PriceAdjustment);
        var result3 = group.AddChoiceWithAutoOrder("Large", new Money(5.00m, Currencies.Default));

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();

        group.Choices.Should().HaveCount(3);
        group.Choices[0].Name.Should().Be("Small");
        group.Choices[0].DisplayOrder.Should().Be(1);
        group.Choices[1].Name.Should().Be("Medium");
        group.Choices[1].DisplayOrder.Should().Be(2);
        group.Choices[2].Name.Should().Be("Large");
        group.Choices[2].DisplayOrder.Should().Be(3);
    }

    [Test]
    public void AddChoiceWithAutoOrder_WithExistingChoicesWithGaps_ShouldAssignNextMaxOrder()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Large", new Money(5.00m, Currencies.Default), false, 10); // Gap in ordering

        // Act
        var result = group.AddChoiceWithAutoOrder("Medium", PriceAdjustment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().HaveCount(3);
        var addedChoice = group.Choices.First(c => c.Name == "Medium");
        addedChoice.DisplayOrder.Should().Be(11); // Max existing order (10) + 1
    }

    [Test]
    public void AddChoiceWithAutoOrder_WithDuplicateName_ShouldFailWithChoiceNameNotUniqueError()
    {
        // Arrange
        var group = CreateValidGroup();
        group.AddChoiceWithAutoOrder("Large", DefaultPrice);

        // Act
        var result = group.AddChoiceWithAutoOrder("Large", PriceAdjustment); // Duplicate name

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CustomizationGroupErrors.ChoiceNameNotUnique);
        group.Choices.Should().ContainSingle(); // Only first choice should remain
    }

    #endregion

    #region Helper Methods

    private static CustomizationGroup CreateValidGroup()
    {
        return CustomizationGroup.Create(
            DefaultRestaurantId,
            DefaultGroupName,
            DefaultMinSelections,
            DefaultMaxSelections).Value;
    }

    #endregion
}
