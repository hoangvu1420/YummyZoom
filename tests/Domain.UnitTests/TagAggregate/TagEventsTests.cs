using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.TagAggregate.Events;
using YummyZoom.Domain.TagAggregate.ValueObjects;
using YummyZoom.Domain.TagAggregate.Enums;

namespace YummyZoom.Domain.UnitTests.TagAggregate;

[TestFixture]
public class TagEventsTests
{
    #region TagCreated Event Tests

    [Test]
    public void TagCreated_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var tagId = TagId.CreateUnique();
        var tagName = "Vegetarian";
        var tagCategory = TagCategory.Dietary.ToStringValue();

        // Act
        var tagCreatedEvent = new TagCreated(tagId, tagName, tagCategory);

        // Assert
        tagCreatedEvent.TagId.Should().Be(tagId);
        tagCreatedEvent.TagName.Should().Be(tagName);
        tagCreatedEvent.TagCategory.Should().Be(tagCategory);
    }

    #endregion

    #region TagUpdated Event Tests

    [Test]
    public void TagUpdated_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var tagId = TagId.CreateUnique();
        var tagName = "Vegan";
        var tagCategory = TagCategory.Dietary.ToStringValue();

        // Act
        var tagUpdatedEvent = new TagUpdated(tagId, tagName, tagCategory);

        // Assert
        tagUpdatedEvent.TagId.Should().Be(tagId);
        tagUpdatedEvent.TagName.Should().Be(tagName);
        tagUpdatedEvent.TagCategory.Should().Be(tagCategory);
    }

    #endregion
} 
