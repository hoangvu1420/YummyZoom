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

[TestFixture]
public class CustomizationGroupTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultGroupName = "Size Options";
    private const int DefaultMinSelections = 1;
    private const int DefaultMaxSelections = 1;
    private static readonly Money DefaultPrice = new Money(1.00m, Currencies.Default);
    private static readonly Money PriceAdjustment = new Money(2.50m, Currencies.Default);

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeGroupCorrectly()
    {
        // Arrange & Act
        var result = CustomizationGroup.Create(
            DefaultRestaurantId,
            DefaultGroupName,
            DefaultMinSelections,
            DefaultMaxSelections);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var group = result.Value;
        group.Id.Value.Should().NotBe(Guid.Empty);
        group.RestaurantId.Should().Be(DefaultRestaurantId);
        group.GroupName.Should().Be(DefaultGroupName);
        group.MinSelections.Should().Be(DefaultMinSelections);
        group.MaxSelections.Should().Be(DefaultMaxSelections);
        group.Choices.Should().BeEmpty();
        group.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(CustomizationGroupCreated));

        var groupCreatedEvent = group.DomainEvents.OfType<CustomizationGroupCreated>().Single();
        groupCreatedEvent.CustomizationGroupId.Should().Be((CustomizationGroupId)group.Id);
        groupCreatedEvent.RestaurantId.Should().Be(DefaultRestaurantId);
        groupCreatedEvent.GroupName.Should().Be(DefaultGroupName);
    }

    [Test]
    public void Create_WithChoices_ShouldSucceedAndInitializeWithChoices()
    {
        // Arrange
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice).Value;
        var choice2 = CustomizationChoice.Create("Large", PriceAdjustment).Value;
        var choices = new List<CustomizationChoice> { choice1, choice2 };

        // Act
        var result = CustomizationGroup.Create(
            DefaultRestaurantId,
            DefaultGroupName,
            DefaultMinSelections,
            DefaultMaxSelections,
            choices);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var group = result.Value;
        group.Choices.Should().HaveCount(2);
        group.Choices.Should().Contain(choice1);
        group.Choices.Should().Contain(choice2);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithNullOrEmptyGroupName_ShouldFailWithGroupNameRequiredError(string invalidName)
    {
        // Arrange & Act
        var result = CustomizationGroup.Create(
            DefaultRestaurantId,
            invalidName,
            DefaultMinSelections,
            DefaultMaxSelections);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.GroupNameRequired);
    }

    [Test]
    public void Create_WithNullGroupName_ShouldFailWithGroupNameRequiredError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = CustomizationGroup.Create(
            DefaultRestaurantId,
            null,
            DefaultMinSelections,
            DefaultMaxSelections);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.GroupNameRequired);
    }

    [Test]
    public void Create_WithMaxSelectionsLessThanMinSelections_ShouldFailWithInvalidSelectionRangeError()
    {
        // Arrange & Act
        var result = CustomizationGroup.Create(
            DefaultRestaurantId,
            DefaultGroupName,
            minSelections: 2,
            maxSelections: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidSelectionRange);
    }

    [Test]
    public void Create_WithGroupNameContainingWhitespace_ShouldTrimGroupName()
    {
        // Arrange
        var groupNameWithWhitespace = "  Size Options  ";

        // Act
        var result = CustomizationGroup.Create(
            DefaultRestaurantId,
            groupNameWithWhitespace,
            DefaultMinSelections,
            DefaultMaxSelections);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GroupName.Should().Be(DefaultGroupName);
    }

    [TestCase(0, 0)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(1, 3)]
    [TestCase(2, 5)]
    public void Create_WithValidSelectionRanges_ShouldSucceed(int minSelections, int maxSelections)
    {
        // Arrange & Act
        var result = CustomizationGroup.Create(
            DefaultRestaurantId,
            DefaultGroupName,
            minSelections,
            maxSelections);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinSelections.Should().Be(minSelections);
        result.Value.MaxSelections.Should().Be(maxSelections);
    }

    #endregion

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

    #region UpdateGroupDetails() Method Tests

    [Test]
    public void UpdateGroupDetails_WithValidInputs_ShouldSucceedAndUpdateProperties()
    {
        // Arrange
        var group = CreateValidGroup();
        var newGroupName = "Toppings";
        var newMinSelections = 0;
        var newMaxSelections = 5;

        // Act
        var result = group.UpdateGroupDetails(newGroupName, newMinSelections, newMaxSelections);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.GroupName.Should().Be(newGroupName);
        group.MinSelections.Should().Be(newMinSelections);
        group.MaxSelections.Should().Be(newMaxSelections);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void UpdateGroupDetails_WithNullOrEmptyGroupName_ShouldFailWithGroupNameRequiredError(string invalidName)
    {
        // Arrange
        var group = CreateValidGroup();
        var originalGroupName = group.GroupName;

        // Act
        var result = group.UpdateGroupDetails(invalidName, DefaultMinSelections, DefaultMaxSelections);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.GroupNameRequired);
        group.GroupName.Should().Be(originalGroupName); // Should remain unchanged
    }

    [Test]
    public void UpdateGroupDetails_WithNullGroupName_ShouldFailWithGroupNameRequiredError()
    {
        // Arrange
        var group = CreateValidGroup();
        var originalGroupName = group.GroupName;

        // Act
#pragma warning disable CS8625
        var result = group.UpdateGroupDetails(null, DefaultMinSelections, DefaultMaxSelections);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.GroupNameRequired);
        group.GroupName.Should().Be(originalGroupName); // Should remain unchanged
    }

    [Test]
    public void UpdateGroupDetails_WithMaxSelectionsLessThanMinSelections_ShouldFailWithInvalidSelectionRangeError()
    {
        // Arrange
        var group = CreateValidGroup();
        var originalMinSelections = group.MinSelections;
        var originalMaxSelections = group.MaxSelections;

        // Act
        var result = group.UpdateGroupDetails(DefaultGroupName, minSelections: 3, maxSelections: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidSelectionRange);
        group.MinSelections.Should().Be(originalMinSelections); // Should remain unchanged
        group.MaxSelections.Should().Be(originalMaxSelections); // Should remain unchanged
    }

    [Test]
    public void UpdateGroupDetails_WithGroupNameContainingWhitespace_ShouldTrimGroupName()
    {
        // Arrange
        var group = CreateValidGroup();
        var groupNameWithWhitespace = "  Updated Group Name  ";
        var expectedTrimmedName = "Updated Group Name";

        // Act
        var result = group.UpdateGroupDetails(groupNameWithWhitespace, DefaultMinSelections, DefaultMaxSelections);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.GroupName.Should().Be(expectedTrimmedName);
    }

    [TestCase(0, 0)]
    [TestCase(0, 10)]
    [TestCase(1, 1)]
    [TestCase(2, 5)]
    public void UpdateGroupDetails_WithValidSelectionRanges_ShouldSucceed(int minSelections, int maxSelections)
    {
        // Arrange
        var group = CreateValidGroup();

        // Act
        var result = group.UpdateGroupDetails("Updated Name", minSelections, maxSelections);

        // Assert
        result.IsSuccess.Should().BeTrue();
        group.MinSelections.Should().Be(minSelections);
        group.MaxSelections.Should().Be(maxSelections);
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
