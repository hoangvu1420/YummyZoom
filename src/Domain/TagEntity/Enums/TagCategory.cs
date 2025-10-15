namespace YummyZoom.Domain.TagEntity.Enums;

/// <summary>
/// Defines the valid tag categories for the system using strongly-typed enum.
/// This provides compile-time type safety and exhaustiveness checking.
/// </summary>
public enum TagCategory
{
    /// <summary>
    /// Tags related to dietary restrictions and preferences (e.g., Vegetarian, Vegan, Gluten-Free)
    /// </summary>
    Dietary,

    /// <summary>
    /// Tags related to cuisine types and food origins (e.g., Italian, Chinese, Mexican)
    /// </summary>
    Cuisine,

    /// <summary>
    /// Tags related to spice levels and heat intensity (e.g., Mild, Medium, Hot, Extra Hot)
    /// </summary>
    SpiceLevel,

    /// <summary>
    /// Tags related to allergen information (e.g., Contains Nuts, Contains Dairy, Shellfish)
    /// </summary>
    Allergen,

    /// <summary>
    /// Tags related to food preparation methods (e.g., Grilled, Fried, Steamed, Raw)
    /// </summary>
    Preparation,

    /// <summary>
    /// Tags related to serving temperature (e.g., Hot, Cold, Room Temperature)
    /// </summary>
    Temperature,

    /// <summary>
    /// Tags related to meal types and occasions (e.g., Breakfast, Lunch, Dinner, Snack, Dessert)
    /// </summary>
    CookingMethod,

    /// <summary>
    /// Tags related to course types (e.g., Appetizer, Main Course, Dessert)
    /// </summary>
    Course,

    /// <summary>
    /// Tags related to beverage types (e.g., Alcoholic, Non-Alcoholic, Hot Beverage, Cold Beverage)
    /// </summary>
    Beverage,

    /// <summary>
    /// Tags related to portion sizes (e.g., Small, Medium, Large, Family Size)
    /// </summary>
    PortionSize,

    /// <summary>
    /// Tags related to popularity (e.g., Most Popular, Trending, New)
    /// </summary>
    Popularity
}

/// <summary>
/// Extension methods for TagCategory enum to handle string conversion and validation.
/// </summary>
public static class TagCategoryExtensions
{
    /// <summary>
    /// Converts TagCategory enum to its string representation.
    /// </summary>
    public static string ToStringValue(this TagCategory category)
    {
        return category.ToString();
    }

    /// <summary>
    /// Attempts to parse a string to TagCategory enum.
    /// </summary>
    public static bool TryParse(string? value, out TagCategory category)
    {
        return Enum.TryParse<TagCategory>(value, ignoreCase: true, out category);
    }

    /// <summary>
    /// Gets all valid TagCategory values as strings for error messages.
    /// </summary>
    public static string[] GetAllAsStrings()
    {
        return Enum.GetNames<TagCategory>();
    }

    /// <summary>
    /// Validates if a string represents a valid TagCategory.
    /// </summary>
    public static bool IsValid(string? value)
    {
        return TryParse(value, out _);
    }

    /// <summary>
    /// Parses a string to TagCategory enum, throwing exception if invalid.
    /// </summary>
    public static TagCategory Parse(string value)
    {
        return Enum.Parse<TagCategory>(value, ignoreCase: true);
    }
}
