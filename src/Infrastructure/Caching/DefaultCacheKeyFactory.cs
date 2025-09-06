using System.Globalization;
using System.Text;
using YummyZoom.Application.Common.Caching;

namespace YummyZoom.Infrastructure.Caching;

public sealed class DefaultCacheKeyFactory : ICacheKeyFactory
{
    private const string Version = "v1";

    public string Create(string @namespace, params object[] parts)
    {
        var sb = new StringBuilder();
        sb.Append(@namespace);
        sb.Append(':');
        sb.Append(Version);

        if (parts is { Length: > 0 })
        {
            foreach (var p in parts)
            {
                sb.Append(':');
                sb.Append(NormalizePart(p));
            }
        }

        return sb.ToString();
    }

    private static string NormalizePart(object part)
    {
        return part switch
        {
            null => "null",
            string s => s.Trim().ToLowerInvariant(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => part.ToString() ?? string.Empty
        };
    }
}

