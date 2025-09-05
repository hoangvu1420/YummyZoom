namespace YummyZoom.Infrastructure.Data.ReadModels.Reviews;

public sealed class ReviewSummaryMaintenanceOptions
{
    public bool Enabled { get; set; } = false;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;
    public TimeSpan ReconInterval { get; set; } = TimeSpan.FromMinutes(15);
}

