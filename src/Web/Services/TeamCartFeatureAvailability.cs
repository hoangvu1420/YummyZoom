using Microsoft.Extensions.Options;
using YummyZoom.Web.Configuration;

namespace YummyZoom.Web.Services;

public interface ITeamCartFeatureAvailability
{
    bool Enabled { get; }
    bool RealTimeReady { get; }
}

internal sealed class TeamCartFeatureAvailability : ITeamCartFeatureAvailability
{
    private readonly FeatureFlagsOptions _features;
    private readonly IConfiguration _configuration;

    public TeamCartFeatureAvailability(IOptions<FeatureFlagsOptions> features, IConfiguration configuration)
    {
        _features = features.Value;
        _configuration = configuration;
    }

    public bool Enabled => _features.TeamCart;

    // Consider real-time ready when a Redis connection is configured.
    public bool RealTimeReady
    {
        get
        {
            var redis = _configuration.GetConnectionString("redis")
                        ?? _configuration["Cache:Redis:ConnectionString"];
            return !string.IsNullOrWhiteSpace(redis);
        }
    }
}

