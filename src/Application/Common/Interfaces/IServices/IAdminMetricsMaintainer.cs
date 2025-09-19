namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IAdminMetricsMaintainer
{
    Task RecomputeAllAsync(int dailySeriesWindowDays, CancellationToken ct = default);
}
