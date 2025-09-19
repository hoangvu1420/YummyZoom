using System;
namespace YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

public sealed class AdminMetricsMaintenanceOptions
{
    public const string SectionName = "AdminMetricsMaintenance";

    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
    public int DailySeriesWindowDays { get; set; } = 30;
}
