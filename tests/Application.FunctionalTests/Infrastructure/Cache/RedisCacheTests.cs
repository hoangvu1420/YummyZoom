using Microsoft.Extensions.Caching.Distributed;

namespace YummyZoom.Application.FunctionalTests.Infrastructure.Cache;

[TestFixture]
public class RedisCacheTests
{
    [Test]
    public async Task DistributedCache_SetAndGet_String_Works()
    {
        var cache = Testing.GetService<IDistributedCache>();

        await cache.SetStringAsync("k1", "v1");
        var value = await cache.GetStringAsync("k1");

        value.Should().Be("v1");
    }
}

