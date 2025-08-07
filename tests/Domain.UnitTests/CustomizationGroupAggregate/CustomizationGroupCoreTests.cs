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
/// Tests for core CustomizationGroup aggregate functionality including creation and group details management.
/// </summary>
[TestFixture]
public class CustomizationGroupCoreTests
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
        result.ShouldBeSuccessful();
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
        result.ShouldBeSuccessful();
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
        result.ShouldBeSuccessful();
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
        result.ShouldBeSuccessful();
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
        result.ShouldBeSuccessful();
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
        result.ShouldBeSuccessful();
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

    #region Property Immutability Tests

    [Test]
    public void Choices_ShouldBeReadOnly()
    {
        // Arrange
        var choice1 = CustomizationChoice.Create("Small", DefaultPrice).Value;
        var choices = new List<CustomizationChoice> { choice1 };

        // Act
        var group = CustomizationGroup.Create(
            DefaultRestaurantId,
            DefaultGroupName,
            DefaultMinSelections,
            DefaultMaxSelections,
            choices).Value;

        // Assert
        // Type check
        var property = typeof(CustomizationGroup).GetProperty(nameof(CustomizationGroup.Choices));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<CustomizationChoice>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<CustomizationChoice>)group.Choices).Add(
            CustomizationChoice.Create("Large", PriceAdjustment).Value);
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the group
        choices.Add(CustomizationChoice.Create("Large", PriceAdjustment).Value);
        group.Choices.Should().HaveCount(1);
    }

    #endregion
}
