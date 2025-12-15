using System;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models.Media;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Media.Fakes;

public sealed class FakeMediaStorageService : IMediaStorageService
{
    public Task<Result<MediaUploadResult>> UploadAsync(MediaUploadRequest request, CancellationToken cancellationToken = default)
    {
        var publicId = request.PublicIdHint ?? $"fake/{Guid.NewGuid():N}";
        var url = $"https://cdn.fake.yummyzoom/{publicId}.jpg";
        var result = new MediaUploadResult(publicId, url, Width: 800, Height: 600, Bytes: request.Length ?? 0, Format: "jpg");
        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<MediaDeleteResult>> DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(new MediaDeleteResult(publicId, Deleted: true)));
    }

    public Result<string> BuildUrl(string publicId, MediaTransform? transform = null)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return Result.Failure<string>(Error.Validation("Media.InvalidPublicId", "publicId is required."));
        }

        var url = $"https://cdn.fake.yummyzoom/{publicId}.jpg";
        return Result.Success(url);
    }
}
