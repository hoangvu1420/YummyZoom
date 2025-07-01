using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.CustomizationGroupAggregate.Events;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CustomizationGroupAggregate;

[TestFixture]
public class CustomizationGroupEventsTests
{
    #region CustomizationGroupCreated Event Tests

    [Test]
    public void CustomizationGroupCreated_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var groupName = "Size Options";

        // Act
        var customizationGroupCreatedEvent = new CustomizationGroupCreated(
            customizationGroupId, 
            restaurantId, 
            groupName);

        // Assert
        customizationGroupCreatedEvent.CustomizationGroupId.Should().Be(customizationGroupId);
        customizationGroupCreatedEvent.RestaurantId.Should().Be(restaurantId);
        customizationGroupCreatedEvent.GroupName.Should().Be(groupName);
    }

    [Test]
    public void CustomizationGroupCreated_WithEmptyGroupName_ShouldStillInitialize()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var groupName = "";

        // Act
        var customizationGroupCreatedEvent = new CustomizationGroupCreated(
            customizationGroupId, 
            restaurantId, 
            groupName);

        // Assert
        customizationGroupCreatedEvent.CustomizationGroupId.Should().Be(customizationGroupId);
        customizationGroupCreatedEvent.RestaurantId.Should().Be(restaurantId);
        customizationGroupCreatedEvent.GroupName.Should().Be(groupName);
    }

    [Test]
    public void CustomizationGroupCreated_WithSpecialCharactersInName_ShouldInitializeCorrectly()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var groupName = "Spice Level (1-10) & Heat Options!";

        // Act
        var customizationGroupCreatedEvent = new CustomizationGroupCreated(
            customizationGroupId, 
            restaurantId, 
            groupName);

        // Assert
        customizationGroupCreatedEvent.CustomizationGroupId.Should().Be(customizationGroupId);
        customizationGroupCreatedEvent.RestaurantId.Should().Be(restaurantId);
        customizationGroupCreatedEvent.GroupName.Should().Be(groupName);
    }

    #endregion

    #region CustomizationChoiceAdded Event Tests

    [Test]
    public void CustomizationChoiceAdded_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var choiceId = ChoiceId.CreateUnique();
        var choiceName = "Large";

        // Act
        var customizationChoiceAddedEvent = new CustomizationChoiceAdded(
            customizationGroupId, 
            choiceId, 
            choiceName);

        // Assert
        customizationChoiceAddedEvent.CustomizationGroupId.Should().Be(customizationGroupId);
        customizationChoiceAddedEvent.ChoiceId.Should().Be(choiceId);
        customizationChoiceAddedEvent.Name.Should().Be(choiceName);
    }

    [Test]
    public void CustomizationChoiceAdded_WithEmptyChoiceName_ShouldStillInitialize()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var choiceId = ChoiceId.CreateUnique();
        var choiceName = "";

        // Act
        var customizationChoiceAddedEvent = new CustomizationChoiceAdded(
            customizationGroupId, 
            choiceId, 
            choiceName);

        // Assert
        customizationChoiceAddedEvent.CustomizationGroupId.Should().Be(customizationGroupId);
        customizationChoiceAddedEvent.ChoiceId.Should().Be(choiceId);
        customizationChoiceAddedEvent.Name.Should().Be(choiceName);
    }

    [Test]
    public void CustomizationChoiceAdded_WithSpecialCharactersInName_ShouldInitializeCorrectly()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var choiceId = ChoiceId.CreateUnique();
        var choiceName = "Extra-Large (20oz) + Free Refill";

        // Act
        var customizationChoiceAddedEvent = new CustomizationChoiceAdded(
            customizationGroupId, 
            choiceId, 
            choiceName);

        // Assert
        customizationChoiceAddedEvent.CustomizationGroupId.Should().Be(customizationGroupId);
        customizationChoiceAddedEvent.ChoiceId.Should().Be(choiceId);
        customizationChoiceAddedEvent.Name.Should().Be(choiceName);
    }

    #endregion

    #region Event Equality Tests (for record types)

    [Test]
    public void CustomizationGroupCreated_WithSameProperties_ShouldBeEqual()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var groupName = "Size Options";

        var event1 = new CustomizationGroupCreated(customizationGroupId, restaurantId, groupName);
        var event2 = new CustomizationGroupCreated(customizationGroupId, restaurantId, groupName);

        // Act & Assert
        event1.Should().Be(event2);
        event1.Equals(event2).Should().BeTrue();
        (event1 == event2).Should().BeTrue();
        (event1 != event2).Should().BeFalse();
    }

    [Test]
    public void CustomizationGroupCreated_WithDifferentProperties_ShouldNotBeEqual()
    {
        // Arrange
        var customizationGroupId1 = CustomizationGroupId.CreateUnique();
        var customizationGroupId2 = CustomizationGroupId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var groupName = "Size Options";

        var event1 = new CustomizationGroupCreated(customizationGroupId1, restaurantId, groupName);
        var event2 = new CustomizationGroupCreated(customizationGroupId2, restaurantId, groupName);

        // Act & Assert
        event1.Should().NotBe(event2);
        event1.Equals(event2).Should().BeFalse();
        (event1 == event2).Should().BeFalse();
        (event1 != event2).Should().BeTrue();
    }

    [Test]
    public void CustomizationChoiceAdded_WithSameProperties_ShouldBeEqual()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var choiceId = ChoiceId.CreateUnique();
        var choiceName = "Large";

        var event1 = new CustomizationChoiceAdded(customizationGroupId, choiceId, choiceName);
        var event2 = new CustomizationChoiceAdded(customizationGroupId, choiceId, choiceName);

        // Act & Assert
        event1.Should().Be(event2);
        event1.Equals(event2).Should().BeTrue();
        (event1 == event2).Should().BeTrue();
        (event1 != event2).Should().BeFalse();
    }

    [Test]
    public void CustomizationChoiceAdded_WithDifferentProperties_ShouldNotBeEqual()
    {
        // Arrange
        var customizationGroupId = CustomizationGroupId.CreateUnique();
        var choiceId1 = ChoiceId.CreateUnique();
        var choiceId2 = ChoiceId.CreateUnique();
        var choiceName = "Large";

        var event1 = new CustomizationChoiceAdded(customizationGroupId, choiceId1, choiceName);
        var event2 = new CustomizationChoiceAdded(customizationGroupId, choiceId2, choiceName);

        // Act & Assert
        event1.Should().NotBe(event2);
        event1.Equals(event2).Should().BeFalse();
        (event1 == event2).Should().BeFalse();
        (event1 != event2).Should().BeTrue();
    }

    #endregion
}
