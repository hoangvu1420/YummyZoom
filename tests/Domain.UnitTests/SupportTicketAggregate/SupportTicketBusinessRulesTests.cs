using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.Entities;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.Domain.SupportTicketAggregate.Events;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate;

/// <summary>
/// Tests for complex business logic and query methods in the SupportTicket aggregate.
/// </summary>
[TestFixture]
public class SupportTicketBusinessRulesTests
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

    private static SupportTicket CreateDefaultTicket(SupportTicketPriority priority = DefaultPriority)
    {
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            priority,
            CreateDefaultContextLinks(),
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);
        result.IsSuccess.Should().BeTrue();
        result.Value.ClearDomainEvents();
        return result.Value;
    }

    private static SupportTicket CreateTicketForReconstruction(
    DateTimeOffset submissionTimestamp,
    DateTimeOffset? lastUpdateTimestamp = null,
    SupportTicketStatus status = SupportTicketStatus.Open,
    SupportTicketPriority priority = DefaultPriority)
    {
        var ticketId = SupportTicketId.CreateUnique();
        var ticketNumber = TicketNumber.CreateFromSequence(123);
        var contextLinks = CreateDefaultContextLinks();
        var messages = new List<TicketMessage>
        {
            TicketMessage.Create(DefaultAuthorId, DefaultAuthorType, DefaultInitialMessage).Value
        };

        var result = SupportTicket.Create(
            ticketId,
            ticketNumber,
            DefaultSubject,
            DefaultType,
            priority,
            status,
            submissionTimestamp,
            lastUpdateTimestamp ?? submissionTimestamp,
            contextLinks,
            messages);

        result.IsSuccess.Should().BeTrue();
        result.Value.ClearDomainEvents();
        return result.Value;
    }

    #endregion

    #region Time-based Operations Tests

    [Test]
    public void GetAge_ShouldReturnCorrectTimeSpanSinceSubmission()
    {
        // Arrange
        var submissionTime = DateTimeOffset.UtcNow.AddDays(-1);
        var ticket = CreateTicketForReconstruction(submissionTime);

        // Act
        var age = ticket.GetAge();

        // Assert
        age.Should().BeCloseTo(TimeSpan.FromDays(1), TimeSpan.FromSeconds(5));
    }

    [Test]
    public void GetTimeSinceLastUpdate_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var lastUpdateTime = DateTimeOffset.UtcNow.AddHours(-1);
        var ticket = CreateTicketForReconstruction(DateTimeOffset.UtcNow.AddDays(-1), lastUpdateTime);

        // Act
        var timeSinceUpdate = ticket.GetTimeSinceLastUpdate();

        // Assert
        timeSinceUpdate.Should().BeCloseTo(TimeSpan.FromHours(1), TimeSpan.FromSeconds(5));
    }

    [Test]
    public void ShouldAutoEscalate_WhenAgeExceedsMaxAndNotHighPriority_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateTicketForReconstruction(DateTimeOffset.UtcNow.AddHours(-2));
        var maxAge = TimeSpan.FromHours(1);

        // Act
        var result = ticket.ShouldAutoEscalate(maxAge);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsStale_WhenTimeSinceUpdateExceedsMax_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateTicketForReconstruction(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddHours(-2));
        var maxTime = TimeSpan.FromHours(1);

        // Act
        var result = ticket.IsStale(maxTime);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Priority Management Tests

    [Test]
    public void UpdatePriority_WithValidAdminIdAndNewPriority_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var newPriority = SupportTicketPriority.High;

        // Act
        var result = ticket.UpdatePriority(newPriority, adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Priority.Should().Be(newPriority);
        ticket.LastUpdateTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void UpdatePriority_WithValidAdminId_ShouldRaiseDomainEvent()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var newPriority = SupportTicketPriority.High;

        // Act
        ticket.UpdatePriority(newPriority, adminId);

        // Assert
        ticket.DomainEvents.Should().ContainSingle(e => e is SupportTicketPriorityChanged)
            .Which.Should().BeEquivalentTo(new SupportTicketPriorityChanged((SupportTicketId)ticket.Id, DefaultPriority, newPriority, adminId));
    }

    [Test]
    public void UpdatePriority_WithEmptyAdminId_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var newPriority = SupportTicketPriority.High;

        // Act
        var result = ticket.UpdatePriority(newPriority, Guid.Empty);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
    }

    [TestCase(SupportTicketPriority.Low, SupportTicketPriority.Normal, true)]
    [TestCase(SupportTicketPriority.Normal, SupportTicketPriority.High, true)]
    [TestCase(SupportTicketPriority.High, SupportTicketPriority.Urgent, true)]
    [TestCase(SupportTicketPriority.Normal, SupportTicketPriority.Low, false)]
    public void IsValidPriorityEscalation_ShouldReturnCorrectResult(SupportTicketPriority current, SupportTicketPriority next, bool expected)
    {
        // Act
        var result = SupportTicket.IsValidPriorityEscalation(current, next);

        // Assert
        result.Should().Be(expected);
    }

    [TestCase(SupportTicketPriority.High, true)]
    [TestCase(SupportTicketPriority.Urgent, true)]
    [TestCase(SupportTicketPriority.Normal, false)]
    [TestCase(SupportTicketPriority.Low, false)]
    public void IsHighPriority_ShouldReturnCorrectResult(SupportTicketPriority priority, bool expected)
    {
        // Arrange
        var ticket = CreateDefaultTicket(priority);

        // Act
        var result = ticket.IsHighPriority();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Subject Management Tests

    [Test]
    public void UpdateSubject_WithValidSubjectAndAdminId_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var newSubject = "This is an updated subject";

        // Act
        var result = ticket.UpdateSubject(newSubject, adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Subject.Should().Be(newSubject);
        ticket.LastUpdateTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void UpdateSubject_WithEmptySubject_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();

        // Act
        var result = ticket.UpdateSubject("", adminId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidSubject("Subject cannot be empty"));
    }

    [Test]
    public void UpdateSubject_WithSubjectTooLong_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var adminId = Guid.NewGuid();
        var longSubject = new string('A', 201);

        // Act
        var result = ticket.UpdateSubject(longSubject, adminId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidSubject("Subject cannot exceed 200 characters"));
    }

    [Test]
    public void UpdateSubject_WithEmptyAdminId_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var newSubject = "This is an updated subject";

        // Act
        var result = ticket.UpdateSubject(newSubject, Guid.Empty);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
    }

    #endregion
}
