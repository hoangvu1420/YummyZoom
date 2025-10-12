namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;

public sealed class RestaurantBundleOptions
{
    public bool ReportOnly { get; set; } = false;
    public bool UpdateDescriptions { get; set; } = false;
    public bool UpdateBasePrices { get; set; } = false;
    public string[] RestaurantGlobs { get; set; } = System.Array.Empty<string>();
}

