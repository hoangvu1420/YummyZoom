using System.Text.Json;
using System.Text.Json.Nodes;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding;

public static class SeedingConfigurationExtensions
{
    /// <summary>
    /// Returns strongly-typed options for the RestaurantBundle seeder from SeedingConfiguration.SeederSettings.
    /// Falls back to safe defaults when settings are absent or invalid.
    /// </summary>
    public static RestaurantBundleOptions GetRestaurantBundleOptions(this SeedingConfiguration config)
    {
        var defaults = new RestaurantBundleOptions();
        if (config.SeederSettings is null || config.SeederSettings.Count == 0)
            return defaults;

        if (!config.SeederSettings.TryGetValue("RestaurantBundle", out var raw) || raw is null)
            return defaults;

        try
        {
            switch (raw)
            {
                case JsonObject jo:
                    return FromJsonObject(jo, defaults);
                case JsonElement je when je.ValueKind == JsonValueKind.Object:
                    return FromJsonElement(je, defaults);
                case Dictionary<string, object> dict:
                    return FromDictionary(dict, defaults);
                case string json when LooksLikeJson(json):
                    var doc = JsonDocument.Parse(json);
                    return FromJsonElement(doc.RootElement, defaults);
                default:
                    return defaults;
            }
        }
        catch
        {
            return defaults;
        }
    }

    private static RestaurantBundleOptions FromJsonObject(JsonObject jo, RestaurantBundleOptions defaults)
    {
        var opts = new RestaurantBundleOptions
        {
            ReportOnly = TryGetBool(jo, "ReportOnly", defaults.ReportOnly),
            UpdateDescriptions = TryGetBool(jo, "UpdateDescriptions", defaults.UpdateDescriptions),
            UpdateBasePrices = TryGetBool(jo, "UpdateBasePrices", defaults.UpdateBasePrices),
            RestaurantGlobs = TryGetStringArray(jo, "RestaurantGlobs", defaults.RestaurantGlobs)
        };
        return opts;
    }

    private static RestaurantBundleOptions FromJsonElement(JsonElement el, RestaurantBundleOptions defaults)
    {
        var opts = new RestaurantBundleOptions
        {
            ReportOnly = el.TryGetProperty("ReportOnly", out var r) && TryBool(r, defaults.ReportOnly),
            UpdateDescriptions = el.TryGetProperty("UpdateDescriptions", out var d) && TryBool(d, defaults.UpdateDescriptions),
            UpdateBasePrices = el.TryGetProperty("UpdateBasePrices", out var p) && TryBool(p, defaults.UpdateBasePrices),
            RestaurantGlobs = el.TryGetProperty("RestaurantGlobs", out var g) ? ToStringArray(g) : defaults.RestaurantGlobs
        };
        return opts;
    }

    private static RestaurantBundleOptions FromDictionary(Dictionary<string, object> dict, RestaurantBundleOptions defaults)
    {
        var opts = new RestaurantBundleOptions
        {
            ReportOnly = TryGetBool(dict, "ReportOnly", defaults.ReportOnly),
            UpdateDescriptions = TryGetBool(dict, "UpdateDescriptions", defaults.UpdateDescriptions),
            UpdateBasePrices = TryGetBool(dict, "UpdateBasePrices", defaults.UpdateBasePrices),
            RestaurantGlobs = TryGetStringArray(dict, "RestaurantGlobs", defaults.RestaurantGlobs)
        };
        return opts;
    }

    private static bool TryGetBool(JsonObject jo, string key, bool fallback)
        => jo.TryGetPropertyValue(key, out var node) && node is not null &&
           bool.TryParse(node.ToString(), out var val) ? val : fallback;

    private static bool TryBool(JsonElement el, bool fallback)
        => el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False
            ? el.GetBoolean()
            : (bool.TryParse(el.ToString(), out var b) ? b : fallback);

    private static string[] TryGetStringArray(JsonObject jo, string key, string[] fallback)
    {
        if (!jo.TryGetPropertyValue(key, out var node) || node is null)
            return fallback;

        if (node is JsonArray arr)
        {
            return arr.Select(n => n?.ToString() ?? string.Empty)
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .ToArray();
        }

        var asString = node.ToString();
        return SplitList(asString, fallback);
    }

    private static string[] TryGetStringArray(Dictionary<string, object> dict, string key, string[] fallback)
    {
        if (!dict.TryGetValue(key, out var obj) || obj is null)
            return fallback;

        switch (obj)
        {
            case IEnumerable<object> list:
                return list.Select(x => x?.ToString() ?? string.Empty)
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .ToArray();
            case string s:
                return SplitList(s, fallback);
            default:
                return fallback;
        }
    }

    private static bool TryGetBool(Dictionary<string, object> dict, string key, bool fallback)
    {
        if (!dict.TryGetValue(key, out var obj) || obj is null)
            return fallback;
        if (obj is bool b) return b;
        return bool.TryParse(obj.ToString(), out var val) ? val : fallback;
    }

    private static string[] ToStringArray(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    list.Add(item.GetString()!);
                else
                    list.Add(item.ToString());
            }
            return list.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        }
        return SplitList(el.ToString(), System.Array.Empty<string>());
    }

    private static string[] SplitList(string? s, string[] fallback)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        var parts = s.Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                     .Select(p => p.Trim())
                     .Where(p => p.Length > 0)
                     .ToArray();
        return parts.Length > 0 ? parts : fallback;
    }

    private static bool LooksLikeJson(string s)
    {
        s = s.Trim();
        return (s.StartsWith("{") && s.EndsWith("}"));
    }
}
