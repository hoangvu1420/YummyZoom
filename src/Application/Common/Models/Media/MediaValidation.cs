using System;
using System.Collections.Generic;

namespace YummyZoom.Application.Common.Models.Media;

public static class MediaValidation
{
    public const long DefaultMaxBytes = 10 * 1024 * 1024; // 10 MB

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static bool IsAllowedContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType);

    public static bool IsWithinSizeLimit(long? length, long maxBytes = DefaultMaxBytes) =>
        length is null || length <= maxBytes;
}
