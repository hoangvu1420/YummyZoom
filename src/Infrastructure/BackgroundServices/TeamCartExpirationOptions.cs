using System;

namespace YummyZoom.Infrastructure.BackgroundServices;

public sealed class TeamCartExpirationOptions
{
    public const string SectionName = "TeamCart:Expiration";

    public bool Enabled { get; set; } = true;

    public TimeSpan Cadence { get; set; } = TimeSpan.FromMinutes(1);

    public int BatchSize { get; set; } = 200;

    public TimeSpan GraceWindow { get; set; } = TimeSpan.FromSeconds(60);
}


