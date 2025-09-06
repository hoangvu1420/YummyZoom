using System.Threading;
using System.Threading.Tasks;

namespace YummyZoom.Application.Common.Caching;

/// <summary>
/// Serialization abstraction for cache payloads.
/// Distributed implementations typically use UTF8 JSON bytes; memory cache may store objects directly.
/// </summary>
public interface ICacheSerializer
{
    byte[] Serialize<T>(T value);
    T? Deserialize<T>(byte[] payload);
}

