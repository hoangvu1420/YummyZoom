namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IPhoneNumberNormalizer
{
    // Returns E.164 formatted phone number (e.g., +15551234567) or null if invalid.
    string? Normalize(string? rawPhone);
}

