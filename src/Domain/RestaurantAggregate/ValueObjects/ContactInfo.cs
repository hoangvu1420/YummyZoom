using System.Text.RegularExpressions;
using YummyZoom.Domain.RestaurantAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.ValueObjects;

public sealed class ContactInfo : ValueObject
{
    public string PhoneNumber { get; private set; }
    public string Email { get; private set; }

    private ContactInfo(string phoneNumber, string email)
    {
        PhoneNumber = phoneNumber;
        Email = email;
    }

    public static Result<ContactInfo> Create(string phoneNumber, string email)
    {
        // Validate phone number
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result.Failure<ContactInfo>(RestaurantErrors.ContactPhoneIsRequired());

        // Basic phone number validation (allows common formats)
        var phonePattern = @"^[\d\s\-\(\)\+\.]+$";
        if (!Regex.IsMatch(phoneNumber.Trim(), phonePattern) || phoneNumber.Trim().Length < 10)
            return Result.Failure<ContactInfo>(RestaurantErrors.ContactPhoneInvalidFormat(phoneNumber));

        // Validate email
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<ContactInfo>(RestaurantErrors.ContactEmailIsRequired());

        // Basic email validation
        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email.Trim(), emailPattern))
            return Result.Failure<ContactInfo>(RestaurantErrors.ContactEmailInvalidFormat(email));

        return Result.Success(new ContactInfo(phoneNumber.Trim(), email.Trim().ToLowerInvariant()));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PhoneNumber;
        yield return Email;
    }

#pragma warning disable CS8618
    private ContactInfo() { }
#pragma warning restore CS8618
}
