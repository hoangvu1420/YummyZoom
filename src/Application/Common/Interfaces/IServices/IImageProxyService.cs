using YummyZoom.Application.Common.Configuration;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IImageProxyService
{
    Task<Result<ProxiedImage>> GetAsync(Uri url, CancellationToken cancellationToken = default);
}

public sealed record ProxiedImage(
    Stream Content,
    string ContentType,
    long? ContentLength,
    string? ETag,
    DateTimeOffset? LastModified);

