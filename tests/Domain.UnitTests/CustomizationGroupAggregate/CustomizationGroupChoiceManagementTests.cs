using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.Entities;
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
        var choice = CustomizationChoice.Create("Large", PriceAdjustment).Value;

        // Act
        var result = group.AddChoice(choice);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().ContainSingle();
        group.Choices.Should().Contain(choice);
        group.DomainEvents.Should().Contain(e => e.GetType() == typeof(CustomizationChoiceAdded));

        var choiceAddedEvent = group.DomainEvents.OfType<CustomizationChoiceAdded>().Single();
        choiceAddedEvent.CustomizationGroupId.Should().Be((CustomizationGroupId)group.Id);
        choiceAddedEvent.ChoiceId.Should().Be(choice.Id);
        choiceAddedEvent.Name.Should().Be(choice.Name);
    }

    [Test]
    public void AddChoice_WithDuplicateChoiceName_ShouldFailWithChoiceNameNotUniqueError()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice1 = CustomizationChoice.Create("Large", DefaultPrice).Value;
        var choice2 = CustomizationChoice.Create("Large", PriceAdjustment).Value; // Same name
        group.AddChoice(choice1);

        // Act
        var result = group.AddChoice(choice2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.ChoiceNameNotUnique);
        group.Choices.Should().ContainSingle(); // Only the first choice should remain
        group.Choices.Should().Contain(choice1);
        group.Choices.Should().NotContain(choice2);
    }

    [Test]
    public void AddChoice_MultipleValidChoices_ShouldSucceedAndAddAllChoices()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice).Value;
        var choice2 = CustomizationChoice.Create("Medium", PriceAdjustment).Value;
        var choice3 = CustomizationChoice.Create("Large", new Money(5.00m, Currencies.Default)).Value;

        // Act
        var result1 = group.AddChoice(choice1);
        var result2 = group.AddChoice(choice2);
        var result3 = group.AddChoice(choice3);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        group.Choices.Should().HaveCount(3);
        group.Choices.Should().Contain(choice1);
        group.Choices.Should().Contain(choice2);
        group.Choices.Should().Contain(choice3);
    }

    #endregion

    #region RemoveChoice() Method Tests

    [Test]
    public void RemoveChoice_WithValidChoiceId_ShouldSucceedAndRemoveChoice()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice = CustomizationChoice.Create("Large", PriceAdjustment).Value;
        group.AddChoice(choice);

        // Act
        var result = group.RemoveChoice(choice.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().BeEmpty();
        group.DomainEvents.Should().Contain(e => e.GetType() == typeof(CustomizationChoiceRemoved));

        var choiceRemovedEvent = group.DomainEvents.OfType<CustomizationChoiceRemoved>().Single();
        choiceRemovedEvent.CustomizationGroupId.Should().Be((CustomizationGroupId)group.Id);
        choiceRemovedEvent.ChoiceId.Should().Be(choice.Id);
        choiceRemovedEvent.Name.Should().Be(choice.Name);
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
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidChoiceId);
    }

    [Test]
    public void RemoveChoice_WithMultipleChoices_ShouldRemoveOnlySpecifiedChoice()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice).Value;
        var choice2 = CustomizationChoice.Create("Large", PriceAdjustment).Value;
        group.AddChoice(choice1);
        group.AddChoice(choice2);

        // Act
        var result = group.RemoveChoice(choice1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.Choices.Should().ContainSingle();
        group.Choices.Should().Contain(choice2);
        group.Choices.Should().NotContain(choice1);
    }

    #endregion

    #region UpdateChoice() Method Tests

    [Test]
    public void UpdateChoice_WithValidInputs_ShouldSucceedAndUpdateChoice()
    {
        // Arrange
        var group = CreateValidGroup();
        var originalChoice = CustomizationChoice.Create("Small", DefaultPrice).Value;
        group.AddChoice(originalChoice);
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
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidChoiceId);
    }

    [Test]
    public void UpdateChoice_WithDuplicateName_ShouldFailWithChoiceNameNotUniqueError()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice).Value;
        var choice2 = CustomizationChoice.Create("Large", PriceAdjustment).Value;
        group.AddChoice(choice1);
        group.AddChoice(choice2);

        // Act - Try to update choice2 to have the same name as choice1
        var result = group.UpdateChoice(choice2.Id, "Small", DefaultPrice, false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.ChoiceNameNotUnique);
        // Verify the original choice remains unchanged
        var unchangedChoice = group.Choices.First(c => c.Id == choice2.Id);
        unchangedChoice.Name.Should().Be("Large");
    }

    [Test]
    public void UpdateChoice_WithSameName_ShouldSucceedAndUpdateOtherProperties()
    {
        // Arrange
        var group = CreateValidGroup();
        var originalChoice = CustomizationChoice.Create("Large", DefaultPrice, false).Value;
        group.AddChoice(originalChoice);
        var newPriceAdjustment = new Money(4.00m, Currencies.Default);

        // Act - Update with same name but different price and default flag
        var result = group.UpdateChoice(originalChoice.Id, "Large", newPriceAdjustment, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedChoice = group.Choices.Single();
        updatedChoice.Name.Should().Be("Large");
        updatedChoice.PriceAdjustment.Should().Be(newPriceAdjustment);
        updatedChoice.IsDefault.Should().BeTrue();
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
