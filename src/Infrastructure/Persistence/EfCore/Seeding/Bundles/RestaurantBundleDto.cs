using System.Text.Json.Serialization;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Bundles;

public sealed class RestaurantBundle
{
    [JsonPropertyName("restaurantSlug")] public string RestaurantSlug { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("cuisineType")] public string CuisineType { get; set; } = string.Empty;
    [JsonPropertyName("logoUrl")] public string? LogoUrl { get; set; }
    [JsonPropertyName("backgroundImageUrl")] public string? BackgroundImageUrl { get; set; }
    [JsonPropertyName("defaultCurrency")] public string? DefaultCurrency { get; set; }

    [JsonPropertyName("address")] public AddressDto Address { get; set; } = new();
    [JsonPropertyName("contact")] public ContactDto Contact { get; set; } = new();
    [JsonPropertyName("businessHours")] public string BusinessHours { get; set; } = string.Empty;

    [JsonPropertyName("isVerified")] public bool IsVerified { get; set; } = true;
    [JsonPropertyName("isAcceptingOrders")] public bool IsAcceptingOrders { get; set; } = true;

    [JsonPropertyName("tags")] public List<TagDto>? Tags { get; set; }
    [JsonPropertyName("customizationGroups")] public List<CustomizationGroupDto>? CustomizationGroups { get; set; }

    [JsonPropertyName("menu")] public MenuDto Menu { get; set; } = new();
}

public sealed class AddressDto
{
    [JsonPropertyName("street")] public string Street { get; set; } = string.Empty;
    [JsonPropertyName("city")] public string City { get; set; } = string.Empty;
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    [JsonPropertyName("zipCode")] public string ZipCode { get; set; } = string.Empty;
    [JsonPropertyName("country")] public string Country { get; set; } = string.Empty;
}

public sealed class ContactDto
{
    [JsonPropertyName("phone")] public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
}

public sealed class TagDto
{
    [JsonPropertyName("tagName")] public string TagName { get; set; } = string.Empty;
    [JsonPropertyName("tagCategory")] public string TagCategory { get; set; } = string.Empty;
    [JsonPropertyName("tagDescription")] public string? TagDescription { get; set; }
}

public sealed class CustomizationGroupDto
{
    // Stable key that maps to domain GroupName for now
    [JsonPropertyName("groupKey")] public string GroupKey { get; set; } = string.Empty;
    [JsonPropertyName("minSelections")] public int MinSelections { get; set; }
    [JsonPropertyName("maxSelections")] public int MaxSelections { get; set; }
    [JsonPropertyName("choices")] public List<CustomizationChoiceDto> Choices { get; set; } = new();
}

public sealed class CustomizationChoiceDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("priceAdjustment")] public decimal PriceAdjustment { get; set; }
    [JsonPropertyName("isDefault")] public bool IsDefault { get; set; }
    [JsonPropertyName("displayOrder")] public int? DisplayOrder { get; set; }
}

public sealed class MenuDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("categories")] public List<MenuCategoryDto> Categories { get; set; } = new();
}

public sealed class MenuCategoryDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("displayOrder")] public int DisplayOrder { get; set; }
    [JsonPropertyName("items")] public List<MenuItemDto> Items { get; set; } = new();
}

public sealed class MenuItemDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("basePrice")] public decimal BasePrice { get; set; }
    [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
    [JsonPropertyName("isAvailable")] public bool IsAvailable { get; set; } = true;

    // Names of dietary tags (Dietary category), case-insensitive match during seeding
    [JsonPropertyName("dietaryTags")] public List<string>? DietaryTags { get; set; }

    // References to CustomizationGroupDto.GroupKey
    [JsonPropertyName("customizationGroups")] public List<string>? CustomizationGroups { get; set; }
}
