using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.Errors;
using YummyZoom.Domain.ReviewAggregate.Events;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// Tests for core Review aggregate functionality including creation and business operations.
/// </summary>
[TestFixture]
public class ReviewCoreTests
{
    private static readonly OrderId DefaultOrderId = OrderId.CreateUnique();
    private static readonly UserId DefaultCustomerId = UserId.CreateUnique();
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private static readonly Rating DefaultRating = Rating.Create(4).Value;
    private const string DefaultComment = "Great food and service!";
    private const string DefaultReply = "Thank you for your review!";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeReviewCorrectly()
    {
        // Arrange & Act
        var result = Review.Create(
            DefaultOrderId,
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultRating,
            DefaultComment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var review = result.Value;
        review.Id.Value.Should().NotBe(Guid.Empty);
        review.OrderId.Should().Be(DefaultOrderId);
        review.CustomerId.Should().Be(DefaultCustomerId);
        review.RestaurantId.Should().Be(DefaultRestaurantId);
        review.Rating.Should().Be(DefaultRating);
        review.Comment.Should().Be(DefaultComment);
        review.SubmissionTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        review.IsModerated.Should().BeFalse();
        review.IsHidden.Should().BeFalse();
        review.Reply.Should().BeNull();

        // Verify domain event
        review.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(ReviewCreated));
        var reviewCreatedEvent = review.DomainEvents.OfType<ReviewCreated>().Single();
        reviewCreatedEvent.ReviewId.Should().Be((ReviewId)review.Id);
        reviewCreatedEvent.OrderId.Should().Be(DefaultOrderId);
        reviewCreatedEvent.CustomerId.Should().Be(DefaultCustomerId);
        reviewCreatedEvent.RestaurantId.Should().Be(DefaultRestaurantId);
        reviewCreatedEvent.Rating.Should().Be(DefaultRating);
        reviewCreatedEvent.SubmissionTimestamp.Should().Be(review.SubmissionTimestamp);
    }

    [Test]
    public void Create_WithoutComment_ShouldSucceedAndSetCommentToNull()
    {
        // Arrange & Act
        var result = Review.Create(
            DefaultOrderId,
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultRating);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var review = result.Value;
        review.Comment.Should().BeNull();
        review.OrderId.Should().Be(DefaultOrderId);
        review.CustomerId.Should().Be(DefaultCustomerId);
        review.RestaurantId.Should().Be(DefaultRestaurantId);
        review.Rating.Should().Be(DefaultRating);
    }

    [Test]
    public void Create_WithNullOrderId_ShouldFailWithInvalidOrderIdError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = Review.Create(
            null,
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultRating,
            DefaultComment);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.InvalidOrderId);
    }

    [Test]
    public void Create_WithNullCustomerId_ShouldFailWithInvalidCustomerIdError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = Review.Create(
            DefaultOrderId,
            null,
            DefaultRestaurantId,
            DefaultRating,
            DefaultComment);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.InvalidCustomerId);
    }

    [Test]
    public void Create_WithNullRestaurantId_ShouldFailWithInvalidRestaurantIdError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = Review.Create(
            DefaultOrderId,
            DefaultCustomerId,
            null,
            DefaultRating,
            DefaultComment);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.InvalidRestaurantId);
    }

    #endregion

    #region MarkAsModerated() Method Tests

    [Test]
    public void MarkAsModerated_WhenNotModerated_ShouldSucceedAndSetModerationFlag()
    {
        // Arrange
        var review = CreateValidReview();
        review.IsModerated.Should().BeFalse();

        // Act
        var result = review.MarkAsModerated();

        // Assert
        result.IsSuccess.Should().BeTrue();
        review.IsModerated.Should().BeTrue();

        // Verify domain event
        var moderatedEvents = review.DomainEvents.OfType<ReviewModerated>();
        IEnumerable<ReviewModerated> reviewModerateds = moderatedEvents as ReviewModerated[] ?? moderatedEvents.ToArray();
        reviewModerateds.Should().ContainSingle();
        var moderatedEvent = reviewModerateds.Single();
        moderatedEvent.ReviewId.Should().Be((ReviewId)review.Id);
        moderatedEvent.ModeratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void MarkAsModerated_WhenAlreadyModerated_ShouldSucceedAndNotAddDuplicateEvent()
    {
        // Arrange
        var review = CreateValidReview();
        review.MarkAsModerated(); // First moderation
        review.ClearDomainEvents(); // Clear events to test second call

        // Act
        var result = review.MarkAsModerated();

        // Assert
        result.IsSuccess.Should().BeTrue();
        review.IsModerated.Should().BeTrue();
        review.DomainEvents.Should().BeEmpty("because no new event should be added for already moderated review");
    }

    #endregion

    #region Hide() and Show() Method Tests

    [Test]
    public void Hide_ShouldSucceedAndSetHiddenFlag()
    {
        // Arrange
        var review = CreateValidReview();
        review.IsHidden.Should().BeFalse();

        // Act
        var result = review.Hide();

        // Assert
        result.IsSuccess.Should().BeTrue();
        review.IsHidden.Should().BeTrue();
    }

    [Test]
    public void Show_ShouldSucceedAndClearHiddenFlag()
    {
        // Arrange
        var review = CreateValidReview();
        review.Hide(); // First hide it
        review.IsHidden.Should().BeTrue();

        // Act
        var result = review.Show();

        // Assert
        result.IsSuccess.Should().BeTrue();
        review.IsHidden.Should().BeFalse();
    }

    [Test]
    public void Hide_WhenAlreadyHidden_ShouldSucceedAndRemainHidden()
    {
        // Arrange
        var review = CreateValidReview();
        review.Hide();

        // Act
        var result = review.Hide();

        // Assert
        result.IsSuccess.Should().BeTrue();
        review.IsHidden.Should().BeTrue();
    }

    [Test]
    public void Show_WhenAlreadyVisible_ShouldSucceedAndRemainVisible()
    {
        // Arrange
        var review = CreateValidReview();
        review.IsHidden.Should().BeFalse();

        // Act
        var result = review.Show();

        // Assert
        result.IsSuccess.Should().BeTrue();
        review.IsHidden.Should().BeFalse();
    }

    #endregion

    #region AddReply() Method Tests

    [Test]
    public void AddReply_WithValidReply_ShouldSucceedAndSetReply()
    {
        // Arrange
        var review = CreateValidReview();
        review.Reply.Should().BeNull();

        // Act
        var result = review.AddReply(DefaultReply);

        // Assert
        result.IsSuccess.Should().BeTrue();
        review.Reply.Should().Be(DefaultReply);

        // Verify domain event
        var repliedEvents = review.DomainEvents.OfType<ReviewReplied>();
        IEnumerable<ReviewReplied> reviewReplieds = repliedEvents as ReviewReplied[] ?? repliedEvents.ToArray();
        reviewReplieds.Should().ContainSingle();
        var repliedEvent = reviewReplieds.Single();
        repliedEvent.ReviewId.Should().Be((ReviewId)review.Id);
        repliedEvent.Reply.Should().Be(DefaultReply);
        repliedEvent.RepliedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void AddReply_WithNullReply_ShouldFailWithEmptyReplyError()
    {
        // Arrange
        var review = CreateValidReview();

        // Act
#pragma warning disable CS8625
        var result = review.AddReply(null);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.EmptyReply);
        review.Reply.Should().BeNull();
        review.DomainEvents.OfType<ReviewReplied>().Should().BeEmpty();
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t")]
    [TestCase("\n")]
    public void AddReply_WithEmptyOrWhitespaceReply_ShouldFailWithEmptyReplyError(string invalidReply)
    {
        // Arrange
        var review = CreateValidReview();

        // Act
        var result = review.AddReply(invalidReply);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.EmptyReply);
        review.Reply.Should().BeNull();
        review.DomainEvents.OfType<ReviewReplied>().Should().BeEmpty();
    }

    [Test]
    public void AddReply_WhenReviewAlreadyHasReply_ShouldFailWithReviewAlreadyRepliedError()
    {
        // Arrange
        var review = CreateValidReview();
        review.AddReply(DefaultReply); // First reply
        review.ClearDomainEvents(); // Clear events to test second reply attempt

        // Act
        var result = review.AddReply("Second reply attempt");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.ReviewAlreadyReplied);
        review.Reply.Should().Be(DefaultReply, "because the original reply should remain unchanged");
        review.DomainEvents.Should().BeEmpty("because no new event should be added for failed reply attempt");
    }

    #endregion

    #region Helper Methods

    private static Review CreateValidReview()
    {
        var result = Review.Create(
            DefaultOrderId,
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultRating,
            DefaultComment);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    #endregion
}
