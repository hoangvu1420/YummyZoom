namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding;

public class SeedingConfiguration
{
    public const string SectionName = "Seeding";

    public string Profile { get; set; } = "Development";
    public bool EnableIdempotentSeeding { get; set; } = true;
    public bool ClearExistingData { get; set; } = false;
    public bool SeedTestData { get; set; } = true;

    public Dictionary<string, bool> EnabledSeeders { get; set; } = new();
    public Dictionary<string, object> SeederSettings { get; set; } = new();
}

