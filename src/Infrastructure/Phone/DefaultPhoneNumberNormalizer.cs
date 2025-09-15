using System.Text.RegularExpressions;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.Phone;

public class DefaultPhoneNumberNormalizer : IPhoneNumberNormalizer
{
    private static readonly Regex Digits = new("[0-9]+", RegexOptions.Compiled);

    // Very small normalizer: if starts with '+', keep only '+' + digits; if 10 digits, assume US (+1).
    public string? Normalize(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone)) return null;
        var s = rawPhone.Trim();

        if (s.StartsWith("+"))
        {
            var digits = string.Concat(s.Skip(1).Where(char.IsDigit));
            if (digits.Length < 8 || digits.Length > 15) return null;
            return "+" + digits;
        }

        var onlyDigits = string.Concat(s.Where(char.IsDigit));
        // Assume US default for 10-digit inputs
        if (onlyDigits.Length == 10)
        {
            return "+1" + onlyDigits;
        }

        // If caller passes 11-15 digits and starts with country code without '+', reject to avoid ambiguity
        return null;
    }
}

