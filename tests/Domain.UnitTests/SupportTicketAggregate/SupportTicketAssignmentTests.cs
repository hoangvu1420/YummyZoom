using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.Domain.SupportTicketAggregate.Events;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate;

/// <summary>
/// Tests for admin assignment and management in the SupportTicket aggregate.
/// </summary>
[TestFixture]
public class SupportTicketAssignmentTests
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
        return result.Value;
    }

    #endregion

    #region AssignToAdmin() Method Tests

    [Test]
    public void AssignToAdmin_WithValidAdminId_ShouldSucceedAndSetAdminId()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();

        // Act
        var result = ticket.AssignToAdmin(adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.AssignedToAdminId.Should().Be(adminId);
        ticket.LastUpdateTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void AssignToAdmin_WithValidAdminId_ShouldRaiseDomainEvent()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();

        // Act
        var result = ticket.AssignToAdmin(adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvent = ticket.DomainEvents.OfType<SupportTicketAssigned>().Single();
        domainEvent.SupportTicketId.Should().Be((SupportTicketId)ticket.Id);
        domainEvent.AssignedToAdminId.Should().Be(adminId);
        domainEvent.PreviousAdminId.Should().BeNull();
    }

    [Test]
    public void AssignToAdmin_WithEmptyAdminId_ShouldFailWithValidationError()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Act
        var result = ticket.AssignToAdmin(Guid.Empty);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
    }

    [Test]
    public void AssignToAdmin_WhenAlreadyAssigned_ShouldReassignAndRaiseEventWithPreviousAdminId()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var initialAdminId = Guid.NewGuid();
        ticket.AssignToAdmin(initialAdminId); // Initial assignment
        ticket.ClearDomainEvents();
        var newAdminId = Guid.NewGuid();

        // Act
        var result = ticket.AssignToAdmin(newAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.AssignedToAdminId.Should().Be(newAdminId);
        var domainEvent = ticket.DomainEvents.OfType<SupportTicketAssigned>().Single();
        domainEvent.SupportTicketId.Should().Be((SupportTicketId)ticket.Id);
        domainEvent.AssignedToAdminId.Should().Be(newAdminId);
        domainEvent.PreviousAdminId.Should().Be(initialAdminId);
    }

    #endregion

    #region UnassignFromAdmin() Method Tests

    [Test]
    public void UnassignFromAdmin_WhenAssigned_ShouldSucceedAndClearAdminId()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        ticket.AssignToAdmin(adminId);
        ticket.ClearDomainEvents();

        // Act
        var result = ticket.UnassignFromAdmin(adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.AssignedToAdminId.Should().BeNull();
        ticket.LastUpdateTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void UnassignFromAdmin_WhenAssigned_ShouldRaiseDomainEvent()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        ticket.AssignToAdmin(adminId);
        ticket.ClearDomainEvents();

        // Act
        var result = ticket.UnassignFromAdmin(adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvent = ticket.DomainEvents.OfType<SupportTicketAssigned>().Single();
        domainEvent.SupportTicketId.Should().Be((SupportTicketId)ticket.Id);
        domainEvent.AssignedToAdminId.Should().Be(adminId);
        domainEvent.PreviousAdminId.Should().Be(adminId);
    }

    [Test]
    public void UnassignFromAdmin_WithEmptyAdminId_ShouldFailWithValidationError()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        ticket.AssignToAdmin(adminId);

        // Act
        var result = ticket.UnassignFromAdmin(Guid.Empty);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
    }

    [Test]
    public void UnassignFromAdmin_WhenNotAssigned_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();

        // Act
        var result = ticket.UnassignFromAdmin(adminId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.TicketUpdateFailed("Ticket is not currently assigned to any admin"));
    }

    #endregion
}
