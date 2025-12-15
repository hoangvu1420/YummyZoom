using YummyZoom.Application.Common.Models.Media;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IMediaStorageService
{
    Task<Result<MediaUploadResult>> UploadAsync(MediaUploadRequest request, CancellationToken cancellationToken = default);

    Task<Result<MediaDeleteResult>> DeleteAsync(string publicId, CancellationToken cancellationToken = default);

    Result<string> BuildUrl(string publicId, MediaTransform? transform = null);
}
