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
/// Tests for CustomizationGroup ordering functionality including ReorderChoices and display order management.
/// </summary>
[TestFixture]
public class CustomizationGroupOrderingTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultGroupName = "Size Options";
    private const int DefaultMinSelections = 1;
    private const int DefaultMaxSelections = 1;
    private static readonly Money DefaultPrice = new Money(1.00m, Currencies.Default);
    private static readonly Money PriceAdjustment = new Money(2.50m, Currencies.Default);
    private static readonly Money HighPriceAdjustment = new Money(5.00m, Currencies.Default);

    #region ReorderChoices() Method Tests

    [Test]
    public void ReorderChoices_WithSingleValidChoice_ShouldSucceedAndUpdateOrder()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice = CustomizationChoice.Create("Large", DefaultPrice, false, 1).Value;
        group.AddChoice(choice);
        
        var orderChanges = new List<(ChoiceId choiceId, int newDisplayOrder)>
        {
            (choice.Id, 5)
        };

        // Act
        var result = group.ReorderChoices(orderChanges);

        // Assert
        result.IsSuccess.Should().BeTrue();
        choice.DisplayOrder.Should().Be(5);
        
        var reorderEvent = group.DomainEvents.OfType<CustomizationChoicesReordered>().SingleOrDefault();
        reorderEvent.Should().NotBeNull();
        reorderEvent!.CustomizationGroupId.Should().Be((CustomizationGroupId)group.Id);
        reorderEvent.ReorderedChoices.Should().ContainKey(choice.Id);
        reorderEvent.ReorderedChoices[choice.Id].Should().Be(5);
    }

    [Test]
    public void ReorderChoices_WithMultipleValidChoices_ShouldSucceedAndUpdateAllOrders()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice, false, 1).Value;
        var choice2 = CustomizationChoice.Create("Medium", PriceAdjustment, false, 2).Value;
        var choice3 = CustomizationChoice.Create("Large", HighPriceAdjustment, false, 3).Value;
        
        group.AddChoice(choice1);
        group.AddChoice(choice2);
        group.AddChoice(choice3);
        
        var orderChanges = new List<(ChoiceId choiceId, int newDisplayOrder)>
        {
            (choice1.Id, 10),
            (choice3.Id, 5)
        };

        // Act
        var result = group.ReorderChoices(orderChanges);

        // Assert
        result.IsSuccess.Should().BeTrue();
        choice1.DisplayOrder.Should().Be(10);
        choice2.DisplayOrder.Should().Be(2); // Unchanged
        choice3.DisplayOrder.Should().Be(5);
        
        var reorderEvent = group.DomainEvents.OfType<CustomizationChoicesReordered>().SingleOrDefault();
        reorderEvent.Should().NotBeNull();
        reorderEvent!.ReorderedChoices.Should().HaveCount(2);
        reorderEvent.ReorderedChoices[choice1.Id].Should().Be(10);
        reorderEvent.ReorderedChoices[choice3.Id].Should().Be(5);
    }

    [Test]
    public void ReorderChoices_WithSparseOrdering_ShouldSucceedAndAllowGaps()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice, false, 1).Value;
        var choice2 = CustomizationChoice.Create("Medium", PriceAdjustment, false, 2).Value;
        var choice3 = CustomizationChoice.Create("Large", HighPriceAdjustment, false, 3).Value;
        
        group.AddChoice(choice1);
        group.AddChoice(choice2);
        group.AddChoice(choice3);
        
        // Test sparse ordering: 1, 5, 10
        var orderChanges = new List<(ChoiceId choiceId, int newDisplayOrder)>
        {
            (choice1.Id, 1),
            (choice2.Id, 5),
            (choice3.Id, 10)
        };

        // Act
        var result = group.ReorderChoices(orderChanges);

        // Assert
        result.IsSuccess.Should().BeTrue();
        choice1.DisplayOrder.Should().Be(1);
        choice2.DisplayOrder.Should().Be(5);
        choice3.DisplayOrder.Should().Be(10);
    }

    [Test]
    public void ReorderChoices_WithInvalidChoiceId_ShouldFailWithChoiceNotFoundError()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice = CustomizationChoice.Create("Large", DefaultPrice, false, 1).Value;
        group.AddChoice(choice);
        
        var invalidChoiceId = ChoiceId.CreateUnique(); // Not in the group
        var orderChanges = new List<(ChoiceId choiceId, int newDisplayOrder)>
        {
            (invalidChoiceId, 5)
        };

        // Act
        var result = group.ReorderChoices(orderChanges);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.ChoiceNotFoundForReordering);
        choice.DisplayOrder.Should().Be(1); // Should remain unchanged
    }

    [Test]
    public void ReorderChoices_WithNegativeDisplayOrder_ShouldFailWithInvalidDisplayOrderError()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice = CustomizationChoice.Create("Large", DefaultPrice, false, 1).Value;
        group.AddChoice(choice);
        
        var orderChanges = new List<(ChoiceId choiceId, int newDisplayOrder)>
        {
            (choice.Id, -1)
        };

        // Act
        var result = group.ReorderChoices(orderChanges);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidDisplayOrder);
        choice.DisplayOrder.Should().Be(1); // Should remain unchanged
    }

    [Test]
    public void ReorderChoices_WithDuplicateDisplayOrders_ShouldFailWithDuplicateDisplayOrderError()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice, false, 1).Value;
        var choice2 = CustomizationChoice.Create("Medium", PriceAdjustment, false, 2).Value;
        var choice3 = CustomizationChoice.Create("Large", HighPriceAdjustment, false, 3).Value;
        
        group.AddChoice(choice1);
        group.AddChoice(choice2);
        group.AddChoice(choice3);
        
        // Try to set both choice1 and choice2 to order 5
        var orderChanges = new List<(ChoiceId choiceId, int newDisplayOrder)>
        {
            (choice1.Id, 5),
            (choice2.Id, 5) // Duplicate!
        };

        // Act
        var result = group.ReorderChoices(orderChanges);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.DuplicateDisplayOrder);
        choice1.DisplayOrder.Should().Be(1); // Should remain unchanged
        choice2.DisplayOrder.Should().Be(2); // Should remain unchanged
    }

    [Test]
    public void ReorderChoices_WithConflictWithUnchangedChoice_ShouldSucceedAndAllowTies()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice, false, 1).Value;
        var choice2 = CustomizationChoice.Create("Medium", PriceAdjustment, false, 2).Value;
        
        group.AddChoice(choice1);
        group.AddChoice(choice2);
        
        // Set choice1 to order 2 (which choice2 already has) - this should be allowed as ties are permitted
        var orderChanges = new List<(ChoiceId choiceId, int newDisplayOrder)>
        {
            (choice1.Id, 2) // Creates a tie with choice2
        };

        // Act
        var result = group.ReorderChoices(orderChanges);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Ties are allowed
        choice1.DisplayOrder.Should().Be(2); // Should be updated
        choice2.DisplayOrder.Should().Be(2); // Should remain the same
        
        // Verify ordering: both have same display order, so sorted by name
        var orderedChoices = group.Choices.ToList();
        orderedChoices[0].Name.Should().Be("Medium"); // "Medium" comes before "Small" alphabetically
        orderedChoices[1].Name.Should().Be("Small");
    }

    [Test]
    public void ReorderChoices_WithEmptyOrderChanges_ShouldSucceedAndDoNothing()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice = CustomizationChoice.Create("Large", DefaultPrice, false, 1).Value;
        group.AddChoice(choice);
        
        var orderChanges = new List<(ChoiceId choiceId, int newDisplayOrder)>();

        // Act
        var result = group.ReorderChoices(orderChanges);

        // Assert
        result.IsSuccess.Should().BeTrue();
        choice.DisplayOrder.Should().Be(1); // Should remain unchanged
        group.DomainEvents.OfType<CustomizationChoicesReordered>().Should().BeEmpty();
    }

    [Test]
    public void ReorderChoices_WithNullOrderChanges_ShouldSucceedAndDoNothing()
    {
        // Arrange
        var group = CreateValidGroup();
        var choice = CustomizationChoice.Create("Large", DefaultPrice, false, 1).Value;
        group.AddChoice(choice);

        // Act
        var result = group.ReorderChoices(null!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        choice.DisplayOrder.Should().Be(1); // Should remain unchanged
        group.DomainEvents.OfType<CustomizationChoicesReordered>().Should().BeEmpty();
    }

    #endregion

    #region Choices Property Ordering Tests

    [Test]
    public void Choices_WithMultipleChoices_ShouldReturnOrderedByDisplayOrderThenName()
    {
        // Arrange
        var group = CreateValidGroup();
        
        // Add choices in non-order sequence
        var choice1 = CustomizationChoice.Create("Large", HighPriceAdjustment, false, 3).Value;
        var choice2 = CustomizationChoice.Create("Small", DefaultPrice, false, 1).Value;
        var choice3 = CustomizationChoice.Create("Medium", PriceAdjustment, false, 2).Value;
        
        group.AddChoice(choice1);
        group.AddChoice(choice2);
        group.AddChoice(choice3);

        // Act
        var choices = group.Choices;

        // Assert
        choices.Should().HaveCount(3);
        choices[0].Should().Be(choice2); // DisplayOrder 1
        choices[1].Should().Be(choice3); // DisplayOrder 2
        choices[2].Should().Be(choice1); // DisplayOrder 3
    }

    [Test]
    public void Choices_WithSameDisplayOrder_ShouldOrderByNameAscending()
    {
        // Arrange
        var group = CreateValidGroup();
        
        // Both choices have the same display order, should be ordered by name
        var choiceB = CustomizationChoice.Create("Beta", DefaultPrice, false, 1).Value;
        var choiceA = CustomizationChoice.Create("Alpha", PriceAdjustment, false, 1).Value;
        
        group.AddChoice(choiceB);
        group.AddChoice(choiceA);

        // Act
        var choices = group.Choices;

        // Assert
        choices.Should().HaveCount(2);
        choices[0].Name.Should().Be("Alpha"); // Alphabetically first
        choices[1].Name.Should().Be("Beta");
    }

    [Test]
    public void Choices_WithEmptyGroup_ShouldReturnEmptyList()
    {
        // Arrange
        var group = CreateValidGroup();

        // Act
        var choices = group.Choices;

        // Assert
        choices.Should().BeEmpty();
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
