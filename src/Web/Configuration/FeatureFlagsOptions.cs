namespace YummyZoom.Web.Configuration;

public sealed class FeatureFlagsOptions
{
    public const string SectionName = "Features";

    public bool TeamCart { get; set; } = false;

    // Dev/Test only: enable order flow simulation endpoint
    public bool OrderFlowSimulation { get; set; } = false;

    // Dev/Test only: enable team cart flow simulation endpoints
    public bool TeamCartFlowSimulation { get; set; } = false;
}
