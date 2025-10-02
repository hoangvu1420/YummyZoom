using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using YummyZoom.Domain.RestaurantAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.ValueObjects;

public sealed class BusinessHours : ValueObject
{
    // For simplicity, we'll use a string to represent business hours.
    // This could be expanded to a more complex type if needed.
    public string Hours { get; private set; }

    [JsonConstructor]
    private BusinessHours(string hours)
    {
        Hours = hours;
    }

    public static Result<BusinessHours> Create(string hours)
    {
        const int maxLength = 200;

        // Validate hours format
        if (string.IsNullOrWhiteSpace(hours))
            return Result.Failure<BusinessHours>(RestaurantErrors.BusinessHoursFormatIsRequired());

        if (hours.Length > maxLength)
            return Result.Failure<BusinessHours>(RestaurantErrors.BusinessHoursFormatTooLong(maxLength));

        var trimmedHours = hours.Trim();

        // Validate the format: hh:mm-hh:mm
        if (!IsValidHourFormat(trimmedHours))
            return Result.Failure<BusinessHours>(RestaurantErrors.BusinessHoursInvalidFormat(trimmedHours));

        return Result.Success(new BusinessHours(trimmedHours));
    }

    private static bool IsValidHourFormat(string hours)
    {
        // Pattern: hh:mm-hh:mm (24-hour format)
        // Examples: 09:00-17:30, 08:30-22:15, 00:00-23:59
        var pattern = @"^([01]?[0-9]|2[0-3]):[0-5][0-9]-([01]?[0-9]|2[0-3]):[0-5][0-9]$";

        if (!Regex.IsMatch(hours, pattern))
            return false;

        // Extract start and end times
        var parts = hours.Split('-');
        if (parts.Length != 2)
            return false;

        var startTime = parts[0];
        var endTime = parts[1];

        // Parse and validate times
        if (!TimeSpan.TryParseExact(startTime, @"hh\:mm", null, out var start) ||
            !TimeSpan.TryParseExact(endTime, @"hh\:mm", null, out var end))
            return false;

        // Ensure start time is before end time (same day operation)
        return start < end;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Hours;
    }

#pragma warning disable CS8618
    private BusinessHours() { }
#pragma warning restore CS8618
}
