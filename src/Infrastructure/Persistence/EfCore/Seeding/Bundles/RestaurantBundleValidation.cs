using System.Text.RegularExpressions;
using YummyZoom.Domain.TagEntity.Enums;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Bundles;

public static class RestaurantBundleValidation
{
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new("^[\\d\\s\\-\\(\\)\\+\\.]+$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled);
    private static readonly Regex HoursRegex = new("^([01]?[0-9]|2[0-3]):[0-5][0-9]-([01]?[0-9]|2[0-3]):[0-5][0-9]$", RegexOptions.Compiled);

    public sealed class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new();
    }

    public static ValidationResult Validate(RestaurantBundle bundle)
    {
        var result = new ValidationResult();
        if (bundle is null)
        {
            result.Errors.Add("Bundle is null.");
            return result;
        }

        // Required: slug, name, cuisineType, address, contact, businessHours, menu
        if (string.IsNullOrWhiteSpace(bundle.RestaurantSlug))
            result.Errors.Add("restaurantSlug is required.");
        else if (!SlugRegex.IsMatch(bundle.RestaurantSlug))
            result.Errors.Add($"restaurantSlug '{bundle.RestaurantSlug}' must be lowercase with hyphens.");

        if (string.IsNullOrWhiteSpace(bundle.Name))
            result.Errors.Add("name is required.");

        if (string.IsNullOrWhiteSpace(bundle.CuisineType))
            result.Errors.Add("cuisineType is required.");

        if (bundle.Address is null)
            result.Errors.Add("address is required.");
        else
        {
            if (string.IsNullOrWhiteSpace(bundle.Address.Street)) result.Errors.Add("address.street is required.");
            if (string.IsNullOrWhiteSpace(bundle.Address.City)) result.Errors.Add("address.city is required.");
            if (string.IsNullOrWhiteSpace(bundle.Address.State)) result.Errors.Add("address.state is required.");
            if (string.IsNullOrWhiteSpace(bundle.Address.ZipCode)) result.Errors.Add("address.zipCode is required.");
            if (string.IsNullOrWhiteSpace(bundle.Address.Country)) result.Errors.Add("address.country is required.");
        }

        if (bundle.Contact is null)
            result.Errors.Add("contact is required.");
        else
        {
            if (string.IsNullOrWhiteSpace(bundle.Contact.Phone))
                result.Errors.Add("contact.phone is required.");
            else if (!PhoneRegex.IsMatch(bundle.Contact.Phone.Trim()) || bundle.Contact.Phone.Trim().Length < 10)
                result.Errors.Add($"contact.phone has invalid format: '{bundle.Contact.Phone}'.");

            if (string.IsNullOrWhiteSpace(bundle.Contact.Email))
                result.Errors.Add("contact.email is required.");
            else if (!EmailRegex.IsMatch(bundle.Contact.Email.Trim()))
                result.Errors.Add($"contact.email has invalid format: '{bundle.Contact.Email}'.");
        }

        if (string.IsNullOrWhiteSpace(bundle.BusinessHours))
            result.Errors.Add("businessHours is required.");
        else if (!HoursRegex.IsMatch(bundle.BusinessHours.Trim()))
            result.Errors.Add($"businessHours must be 'hh:mm-hh:mm' 24h format: '{bundle.BusinessHours}'.");

        // Tags validation (optional)
        if (bundle.Tags is { Count: > 0 })
        {
            var seen = new HashSet<(string Name, string Category)>(StringTupleComparer.OrdinalIgnoreCase);
            foreach (var t in bundle.Tags)
            {
                if (string.IsNullOrWhiteSpace(t.TagName))
                {
                    result.Errors.Add("tags[].tagName is required.");
                    continue;
                }
                if (!TagCategoryExtensions.IsValid(t.TagCategory))
                {
                    var allowed = string.Join(", ", TagCategoryExtensions.GetAllAsStrings());
                    result.Errors.Add($"tags['{t.TagName}'].tagCategory must be one of: {allowed}.");
                }
                var key = (t.TagName.Trim(), (t.TagCategory ?? string.Empty).Trim());
                if (!seen.Add(key))
                {
                    result.Errors.Add($"Duplicate tag name/category pair: '{t.TagName}'/'{t.TagCategory}'.");
                }
            }
        }

        // Customization groups (optional)
        var groupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (bundle.CustomizationGroups is { Count: > 0 })
        {
            foreach (var g in bundle.CustomizationGroups)
            {
                if (string.IsNullOrWhiteSpace(g.GroupKey))
                {
                    result.Errors.Add("customizationGroups[].groupKey is required.");
                    continue;
                }
                if (!groupKeys.Add(g.GroupKey.Trim()))
                {
                    result.Errors.Add($"Duplicate customization groupKey: '{g.GroupKey}'.");
                }
                if (g.MaxSelections < g.MinSelections)
                {
                    result.Errors.Add($"customizationGroups['{g.GroupKey}'] maxSelections must be >= minSelections.");
                }
                // Choices basic validation
                if (g.Choices is null || g.Choices.Count == 0)
                {
                    result.Errors.Add($"customizationGroups['{g.GroupKey}'] must include at least one choice.");
                }
                else
                {
                    var choiceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var c in g.Choices)
                    {
                        if (string.IsNullOrWhiteSpace(c.Name))
                            result.Errors.Add($"customizationGroups['{g.GroupKey}'].choices[].name is required.");
                        if (!choiceNames.Add(c.Name.Trim()))
                            result.Errors.Add($"Duplicate choice name in group '{g.GroupKey}': '{c.Name}'.");
                    }
                }
            }
        }

        // Menu validation
        if (bundle.Menu is null)
        {
            result.Errors.Add("menu is required.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(bundle.Menu.Name))
            result.Errors.Add("menu.name is required.");
        if (string.IsNullOrWhiteSpace(bundle.Menu.Description))
            result.Errors.Add("menu.description is required.");

        var categoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (bundle.Menu.Categories is null || bundle.Menu.Categories.Count == 0)
        {
            result.Errors.Add("menu.categories must include at least one category.");
        }
        else
        {
            foreach (var cat in bundle.Menu.Categories)
            {
                if (string.IsNullOrWhiteSpace(cat.Name))
                {
                    result.Errors.Add("menu.categories[].name is required.");
                    continue;
                }
                if (cat.DisplayOrder <= 0)
                    result.Errors.Add($"menu.categories['{cat.Name}'].displayOrder must be > 0.");
                if (!categoryNames.Add(cat.Name.Trim()))
                    result.Errors.Add($"Duplicate category name: '{cat.Name}'.");

                // Items
                var itemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in cat.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.Name))
                    {
                        result.Errors.Add($"menu.categories['{cat.Name}'].items[].name is required.");
                        continue;
                    }
                    if (!itemNames.Add(item.Name.Trim()))
                        result.Errors.Add($"Duplicate item name in category '{cat.Name}': '{item.Name}'.");
                    if (string.IsNullOrWhiteSpace(item.Description))
                        result.Errors.Add($"menu.categories['{cat.Name}'].items['{item.Name}'].description is required.");
                    if (item.BasePrice <= 0)
                        result.Errors.Add($"menu.categories['{cat.Name}'].items['{item.Name}'].basePrice must be > 0.");

                    // Customization group references must exist if provided
                    if (item.CustomizationGroups is { Count: > 0 })
                    {
                        foreach (var refKey in item.CustomizationGroups)
                        {
                            if (string.IsNullOrWhiteSpace(refKey))
                            {
                                result.Errors.Add($"menu.categories['{cat.Name}'].items['{item.Name}'].customizationGroups contains an empty key.");
                                continue;
                            }
                            if (bundle.CustomizationGroups is null || !groupKeys.Contains(refKey))
                            {
                                result.Errors.Add($"Item '{item.Name}' references missing customization groupKey '{refKey}'.");
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string, string)>
    {
        public static IEqualityComparer<(string, string)> OrdinalIgnoreCase { get; } = new StringTupleComparer();
        public bool Equals((string, string) x, (string, string) y)
            => string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((string, string) obj)
            => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1),
                                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
    }
}

