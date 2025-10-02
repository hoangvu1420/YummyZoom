using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Domain.TagEntity.Errors;
using YummyZoom.Domain.TagEntity.Events;

namespace YummyZoom.Domain.UnitTests.TagEntity;

[TestFixture]
public class TagTests
{
    private const string DefaultTagName = "Vegetarian";
    private const string DefaultTagDescription = "Contains no meat or animal products";
    private static readonly TagCategory DefaultTagCategory = TagCategory.Dietary;
    private static readonly string LongTagName = new('a', 101); // 101 characters

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeTagCorrectly()
    {
        // Arrange & Act
        var result = Tag.Create(DefaultTagName, DefaultTagCategory, DefaultTagDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tag = result.Value;
        tag.Id.Value.Should().NotBe(Guid.Empty);
        tag.TagName.Should().Be(DefaultTagName);
        tag.TagCategory.Should().Be(DefaultTagCategory);
        tag.TagDescription.Should().Be(DefaultTagDescription);
        tag.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TagCreated));

        var tagCreatedEvent = tag.DomainEvents.OfType<TagCreated>().Single();
        tagCreatedEvent.TagId.Should().Be(tag.Id);
        tagCreatedEvent.TagName.Should().Be(DefaultTagName);
        tagCreatedEvent.TagCategory.Should().Be(DefaultTagCategory.ToStringValue());
    }

    [Test]
    public void Create_WithNullDescription_ShouldSucceedAndSetDescriptionToNull()
    {
        // Arrange & Act
        var result = Tag.Create(DefaultTagName, DefaultTagCategory, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tag = result.Value;
        tag.TagName.Should().Be(DefaultTagName);
        tag.TagCategory.Should().Be(DefaultTagCategory);
        tag.TagDescription.Should().BeNull();
        tag.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TagCreated));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithNullOrEmptyTagName_ShouldFailWithNameIsRequiredError(string invalidName)
    {
        // Arrange & Act
        var result = Tag.Create(invalidName, DefaultTagCategory, DefaultTagDescription);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TagErrors.NameIsRequired);
    }

    [Test]
    public void Create_WithNullTagName_ShouldFailWithNameIsRequiredError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = Tag.Create(null, DefaultTagCategory, DefaultTagDescription);
#pragma warning restore CS8625

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TagErrors.NameIsRequired);
    }

    [Test]
    public void Create_WithTagNameTooLong_ShouldFailWithNameTooLongError()
    {
        // Arrange & Act
        var result = Tag.Create(LongTagName, DefaultTagCategory, DefaultTagDescription);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TagErrors.NameTooLong);
    }

    [TestCase(TagCategory.Dietary)]
    [TestCase(TagCategory.Cuisine)]
    [TestCase(TagCategory.SpiceLevel)]
    [TestCase(TagCategory.Allergen)]
    [TestCase(TagCategory.Preparation)]
    [TestCase(TagCategory.Temperature)]
    public void Create_WithAllValidCategories_ShouldSucceed(TagCategory validCategory)
    {
        // Arrange & Act
        var result = Tag.Create(DefaultTagName, validCategory, DefaultTagDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TagCategory.Should().Be(validCategory);
    }

    #endregion

    #region UpdateDetails() Method Tests

    [Test]
    public void UpdateDetails_WithValidInputs_ShouldSucceedAndUpdateProperties()
    {
        // Arrange
        var tag = CreateValidTag();
        var newName = "Vegan";
        var newDescription = "Contains no animal products whatsoever";

        // Act
        var result = tag.UpdateDetails(newName, newDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tag.TagName.Should().Be(newName);
        tag.TagDescription.Should().Be(newDescription);
        tag.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TagUpdated));

        var tagUpdatedEvent = tag.DomainEvents.OfType<TagUpdated>().Single();
        tagUpdatedEvent.TagId.Should().Be(tag.Id);
        tagUpdatedEvent.TagName.Should().Be(newName);
        tagUpdatedEvent.TagCategory.Should().Be(tag.TagCategory.ToStringValue());
    }

    [Test]
    public void UpdateDetails_WithNullDescription_ShouldSucceedAndSetDescriptionToNull()
    {
        // Arrange
        var tag = CreateValidTag();
        var newName = "Gluten-Free";

        // Act
        var result = tag.UpdateDetails(newName, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tag.TagName.Should().Be(newName);
        tag.TagDescription.Should().BeNull();
        tag.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TagUpdated));
    }

    [Test]
    public void UpdateDetails_WithSameName_ShouldSucceedButNotRaiseEvent()
    {
        // Arrange
        var tag = CreateValidTag();
        var originalEventCount = tag.DomainEvents.Count;
        var newDescription = "Updated description";

        // Act
        var result = tag.UpdateDetails(DefaultTagName, newDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tag.TagName.Should().Be(DefaultTagName);
        tag.TagDescription.Should().Be(newDescription);
        tag.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised
    }

    [TestCase("")]
    [TestCase("   ")]
    public void UpdateDetails_WithNullOrEmptyTagName_ShouldFailWithNameIsRequiredError(string invalidName)
    {
        // Arrange
        var tag = CreateValidTag();
        var originalName = tag.TagName;
        var originalDescription = tag.TagDescription;
        var originalEventCount = tag.DomainEvents.Count;

        // Act
        var result = tag.UpdateDetails(invalidName, "New description");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TagErrors.NameIsRequired);
        tag.TagName.Should().Be(originalName); // State unchanged
        tag.TagDescription.Should().Be(originalDescription); // State unchanged
        tag.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised
    }

    [Test]
    public void UpdateDetails_WithNullTagName_ShouldFailWithNameIsRequiredError()
    {
        // Arrange
        var tag = CreateValidTag();
        var originalName = tag.TagName;
        var originalDescription = tag.TagDescription;
        var originalEventCount = tag.DomainEvents.Count;

        // Act
#pragma warning disable CS8625
        var result = tag.UpdateDetails(null, "New description");
#pragma warning restore CS8625

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TagErrors.NameIsRequired);
        tag.TagName.Should().Be(originalName); // State unchanged
        tag.TagDescription.Should().Be(originalDescription); // State unchanged
        tag.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised
    }

    [Test]
    public void UpdateDetails_WithTagNameTooLong_ShouldFailWithNameTooLongError()
    {
        // Arrange
        var tag = CreateValidTag();
        var originalName = tag.TagName;
        var originalDescription = tag.TagDescription;
        var originalEventCount = tag.DomainEvents.Count;

        // Act
        var result = tag.UpdateDetails(LongTagName, "New description");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TagErrors.NameTooLong);
        tag.TagName.Should().Be(originalName); // State unchanged
        tag.TagDescription.Should().Be(originalDescription); // State unchanged
        tag.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised
    }

    #endregion

    #region ChangeCategory() Method Tests

    [Test]
    public void ChangeCategory_WithValidCategory_ShouldSucceedAndUpdateCategory()
    {
        // Arrange
        var tag = CreateValidTag();
        var newCategory = TagCategory.Cuisine;

        // Act
        var result = tag.ChangeCategory(newCategory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tag.TagCategory.Should().Be(newCategory);

        // Should raise only TagCategoryChanged event (plus the initial TagCreated)
        tag.DomainEvents.Should().HaveCount(2);
        tag.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TagCategoryChanged));

        var tagCategoryChangedEvent = tag.DomainEvents.OfType<TagCategoryChanged>().Single();
        tagCategoryChangedEvent.TagId.Should().Be(tag.Id);
        tagCategoryChangedEvent.OldCategory.Should().Be(DefaultTagCategory.ToStringValue());
        tagCategoryChangedEvent.NewCategory.Should().Be(newCategory.ToStringValue());
    }

    [Test]
    public void ChangeCategory_WithSameCategory_ShouldSucceedButNotRaiseEvent()
    {
        // Arrange
        var tag = CreateValidTag();
        var originalEventCount = tag.DomainEvents.Count;

        // Act
        var result = tag.ChangeCategory(DefaultTagCategory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tag.TagCategory.Should().Be(DefaultTagCategory);
        tag.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised
    }

    #endregion

    #region Helper Methods

    private static Tag CreateValidTag(string? name = null, TagCategory? category = null, string? description = null)
    {
        var result = Tag.Create(
            name ?? DefaultTagName,
            category ?? DefaultTagCategory,
            description ?? DefaultTagDescription);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    #endregion
}
