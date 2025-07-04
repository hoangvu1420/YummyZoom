using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.Domain.SupportTicketAggregate.Events;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate;

/// <summary>
/// Tests for SupportTicket status transitions and status-related business rules.
/// </summary>
[TestFixture]
public class SupportTicketStatusTests
{
    private static readonly Guid DefaultAdminId = Guid.NewGuid();
    private static readonly Guid DefaultAuthorId = Guid.NewGuid();

    #region Helper Methods

    private static SupportTicket CreateDefaultTicket(SupportTicketStatus status = SupportTicketStatus.Open)
    {
        var contextLinks = new List<ContextLink>
        {
            ContextLink.Create(ContextEntityType.User, Guid.NewGuid()).Value
        };

        var result = SupportTicket.Create(
            "Test Ticket",
            SupportTicketType.GeneralInquiry,
            SupportTicketPriority.Normal,
            contextLinks,
            "Initial message",
            DefaultAuthorId,
            AuthorType.Customer,
            12345);

        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;

        // Clear domain events from creation
        ticket.ClearDomainEvents();

        // Set status if different from Open
        if (status != SupportTicketStatus.Open)
        {
            SetTicketToStatus(ticket, status);
        }

        return ticket;
    }

    private static void SetTicketToStatus(SupportTicket ticket, SupportTicketStatus targetStatus)
    {
        // Follow valid status transitions to reach target status
        switch (targetStatus)
        {
            case SupportTicketStatus.InProgress:
                ticket.UpdateStatus(SupportTicketStatus.InProgress, DefaultAdminId);
                break;
            
            case SupportTicketStatus.PendingCustomerResponse:
                ticket.UpdateStatus(SupportTicketStatus.InProgress, DefaultAdminId);
                ticket.UpdateStatus(SupportTicketStatus.PendingCustomerResponse, DefaultAdminId);
                break;
            
            case SupportTicketStatus.Resolved:
                ticket.UpdateStatus(SupportTicketStatus.InProgress, DefaultAdminId);
                ticket.UpdateStatus(SupportTicketStatus.Resolved, DefaultAdminId);
                break;
            
            case SupportTicketStatus.Closed:
                ticket.UpdateStatus(SupportTicketStatus.Closed, DefaultAdminId);
                break;
        }
        
        ticket.ClearDomainEvents();
    }

    #endregion

    #region UpdateStatus() Tests - Valid Transitions

    [Test]
    public void UpdateStatus_FromOpenToInProgress_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.InProgress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.InProgress);
        ticket.LastUpdateTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void UpdateStatus_FromOpenToClosed_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Closed, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.Closed);
    }

    [Test]
    public void UpdateStatus_FromInProgressToPendingCustomerResponse_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.PendingCustomerResponse);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.PendingCustomerResponse);
    }

    [Test]
    public void UpdateStatus_FromInProgressToResolved_ShouldSucceedWithAdmin()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Resolved, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.Resolved);
    }

    [Test]
    public void UpdateStatus_FromInProgressToClosed_ShouldSucceedWithAdmin()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Closed, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.Closed);
    }

    [Test]
    public void UpdateStatus_FromPendingCustomerResponseToInProgress_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.InProgress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.InProgress);
    }

    [Test]
    public void UpdateStatus_FromPendingCustomerResponseToClosed_ShouldSucceedWithAdmin()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Closed, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.Closed);
    }

    [Test]
    public void UpdateStatus_FromResolvedToClosed_ShouldSucceedWithAdmin()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Resolved);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Closed, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.Closed);
    }

    [Test]
    public void UpdateStatus_FromResolvedToInProgress_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Resolved);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.InProgress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.InProgress);
    }

    #endregion

    #region UpdateStatus() Tests - Invalid Transitions

    [Test]
    public void UpdateStatus_FromOpenToResolved_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Resolved, DefaultAdminId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.InvalidStatusTransition("Open", "Resolved"));
    }

    [Test]
    public void UpdateStatus_FromOpenToPendingCustomerResponse_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.PendingCustomerResponse);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.InvalidStatusTransition("Open", "PendingCustomerResponse"));
    }

    [Test]
    public void UpdateStatus_FromInProgressToOpen_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Open);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.InvalidStatusTransition("InProgress", "Open"));
    }

    [Test]
    public void UpdateStatus_FromClosedToAnyOtherStatus_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Closed);

        // Act & Assert
        var resultToOpen = ticket.UpdateStatus(SupportTicketStatus.Open);
        resultToOpen.IsFailure.Should().BeTrue();
        resultToOpen.Error.Should().Be(SupportTicketErrors.InvalidStatusTransition("Closed", "Open"));

        var resultToInProgress = ticket.UpdateStatus(SupportTicketStatus.InProgress);
        resultToInProgress.IsFailure.Should().BeTrue();
        resultToInProgress.Error.Should().Be(SupportTicketErrors.InvalidStatusTransition("Closed", "InProgress"));

        var resultToResolved = ticket.UpdateStatus(SupportTicketStatus.Resolved, DefaultAdminId);
        resultToResolved.IsFailure.Should().BeTrue();
        resultToResolved.Error.Should().Be(SupportTicketErrors.InvalidStatusTransition("Closed", "Resolved"));
    }

    [Test]
    public void UpdateStatus_FromPendingCustomerResponseToResolved_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Resolved, DefaultAdminId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.InvalidStatusTransition("PendingCustomerResponse", "Resolved"));
    }

    #endregion

    #region UpdateStatus() Tests - Admin Authorization

    [Test]
    public void UpdateStatus_ToResolvedWithoutAdmin_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Resolved);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.UnauthorizedStatusChange("Resolved"));
    }

    [Test]
    public void UpdateStatus_ToClosedWithoutAdmin_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Closed);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.UnauthorizedStatusChange("Closed"));
    }

    [Test]
    public void UpdateStatus_ToInProgressWithoutAdmin_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.InProgress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.InProgress);
    }

    [Test]
    public void UpdateStatus_ToPendingCustomerResponseWithoutAdmin_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.PendingCustomerResponse);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.PendingCustomerResponse);
    }

    #endregion

    #region UpdateStatus() Tests - Business Rules

    [Test]
    public void UpdateStatus_WithSameStatus_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.InProgress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportTicketErrors.TicketUpdateFailed("Ticket is already in 'InProgress' status"));
    }

    [Test]
    public void UpdateStatus_ToInProgressWithoutAssignment_ShouldAutoAssignAdmin()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);
        ticket.AssignedToAdminId.Should().BeNull("because new tickets are not assigned");

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.InProgress, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.InProgress);
        ticket.AssignedToAdminId.Should().Be(DefaultAdminId, "because admin should be auto-assigned when moving to InProgress");
    }

    [Test]
    public void UpdateStatus_ToInProgressWithExistingAssignment_ShouldNotChangeAssignment()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);
        var originalAdminId = Guid.NewGuid();
        ticket.AssignToAdmin(originalAdminId);
        ticket.ClearDomainEvents();

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.InProgress, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.InProgress);
        ticket.AssignedToAdminId.Should().Be(originalAdminId, "because existing assignment should be preserved");
    }

    #endregion

    #region UpdateStatus() Tests - Domain Events

    [Test]
    public void UpdateStatus_ShouldRaiseDomainEvent()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.InProgress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.DomainEvents.Should().HaveCount(1);

        var statusEvent = ticket.DomainEvents.OfType<SupportTicketStatusChanged>().Single();
        statusEvent.SupportTicketId.Should().Be(ticket.Id);
        statusEvent.PreviousStatus.Should().Be(SupportTicketStatus.Open);
        statusEvent.NewStatus.Should().Be(SupportTicketStatus.InProgress);
        statusEvent.ChangedByAdminId.Should().BeNull();
    }

    [Test]
    public void UpdateStatus_WithAdminId_ShouldIncludeAdminInEvent()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.UpdateStatus(SupportTicketStatus.Resolved, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.DomainEvents.Should().HaveCount(1);

        var statusEvent = ticket.DomainEvents.OfType<SupportTicketStatusChanged>().Single();
        statusEvent.ChangedByAdminId.Should().Be(DefaultAdminId);
    }

    #endregion

    #region Status Query Methods Tests

    [Test]
    public void IsOpen_WithOpenStatus_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Open);

        // Act & Assert
        ticket.IsOpen().Should().BeTrue();
        ticket.IsClosed().Should().BeFalse();
        ticket.IsResolved().Should().BeFalse();
        ticket.RequiresCustomerResponse().Should().BeFalse();
    }

    [Test]
    public void IsClosed_WithClosedStatus_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Closed);

        // Act & Assert
        ticket.IsClosed().Should().BeTrue();
        ticket.IsOpen().Should().BeFalse();
        ticket.IsResolved().Should().BeFalse();
        ticket.RequiresCustomerResponse().Should().BeFalse();
    }

    [Test]
    public void IsResolved_WithResolvedStatus_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Resolved);

        // Act & Assert
        ticket.IsResolved().Should().BeTrue();
        ticket.IsOpen().Should().BeFalse();
        ticket.IsClosed().Should().BeFalse();
        ticket.RequiresCustomerResponse().Should().BeFalse();
    }

    [Test]
    public void RequiresCustomerResponse_WithPendingCustomerResponseStatus_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);

        // Act & Assert
        ticket.RequiresCustomerResponse().Should().BeTrue();
        ticket.IsOpen().Should().BeFalse();
        ticket.IsClosed().Should().BeFalse();
        ticket.IsResolved().Should().BeFalse();
    }

    [Test]
    public void IsInFinalState_WithClosedStatus_ShouldReturnTrue()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Closed);

        // Act & Assert
        ticket.IsInFinalState().Should().BeTrue();
    }

    [Test]
    public void IsInFinalState_WithNonClosedStatus_ShouldReturnFalse()
    {
        // Arrange & Act & Assert
        var openTicket = CreateDefaultTicket(SupportTicketStatus.Open);
        openTicket.IsInFinalState().Should().BeFalse();

        var inProgressTicket = CreateDefaultTicket(SupportTicketStatus.InProgress);
        inProgressTicket.IsInFinalState().Should().BeFalse();

        var pendingTicket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);
        pendingTicket.IsInFinalState().Should().BeFalse();

        var resolvedTicket = CreateDefaultTicket(SupportTicketStatus.Resolved);
        resolvedTicket.IsInFinalState().Should().BeFalse();
    }

    [Test]
    public void CanChangeStatus_ForNonClosedTickets_ShouldReturnTrue()
    {
        // Arrange & Act & Assert
        var openTicket = CreateDefaultTicket(SupportTicketStatus.Open);
        openTicket.CanChangeStatus().Should().BeTrue();

        var inProgressTicket = CreateDefaultTicket(SupportTicketStatus.InProgress);
        inProgressTicket.CanChangeStatus().Should().BeTrue();

        var pendingTicket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);
        pendingTicket.CanChangeStatus().Should().BeTrue();

        var resolvedTicket = CreateDefaultTicket(SupportTicketStatus.Resolved);
        resolvedTicket.CanChangeStatus().Should().BeTrue();
    }

    [Test]
    public void CanChangeStatus_ForClosedTicket_ShouldReturnFalse()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Closed);

        // Act & Assert
        ticket.CanChangeStatus().Should().BeFalse();
    }

    [Test]
    public void CanCustomerRespond_ForNonClosedNonResolvedTickets_ShouldReturnTrue()
    {
        // Arrange & Act & Assert
        var openTicket = CreateDefaultTicket(SupportTicketStatus.Open);
        openTicket.CanCustomerRespond().Should().BeTrue();

        var inProgressTicket = CreateDefaultTicket(SupportTicketStatus.InProgress);
        inProgressTicket.CanCustomerRespond().Should().BeTrue();

        var pendingTicket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);
        pendingTicket.CanCustomerRespond().Should().BeTrue();
    }

    [Test]
    public void CanCustomerRespond_ForClosedOrResolvedTickets_ShouldReturnFalse()
    {
        // Arrange & Act & Assert
        var closedTicket = CreateDefaultTicket(SupportTicketStatus.Closed);
        closedTicket.CanCustomerRespond().Should().BeFalse();

        var resolvedTicket = CreateDefaultTicket(SupportTicketStatus.Resolved);
        resolvedTicket.CanCustomerRespond().Should().BeFalse();
    }

    #endregion
}
