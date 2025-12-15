using System;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models.Media;
using YummyZoom.SharedKernel;
using DomainError = YummyZoom.SharedKernel.Error;

namespace YummyZoom.Infrastructure.Media.Cloudinary;

public sealed class CloudinaryMediaService : IMediaStorageService
{
    private readonly CloudinaryDotNet.Cloudinary _client;
    private readonly CloudinaryOptions _options;
    private readonly ILogger<CloudinaryMediaService> _logger;

    public CloudinaryMediaService(IOptions<CloudinaryOptions> options, ILogger<CloudinaryMediaService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        _client = new CloudinaryDotNet.Cloudinary(account)
        {
            Api =
            {
                Secure = _options.Secure,
                PrivateCdn = _options.PrivateCdn,
                CSubDomain = _options.CdnSubdomain ?? false
            }
        };
    }

    public async Task<Result<MediaUploadResult>> UploadAsync(MediaUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Content is null) return Result.Failure<MediaUploadResult>(DomainError.Validation("Media.InvalidStream", "Upload stream is required."));

        var folder = CombineFolder(request.Folder);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(request.FileName, request.Content),
            Folder = folder,
            PublicId = request.PublicIdHint,
            UseFilename = string.IsNullOrWhiteSpace(request.PublicIdHint),
            UniqueFilename = string.IsNullOrWhiteSpace(request.PublicIdHint),
            Overwrite = request.Overwrite
        };

        if (request.Tags is { Count: > 0 })
        {
            uploadParams.Tags = string.Join(",", request.Tags.Values);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<MediaUploadResult>(DomainError.Failure("Media.UploadCanceled", "Upload was canceled."));
        }

        var result = await _client.UploadAsync(uploadParams);

        if (result is null || result.Error is not null)
        {
            var message = result?.Error?.Message ?? "Unknown upload error";
            _logger.LogWarning("Cloudinary upload failed for folder {Folder}: {Message}", folder, message);
            return Result.Failure<MediaUploadResult>(DomainError.Failure("Media.UploadFailed", message));
        }

        var payload = new MediaUploadResult(
            result.PublicId,
            result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? string.Empty,
            result.Width,
            result.Height,
            result.Bytes,
            result.Format);

        return Result.Success(payload);
    }

    public async Task<Result<MediaDeleteResult>> DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return Result.Failure<MediaDeleteResult>(DomainError.Validation("Media.InvalidPublicId", "publicId is required."));
        }

        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };

        var result = await _client.DestroyAsync(deletionParams);
        if (result is null || result.Result is null)
        {
            return Result.Failure<MediaDeleteResult>(DomainError.Failure("Media.DeleteFailed", "Delete response was empty."));
        }

        var deleted = string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase);
        if (!deleted && !string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Cloudinary delete for {PublicId} returned {Result}", publicId, result.Result);
        }

        return Result.Success(new MediaDeleteResult(publicId, deleted));
    }

    public Result<string> BuildUrl(string publicId, MediaTransform? transform = null)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return Result.Failure<string>(DomainError.Validation("Media.InvalidPublicId", "publicId is required."));
        }

        var urlBuilder = _client.Api.UrlImgUp.Secure(_options.Secure);

        if (transform is not null)
        {
            var cldTransform = new Transformation();
            if (transform.Width.HasValue) cldTransform = cldTransform.Width(transform.Width.Value);
            if (transform.Height.HasValue) cldTransform = cldTransform.Height(transform.Height.Value);
            if (!string.IsNullOrWhiteSpace(transform.CropMode)) cldTransform = cldTransform.Crop(transform.CropMode);
            if (!string.IsNullOrWhiteSpace(transform.Gravity)) cldTransform = cldTransform.Gravity(transform.Gravity);
            if (transform.AutoQuality) cldTransform = cldTransform.Quality("auto");
            if (transform.AutoFormat) cldTransform = cldTransform.FetchFormat("auto");
            if (transform.AutoDpr) cldTransform = cldTransform.Dpr("auto");
            urlBuilder = urlBuilder.Transform(cldTransform);
        }

        return Result.Success(urlBuilder.BuildUrl(publicId));
    }

    private string CombineFolder(string requestFolder)
    {
        if (string.IsNullOrWhiteSpace(_options.DefaultFolder))
        {
            return requestFolder.Trim('/');
        }

        if (string.IsNullOrWhiteSpace(requestFolder))
        {
            return _options.DefaultFolder.Trim('/');
        }

        return $"{_options.DefaultFolder.Trim('/')}/{requestFolder.Trim('/')}";
    }
}
