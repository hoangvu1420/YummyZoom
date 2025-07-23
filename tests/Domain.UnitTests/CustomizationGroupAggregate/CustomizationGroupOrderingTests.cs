using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
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
        group.AddChoice("Large", DefaultPrice, false, 1);
        var choice = group.Choices.Single();
        
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
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Medium", PriceAdjustment, false, 2);
        group.AddChoice("Large", HighPriceAdjustment, false, 3);
        
        var choice1 = group.Choices.First(c => c.Name == "Small");
        var choice2 = group.Choices.First(c => c.Name == "Medium");
        var choice3 = group.Choices.First(c => c.Name == "Large");
        
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
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Medium", PriceAdjustment, false, 2);
        group.AddChoice("Large", HighPriceAdjustment, false, 3);
        
        var choice1 = group.Choices.First(c => c.Name == "Small");
        var choice2 = group.Choices.First(c => c.Name == "Medium");
        var choice3 = group.Choices.First(c => c.Name == "Large");
        
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
        group.AddChoice("Large", DefaultPrice, false, 1);
        var choice = group.Choices.Single();
        
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
        group.AddChoice("Large", DefaultPrice, false, 1);
        var choice = group.Choices.Single();
        
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
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Medium", PriceAdjustment, false, 2);
        group.AddChoice("Large", HighPriceAdjustment, false, 3);
        
        var choice1 = group.Choices.First(c => c.Name == "Small");
        var choice2 = group.Choices.First(c => c.Name == "Medium");
        var choice3 = group.Choices.First(c => c.Name == "Large");
        
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
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Medium", PriceAdjustment, false, 2);
        
        var choice1 = group.Choices.First(c => c.Name == "Small");
        var choice2 = group.Choices.First(c => c.Name == "Medium");
        
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
        group.AddChoice("Large", DefaultPrice, false, 1);
        var choice = group.Choices.Single();
        
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
        group.AddChoice("Large", DefaultPrice, false, 1);
        var choice = group.Choices.Single();

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
        group.AddChoice("Large", HighPriceAdjustment, false, 3);
        group.AddChoice("Small", DefaultPrice, false, 1);
        group.AddChoice("Medium", PriceAdjustment, false, 2);
        
        var choice1 = group.Choices.First(c => c.Name == "Large");
        var choice2 = group.Choices.First(c => c.Name == "Small");
        var choice3 = group.Choices.First(c => c.Name == "Medium");

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
        group.AddChoice("Beta", DefaultPrice, false, 1);
        group.AddChoice("Alpha", PriceAdjustment, false, 1);
        
        var choiceB = group.Choices.First(c => c.Name == "Beta");
        var choiceA = group.Choices.First(c => c.Name == "Alpha");

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
