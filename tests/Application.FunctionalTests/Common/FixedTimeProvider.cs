using System;

namespace YummyZoom.Application.FunctionalTests.Common;

public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public void SetUtcNow(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _now;
    }
}

