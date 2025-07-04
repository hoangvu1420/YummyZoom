using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.SupportTicketAggregate.Entities;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate.Entities;

/// <summary>
/// Tests for TicketMessage entity validation and creation.
/// </summary>
[TestFixture]
public class TicketMessageTests
{
    #region Test Data Helpers

    private static string CreateStringOfLength(int length, char character = 'A')
    {
        return length <= 0 ? string.Empty : new string(character, length);
    }

    private static Guid CreateValidAuthorId() => Guid.NewGuid();

    #endregion

    #region Factory Methods

    [Test]
    public void Create_WithValidInputs_ShouldReturnSuccessResult()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = "This is a valid message";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.AuthorId.Should().Be(authorId);
        result.Value.AuthorType.Should().Be(authorType);
        result.Value.MessageText.Should().Be(messageText);
        result.Value.IsInternalNote.Should().BeFalse();
        result.Value.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Create_WithInternalNote_ShouldReturnSuccessResult()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Admin;
        var messageText = "This is an internal note";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText, isInternalNote: true);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.IsInternalNote.Should().BeTrue();
    }

    [Test]
    public void Create_WithEmptyAuthorId_ShouldReturnFailureResult()
    {
        // Arrange
        var authorId = Guid.Empty;
        var authorType = AuthorType.Customer;
        var messageText = "Valid message";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidAuthorId");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\n")]
    public void Create_WithInvalidMessageText_ShouldReturnFailureResult(string messageText)
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidMessageText");
    }

    [Test]
    public void Create_WithNullMessageText_ShouldReturnFailureResult()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;

        // Act
        var result = TicketMessage.Create(authorId, authorType, null!);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidMessageText");
    }

    [Test]
    public void Create_WithMessageTextExceeding5000Characters_ShouldReturnFailureResult()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = CreateStringOfLength(5001);

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidMessageText");
    }

    [Test]
    public void Create_WithExactly5000Characters_ShouldReturnSuccessResult()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = CreateStringOfLength(5000);

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.MessageText.Should().HaveLength(5000);
    }

    [Test]
    public void Create_WithWhitespaceAroundMessageText_ShouldTrimText()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = "  Valid message with whitespace  ";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.MessageText.Should().Be("Valid message with whitespace");
    }

    [TestCase(AuthorType.Customer)]
    [TestCase(AuthorType.Admin)]
    public void Create_WithDifferentAuthorTypes_ShouldReturnSuccessResult(AuthorType authorType)
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var messageText = "Valid message";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.AuthorType.Should().Be(authorType);
    }

    #endregion

    #region Factory Methods with ID and Timestamp

    [Test]
    public void Create_WithIdAndTimestamp_ShouldReturnSuccessResult()
    {
        // Arrange
        var messageId = MessageId.CreateUnique();
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = "Valid message";
        var timestamp = DateTimeOffset.UtcNow.AddHours(-1);

        // Act
        var result = TicketMessage.Create(messageId, authorId, authorType, messageText, timestamp);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Id.Should().Be(messageId);
        result.Value.Timestamp.Should().Be(timestamp);
    }

    [Test]
    public void Create_WithIdAndTimestamp_WithInternalNote_ShouldReturnSuccessResult()
    {
        // Arrange
        var messageId = MessageId.CreateUnique();
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Admin;
        var messageText = "Internal note";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-30);

        // Act
        var result = TicketMessage.Create(messageId, authorId, authorType, messageText, timestamp, isInternalNote: true);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.IsInternalNote.Should().BeTrue();
        result.Value.Timestamp.Should().Be(timestamp);
    }

    [Test]
    public void Create_WithIdAndTimestamp_WithEmptyAuthorId_ShouldReturnFailureResult()
    {
        // Arrange
        var messageId = MessageId.CreateUnique();
        var authorId = Guid.Empty;
        var authorType = AuthorType.Customer;
        var messageText = "Valid message";
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var result = TicketMessage.Create(messageId, authorId, authorType, messageText, timestamp);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidAuthorId");
    }

    [Test]
    public void Create_WithIdAndTimestamp_WithInvalidMessageText_ShouldReturnFailureResult()
    {
        // Arrange
        var messageId = MessageId.CreateUnique();
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = "";
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var result = TicketMessage.Create(messageId, authorId, authorType, messageText, timestamp);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidMessageText");
    }

    #endregion

    #region Immutability

    [Test]
    public void TicketMessage_ShouldBeImmutable()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = "Original message";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeSuccessful();
        var message = result.Value;

        // Verify all properties are read-only (no public setters)
        // Exclude audit properties as they are set by infrastructure
        var properties = typeof(TicketMessage).GetProperties();
        var auditProperties = new[] { "Created", "CreatedBy", "LastModified", "LastModifiedBy" };
        
        foreach (var property in properties.Where(p => !auditProperties.Contains(p.Name)))
        {
            // Properties should have either no setter or private setter (externally read-only)
            var setMethod = property.GetSetMethod();
            setMethod.Should().BeNull($"Property {property.Name} should not have a public setter");
        }
    }

    [Test]
    public void TicketMessage_ShouldNotHaveUpdateMethods()
    {
        // Arrange & Act
        var updateMethods = typeof(TicketMessage)
            .GetMethods()
            .Where(m => m.Name.StartsWith("Update") || m.Name.StartsWith("Change") || m.Name.StartsWith("Modify"))
            .ToList();

        // Assert
        updateMethods.Should().BeEmpty("TicketMessage should be immutable with no update methods");
    }

    #endregion

    #region Property Encapsulation

    [Test]
    public void Create_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Admin;
        var messageText = "Test message";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText, isInternalNote: true);

        // Assert
        result.ShouldBeSuccessful();
        var message = result.Value;

        message.AuthorId.Should().Be(authorId);
        message.AuthorType.Should().Be(authorType);
        message.MessageText.Should().Be(messageText);
        message.IsInternalNote.Should().BeTrue();
        message.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        message.Id.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Create_WithUnicodeCharacters_ShouldReturnSuccessResult()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = "Message with émojis 🎫 and unicöde chäracters";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.MessageText.Should().Be(messageText);
    }

    [Test]
    public void Create_WithNewlinesAndSpecialCharacters_ShouldReturnSuccessResult()
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = "Message with\nnewlines and\tspecial characters: @#$%^&*()";

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.MessageText.Should().Be(messageText);
    }

    [TestCase(1)]
    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(2500)]
    [TestCase(4999)]
    public void Create_WithVariousValidLengths_ShouldReturnSuccessResult(int length)
    {
        // Arrange
        var authorId = CreateValidAuthorId();
        var authorType = AuthorType.Customer;
        var messageText = CreateStringOfLength(length);

        // Act
        var result = TicketMessage.Create(authorId, authorType, messageText);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.MessageText.Should().HaveLength(length);
    }

    #endregion
}
