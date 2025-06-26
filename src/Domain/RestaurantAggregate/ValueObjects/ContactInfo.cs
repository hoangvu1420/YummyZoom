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

    public static ContactInfo Create(string phoneNumber, string email)
    {
        return new ContactInfo(phoneNumber, email);
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
