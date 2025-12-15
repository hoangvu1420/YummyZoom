namespace YummyZoom.Infrastructure.Media.Cloudinary;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string? CloudName { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string DefaultFolder { get; init; } = "yummyzoom";
    public bool Secure { get; init; } = true;
    public string? PrivateCdn { get; init; }
    public bool? CdnSubdomain { get; init; }
    public bool Enabled { get; init; } = true;
}
