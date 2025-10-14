namespace YummyZoom.Application.Common.Configuration;

public sealed class ImageProxyOptions
{
    public const string SectionName = "ImageProxy";

    public string[] AllowedHosts { get; set; } = Array.Empty<string>();
    public bool RequireSignatureForNonAllowlisted { get; set; } = true;
    public bool BlockPrivateNetworks { get; set; } = true;
    public bool AllowHttp { get; set; } = false;

    public int MaxBytes { get; set; } = 5_000_000; // 5 MB
    public int TimeoutSeconds { get; set; } = 10;
    public int ConnectTimeoutSeconds { get; set; } = 5;
    public int CacheSeconds { get; set; } = 86400; // 1 day
    public int MaxRedirects { get; set; } = 3;

    public string[] AllowedContentTypes { get; set; } = new[]
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    public string UserAgent { get; set; } = "YummyZoom-ImageProxy/1.0";

    public bool EnableSignature { get; set; } = false; // MVP: off by default
    public string? SignatureSecret { get; set; }
    public string SignatureHash { get; set; } = "SHA256";
}

