namespace YummyZoom.Web.Configuration;

public sealed class FeatureFlagsOptions
{
    public const string SectionName = "Features";

    public bool TeamCart { get; set; } = false;
}

