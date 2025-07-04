using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.Enums;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate;

/// <summary>
/// Tests for edge cases and validation rules in the SupportTicket aggregate.
/// </summary>
[TestFixture]
public class SupportTicketValidationTests
{
    #region Test Data Helpers

    private static string CreateStringOfLength(int length, char character = 'A')
    {
        return length <= 0 ? string.Empty : new string(character, length);
    }

    #endregion

    #region Static Validation Methods

    [TestCase("Valid subject", true)]
    [TestCase("", false)]
    [TestCase("  ", false)]
    public void IsValidSubjectLength_ShouldReturnCorrectResult(string subject, bool expected)
    {
        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    public void IsValidSubjectLength_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = SupportTicket.IsValidSubjectLength(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidSubjectLength_WithExactly200Characters_ShouldReturnTrue()
    {
        // Arrange
        var subject = CreateStringOfLength(200);

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidSubjectLength_WithOver200Characters_ShouldReturnFalse()
    {
        // Arrange
        var subject = CreateStringOfLength(201);

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().BeFalse();
    }

    [TestCase("Valid message", true)]
    [TestCase("", false)]
    [TestCase("  ", false)]
    public void IsValidMessageLength_ShouldReturnCorrectResult(string message, bool expected)
    {
        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    public void IsValidMessageLength_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = SupportTicket.IsValidMessageLength(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidMessageLength_WithExactly5000Characters_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateStringOfLength(5000);

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidMessageLength_WithOver5000Characters_ShouldReturnFalse()
    {
        // Arrange
        var message = CreateStringOfLength(5001);

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().BeFalse();
    }

    [TestCase(SupportTicketPriority.Low, SupportTicketPriority.Low, true)]
    [TestCase(SupportTicketPriority.Low, SupportTicketPriority.Normal, true)]
    [TestCase(SupportTicketPriority.Low, SupportTicketPriority.High, true)]
    [TestCase(SupportTicketPriority.Low, SupportTicketPriority.Urgent, true)]
    [TestCase(SupportTicketPriority.Normal, SupportTicketPriority.Normal, true)]
    [TestCase(SupportTicketPriority.Normal, SupportTicketPriority.High, true)]
    [TestCase(SupportTicketPriority.Normal, SupportTicketPriority.Urgent, true)]
    [TestCase(SupportTicketPriority.High, SupportTicketPriority.High, true)]
    [TestCase(SupportTicketPriority.High, SupportTicketPriority.Urgent, true)]
    [TestCase(SupportTicketPriority.Urgent, SupportTicketPriority.Urgent, true)]
    [TestCase(SupportTicketPriority.Normal, SupportTicketPriority.Low, false)]
    [TestCase(SupportTicketPriority.High, SupportTicketPriority.Low, false)]
    [TestCase(SupportTicketPriority.High, SupportTicketPriority.Normal, false)]
    [TestCase(SupportTicketPriority.Urgent, SupportTicketPriority.Low, false)]
    [TestCase(SupportTicketPriority.Urgent, SupportTicketPriority.Normal, false)]
    [TestCase(SupportTicketPriority.Urgent, SupportTicketPriority.High, false)]
    public void IsValidPriorityEscalation_ShouldReturnCorrectResult(SupportTicketPriority current, SupportTicketPriority next, bool expected)
    {
        // Act
        var result = SupportTicket.IsValidPriorityEscalation(current, next);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Edge Cases

    [TestCase(1)]
    [TestCase(50)]
    [TestCase(100)]
    [TestCase(199)]
    public void IsValidSubjectLength_WithVariousValidLengths_ShouldReturnTrue(int length)
    {
        // Arrange
        var subject = CreateStringOfLength(length);

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().BeTrue();
    }

    [TestCase(1)]
    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(2500)]
    [TestCase(4999)]
    public void IsValidMessageLength_WithVariousValidLengths_ShouldReturnTrue(int length)
    {
        // Arrange
        var message = CreateStringOfLength(length);

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidSubjectLength_WithWhitespaceOnlyString_ShouldReturnFalse()
    {
        // Arrange
        var subject = new string(' ', 10);

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidMessageLength_WithWhitespaceOnlyString_ShouldReturnFalse()
    {
        // Arrange
        var message = new string(' ', 100);

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidSubjectLength_WithTabsAndNewlines_ShouldReturnFalse()
    {
        // Arrange
        var subject = "\t\n\r  \t";

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidMessageLength_WithTabsAndNewlines_ShouldReturnFalse()
    {
        // Arrange
        var message = "\t\n\r  \t";

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidSubjectLength_WithMixedContent_ShouldReturnTrue()
    {
        // Arrange
        var subject = "Valid subject with numbers 123 and symbols!@#";

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidMessageLength_WithMixedContent_ShouldReturnTrue()
    {
        // Arrange
        var message = "Valid message with numbers 123, symbols!@#, and\nnewlines";

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidPriorityEscalation_WithSamePriority_ShouldReturnTrue()
    {
        // Arrange
        var priority = SupportTicketPriority.Normal;

        // Act
        var result = SupportTicket.IsValidPriorityEscalation(priority, priority);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidPriorityEscalation_WithLowerPriority_ShouldReturnFalse()
    {
        // Act
        var result = SupportTicket.IsValidPriorityEscalation(SupportTicketPriority.High, SupportTicketPriority.Normal);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidPriorityEscalation_WithMultipleLevelJump_ShouldReturnTrue()
    {
        // Act
        var result = SupportTicket.IsValidPriorityEscalation(SupportTicketPriority.Low, SupportTicketPriority.High);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidPriorityEscalation_WithMaxEscalation_ShouldReturnTrue()
    {
        // Act
        var result = SupportTicket.IsValidPriorityEscalation(SupportTicketPriority.High, SupportTicketPriority.Urgent);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidPriorityEscalation_FromUrgentToLower_ShouldReturnFalse()
    {
        // Act & Assert
        SupportTicket.IsValidPriorityEscalation(SupportTicketPriority.Urgent, SupportTicketPriority.Low).Should().BeFalse();
        SupportTicket.IsValidPriorityEscalation(SupportTicketPriority.Urgent, SupportTicketPriority.Normal).Should().BeFalse();
        SupportTicket.IsValidPriorityEscalation(SupportTicketPriority.Urgent, SupportTicketPriority.High).Should().BeFalse();
    }

    [Test]
    public void IsValidPriorityEscalation_FromUrgentToUrgent_ShouldReturnTrue()
    {
        // Act & Assert
        SupportTicket.IsValidPriorityEscalation(SupportTicketPriority.Urgent, SupportTicketPriority.Urgent).Should().BeTrue();
    }

    #endregion

    #region Boundary Value Testing

    [TestCase(200, true)]
    [TestCase(201, false)]
    [TestCase(202, false)]
    [TestCase(300, false)]
    [TestCase(500, false)]
    public void IsValidSubjectLength_BoundaryValues_ShouldReturnCorrectResult(int length, bool expected)
    {
        // Arrange
        var subject = CreateStringOfLength(length);

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().Be(expected);
    }

    [TestCase(5000, true)]
    [TestCase(5001, false)]
    [TestCase(5002, false)]
    [TestCase(6000, false)]
    [TestCase(10000, false)]
    public void IsValidMessageLength_BoundaryValues_ShouldReturnCorrectResult(int length, bool expected)
    {
        // Arrange
        var message = CreateStringOfLength(length);

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Unicode and Special Characters

    [Test]
    public void IsValidSubjectLength_WithUnicodeCharacters_ShouldReturnTrue()
    {
        // Arrange
        var subject = "Support ticket with émojis 🎫 and unicöde chäracters";

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidMessageLength_WithUnicodeCharacters_ShouldReturnTrue()
    {
        // Arrange
        var message = "Message with émojis 🎫, unicöde chäracters, and various symbols: ©®™";

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidSubjectLength_WithUnicodeThatExceedsLimit_ShouldReturnFalse()
    {
        // Arrange - Create a string with 201 unicode characters
        var unicodeChar = "🎫";
        var subject = string.Concat(Enumerable.Repeat(unicodeChar, 201));

        // Act
        var result = SupportTicket.IsValidSubjectLength(subject);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidMessageLength_WithUnicodeThatExceedsLimit_ShouldReturnFalse()
    {
        // Arrange - Create a string with 5001 unicode characters
        var unicodeChar = "🎫";
        var message = string.Concat(Enumerable.Repeat(unicodeChar, 5001));

        // Act
        var result = SupportTicket.IsValidMessageLength(message);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
