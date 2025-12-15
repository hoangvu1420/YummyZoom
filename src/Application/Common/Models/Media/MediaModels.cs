using System;
using System.Collections.Generic;
using System.IO;

namespace YummyZoom.Application.Common.Models.Media;

public sealed record MediaUploadRequest
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required string Folder { get; init; }
    public string? PublicIdHint { get; init; }
    public bool Overwrite { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public string? Scope { get; init; }
    public string? IdempotencyKey { get; init; }
    public long? Length { get; init; }
    public Guid? RestaurantId { get; init; }
    public Guid? MenuItemId { get; init; }
    public Guid? CorrelationId { get; init; }
}

public sealed record MediaUploadResult(
    string PublicId,
    string SecureUrl,
    int Width,
    int Height,
    long Bytes,
    string Format);

public sealed record MediaDeleteResult(string PublicId, bool Deleted);

public sealed record MediaTransform(
    int? Width = null,
    int? Height = null,
    string? CropMode = null,
    string? Gravity = null,
    bool AutoQuality = true,
    bool AutoFormat = true,
    bool AutoDpr = true);
