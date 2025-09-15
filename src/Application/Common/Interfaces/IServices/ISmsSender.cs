namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface ISmsSender
{
    Task SendAsync(string phoneE164, string message, CancellationToken ct = default);
}

