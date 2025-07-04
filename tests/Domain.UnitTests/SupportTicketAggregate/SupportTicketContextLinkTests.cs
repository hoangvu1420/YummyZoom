
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate;

/// <summary>
/// Tests for context link management in the SupportTicket aggregate.
/// </summary>
[TestFixture]
public class SupportTicketContextLinkTests
{
    private static readonly Guid DefaultAuthorId = Guid.NewGuid();
    private const AuthorType DefaultAuthorType = AuthorType.Customer;
    private const string DefaultSubject = "Test support ticket";
    private const SupportTicketType DefaultType = SupportTicketType.GeneralInquiry;
    private const SupportTicketPriority DefaultPriority = SupportTicketPriority.Normal;
    private const string DefaultInitialMessage = "This is a test support ticket message";
    private const int DefaultTicketSequenceNumber = 12345;

    #region Helper Methods

    private static ContextLink CreateDefaultContextLink()
    {
        var result = ContextLink.Create(ContextEntityType.User, Guid.NewGuid());
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static List<ContextLink> CreateDefaultContextLinks()
    {
        return [CreateDefaultContextLink()];
    }

    private static SupportTicket CreateDefaultTicket()
    {
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            CreateDefaultContextLinks(),
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);
        result.IsSuccess.Should().BeTrue();
        result.Value.ClearDomainEvents();
        return result.Value;
    }

    #endregion

    #region AddContextLink() Method Tests

    [Test]
    public void AddContextLink_WithValidLinkAndAdminId_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var newContextLink = ContextLink.Create(ContextEntityType.Order, Guid.NewGuid()).Value;

        // Act
        var result = ticket.AddContextLink(newContextLink, adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.ContextLinks.Should().HaveCount(2);
        ticket.ContextLinks.Should().Contain(newContextLink);
        ticket.LastUpdateTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void AddContextLink_WithDuplicateLink_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var existingContextLink = ticket.ContextLinks.First();

        // Act
        var result = ticket.AddContextLink(existingContextLink, adminId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.TicketUpdateFailed("Context link already exists"));
    }

    [Test]
    public void AddContextLink_WithEmptyAdminId_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var newContextLink = ContextLink.Create(ContextEntityType.Order, Guid.NewGuid()).Value;

        // Act
        var result = ticket.AddContextLink(newContextLink, Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
    }

    #endregion

    #region RemoveContextLink() Method Tests

    [Test]
    public void RemoveContextLink_WithExistingLinkAndAdminId_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var linkToRemove = ticket.ContextLinks.First();
        ticket.AddContextLink(ContextLink.Create(ContextEntityType.Order, Guid.NewGuid()).Value, adminId); // Add a second link so we can remove one
        ticket.ClearDomainEvents();

        // Act
        var result = ticket.RemoveContextLink(linkToRemove.EntityType, linkToRemove.EntityID, adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.ContextLinks.Should().HaveCount(1);
        ticket.ContextLinks.Should().NotContain(linkToRemove);
        ticket.LastUpdateTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void RemoveContextLink_WithNonExistentLink_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var nonExistentLink = ContextLink.Create(ContextEntityType.Order, Guid.NewGuid()).Value;

        // Act
        var result = ticket.RemoveContextLink(nonExistentLink.EntityType, nonExistentLink.EntityID, adminId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.TicketUpdateFailed("Context link not found"));
    }

    [Test]
    public void RemoveContextLink_WhenOnlyOneLinkExists_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var linkToRemove = ticket.ContextLinks.First();

        // Act
        var result = ticket.RemoveContextLink(linkToRemove.EntityType, linkToRemove.EntityID, adminId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.NoContextLinksProvided);
    }

    [Test]
    public void RemoveContextLink_WithEmptyAdminId_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var linkToRemove = ticket.ContextLinks.First();

        // Act
        var result = ticket.RemoveContextLink(linkToRemove.EntityType, linkToRemove.EntityID, Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
    }

    #endregion

    #region Context Query Methods

    [Test]
    public void HasContextLinkForEntity_WithExistingLink_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var existingLink = ticket.ContextLinks.First();

        // Act
        var result = ticket.HasContextLinkForEntity(existingLink.EntityType, existingLink.EntityID);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasContextLinkForEntity_WithNonExistentLink_ShouldReturnFalse()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var nonExistentLink = ContextLink.Create(ContextEntityType.Order, Guid.NewGuid()).Value;

        // Act
        var result = ticket.HasContextLinkForEntity(nonExistentLink.EntityType, nonExistentLink.EntityID);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsRelatedToEntity_WithExistingLink_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var existingLink = ticket.ContextLinks.First();

        // Act
        var result = ticket.IsRelatedToEntity(existingLink.EntityType, existingLink.EntityID);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
