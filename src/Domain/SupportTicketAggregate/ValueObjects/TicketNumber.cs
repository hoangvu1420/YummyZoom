using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

public sealed class TicketNumber : ValueObject
{
    public string Value { get; private set; }

    private TicketNumber(string value)
    {
        Value = value;
    }

    public static Result<TicketNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TicketNumber>(SupportTicketErrors.InvalidTicketNumber("Ticket number cannot be empty"));
        }

        if (value.Length > 50)
        {
            return Result.Failure<TicketNumber>(SupportTicketErrors.InvalidTicketNumber("Ticket number cannot exceed 50 characters"));
        }

        return Result.Success(new TicketNumber(value));
    }

    public static TicketNumber CreateFromSequence(int sequenceNumber)
    {
        var year = DateTime.UtcNow.Year;
        var ticketNumber = $"TKT-{year}-{sequenceNumber:D6}";
        return new TicketNumber(ticketNumber);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private TicketNumber()
    {
    }
#pragma warning restore CS8618
}
