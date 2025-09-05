using System;

namespace YummyZoom.Infrastructure.ReadModels.FullMenu;

public sealed class MenuReadModelMaintenanceOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;
    public int BackfillBatchSize { get; set; } = 100;
    public int ReconBatchSize { get; set; } = 100;
    public TimeSpan ReconInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxParallelism { get; set; } = 4;
    public bool DeleteOrphans { get; set; } = true;
    public int LogEveryN { get; set; } = 50;
}

