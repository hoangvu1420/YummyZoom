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

    #region CouponBundle Options

    /// <summary>
    /// Returns strongly-typed options for the CouponBundle seeder from SeedingConfiguration.SeederSettings.
    /// Falls back to safe defaults when settings are absent or invalid.
    /// </summary>
    public static CouponBundleOptions GetCouponBundleOptions(this SeedingConfiguration config)
    {
        var defaults = new CouponBundleOptions();
        if (config.SeederSettings is null || config.SeederSettings.Count == 0)
            return defaults;

        if (!config.SeederSettings.TryGetValue("CouponBundle", out var raw) || raw is null)
            return defaults;

        try
        {
            return raw switch
            {
                JsonObject jo => FromJsonObjectCoupon(jo, defaults),
                JsonElement je when je.ValueKind == JsonValueKind.Object => FromJsonElementCoupon(je, defaults),
                Dictionary<string, object> dict => FromDictionaryCoupon(dict, defaults),
                string json when LooksLikeJson(json) => FromJsonElementCoupon(JsonDocument.Parse(json).RootElement, defaults),
                _ => defaults
            };
        }
        catch
        {
            return defaults;
        }
    }

    private static CouponBundleOptions FromJsonObjectCoupon(JsonObject jo, CouponBundleOptions defaults)
    {
        return new CouponBundleOptions
        {
            ReportOnly = TryGetBool(jo, "ReportOnly", defaults.ReportOnly),
            OverwriteExisting = TryGetBool(jo, "OverwriteExisting", defaults.OverwriteExisting),
            CouponGlobs = TryGetStringArray(jo, "CouponGlobs", defaults.CouponGlobs)
        };
    }

    private static CouponBundleOptions FromJsonElementCoupon(JsonElement el, CouponBundleOptions defaults)
    {
        return new CouponBundleOptions
        {
            ReportOnly = el.TryGetProperty("ReportOnly", out var r) && TryBool(r, defaults.ReportOnly),
            OverwriteExisting = el.TryGetProperty("OverwriteExisting", out var o) && TryBool(o, defaults.OverwriteExisting),
            CouponGlobs = el.TryGetProperty("CouponGlobs", out var g) ? ToStringArray(g) : defaults.CouponGlobs
        };
    }

    private static CouponBundleOptions FromDictionaryCoupon(Dictionary<string, object> dict, CouponBundleOptions defaults)
    {
        return new CouponBundleOptions
        {
            ReportOnly = TryGetBool(dict, "ReportOnly", defaults.ReportOnly),
            OverwriteExisting = TryGetBool(dict, "OverwriteExisting", defaults.OverwriteExisting),
            CouponGlobs = TryGetStringArray(dict, "CouponGlobs", defaults.CouponGlobs)
        };
    }

    #endregion

    #region OrderSeeding Options

    /// <summary>
    /// Returns strongly-typed options for the Order seeder from SeedingConfiguration.SeederSettings.
    /// Falls back to safe defaults when settings are absent or invalid.
    /// </summary>
    public static OrderSeedingOptions GetOrderSeedingOptions(this SeedingConfiguration config)
    {
        var defaults = new OrderSeedingOptions();
        if (config.SeederSettings is null || config.SeederSettings.Count == 0)
            return defaults;

        if (!config.SeederSettings.TryGetValue("Order", out var raw) || raw is null)
            return defaults;

        try
        {
            return raw switch
            {
                JsonObject jo => FromJsonObjectOrder(jo, defaults),
                JsonElement je when je.ValueKind == JsonValueKind.Object => FromJsonElementOrder(je, defaults),
                Dictionary<string, object> dict => FromDictionaryOrder(dict, defaults),
                string json when LooksLikeJson(json) => FromJsonElementOrder(JsonDocument.Parse(json).RootElement, defaults),
                _ => defaults
            };
        }
        catch
        {
            return defaults;
        }
    }

    private static OrderSeedingOptions FromJsonObjectOrder(JsonObject jo, OrderSeedingOptions defaults)
    {
        return new OrderSeedingOptions
        {
            OrdersPerRestaurant = TryGetInt(jo, "OrdersPerRestaurant", defaults.OrdersPerRestaurant),
            StatusDistribution = TryGetDictionary(jo, "StatusDistribution", defaults.StatusDistribution),
            CouponUsagePercentage = TryGetDecimal(jo, "CouponUsagePercentage", defaults.CouponUsagePercentage),
            OnlinePaymentPercentage = TryGetDecimal(jo, "OnlinePaymentPercentage", defaults.OnlinePaymentPercentage),
            CreateRealisticTimestamps = TryGetBool(jo, "CreateRealisticTimestamps", defaults.CreateRealisticTimestamps),
            OrderHistoryDays = TryGetInt(jo, "OrderHistoryDays", defaults.OrderHistoryDays),
            GenerateSpecialInstructions = TryGetBool(jo, "GenerateSpecialInstructions", defaults.GenerateSpecialInstructions),
            TipPercentage = TryGetDecimal(jo, "TipPercentage", defaults.TipPercentage),
            MinItemsPerOrder = TryGetInt(jo, "MinItemsPerOrder", defaults.MinItemsPerOrder),
            MaxItemsPerOrder = TryGetInt(jo, "MaxItemsPerOrder", defaults.MaxItemsPerOrder)
        };
    }

    private static OrderSeedingOptions FromJsonElementOrder(JsonElement el, OrderSeedingOptions defaults)
    {
        return new OrderSeedingOptions
        {
            OrdersPerRestaurant = el.TryGetProperty("OrdersPerRestaurant", out var o) ? TryInt(o, defaults.OrdersPerRestaurant) : defaults.OrdersPerRestaurant,
            StatusDistribution = el.TryGetProperty("StatusDistribution", out var s) ? TryDictionary(s, defaults.StatusDistribution) : defaults.StatusDistribution,
            CouponUsagePercentage = el.TryGetProperty("CouponUsagePercentage", out var c) ? TryDecimal(c, defaults.CouponUsagePercentage) : defaults.CouponUsagePercentage,
            OnlinePaymentPercentage = el.TryGetProperty("OnlinePaymentPercentage", out var p) ? TryDecimal(p, defaults.OnlinePaymentPercentage) : defaults.OnlinePaymentPercentage,
            CreateRealisticTimestamps = el.TryGetProperty("CreateRealisticTimestamps", out var r) && TryBool(r, defaults.CreateRealisticTimestamps),
            OrderHistoryDays = el.TryGetProperty("OrderHistoryDays", out var h) ? TryInt(h, defaults.OrderHistoryDays) : defaults.OrderHistoryDays,
            GenerateSpecialInstructions = el.TryGetProperty("GenerateSpecialInstructions", out var g) && TryBool(g, defaults.GenerateSpecialInstructions),
            TipPercentage = el.TryGetProperty("TipPercentage", out var t) ? TryDecimal(t, defaults.TipPercentage) : defaults.TipPercentage,
            MinItemsPerOrder = el.TryGetProperty("MinItemsPerOrder", out var min) ? TryInt(min, defaults.MinItemsPerOrder) : defaults.MinItemsPerOrder,
            MaxItemsPerOrder = el.TryGetProperty("MaxItemsPerOrder", out var max) ? TryInt(max, defaults.MaxItemsPerOrder) : defaults.MaxItemsPerOrder
        };
    }

    private static OrderSeedingOptions FromDictionaryOrder(Dictionary<string, object> dict, OrderSeedingOptions defaults)
    {
        return new OrderSeedingOptions
        {
            OrdersPerRestaurant = TryGetInt(dict, "OrdersPerRestaurant", defaults.OrdersPerRestaurant),
            StatusDistribution = TryGetDictionary(dict, "StatusDistribution", defaults.StatusDistribution),
            CouponUsagePercentage = TryGetDecimal(dict, "CouponUsagePercentage", defaults.CouponUsagePercentage),
            OnlinePaymentPercentage = TryGetDecimal(dict, "OnlinePaymentPercentage", defaults.OnlinePaymentPercentage),
            CreateRealisticTimestamps = TryGetBool(dict, "CreateRealisticTimestamps", defaults.CreateRealisticTimestamps),
            OrderHistoryDays = TryGetInt(dict, "OrderHistoryDays", defaults.OrderHistoryDays),
            GenerateSpecialInstructions = TryGetBool(dict, "GenerateSpecialInstructions", defaults.GenerateSpecialInstructions),
            TipPercentage = TryGetDecimal(dict, "TipPercentage", defaults.TipPercentage),
            MinItemsPerOrder = TryGetInt(dict, "MinItemsPerOrder", defaults.MinItemsPerOrder),
            MaxItemsPerOrder = TryGetInt(dict, "MaxItemsPerOrder", defaults.MaxItemsPerOrder)
        };
    }

    #endregion

    #region ReviewSeeding Options

    /// <summary>
    /// Returns strongly-typed options for the Review seeder from SeedingConfiguration.SeederSettings.
    /// Falls back to safe defaults when settings are absent or invalid.
    /// </summary>
    public static ReviewSeedingOptions GetReviewSeedingOptions(this SeedingConfiguration config)
    {
        var defaults = new ReviewSeedingOptions();
        if (config.SeederSettings is null || config.SeederSettings.Count == 0)
            return defaults;

        if (!config.SeederSettings.TryGetValue("Review", out var raw) || raw is null)
            return defaults;

        try
        {
            return raw switch
            {
                JsonObject jo => FromJsonObjectReview(jo, defaults),
                JsonElement je when je.ValueKind == JsonValueKind.Object => FromJsonElementReview(je, defaults),
                Dictionary<string, object> dict => FromDictionaryReview(dict, defaults),
                string json when LooksLikeJson(json) => FromJsonElementReview(JsonDocument.Parse(json).RootElement, defaults),
                _ => defaults
            };
        }
        catch
        {
            return defaults;
        }
    }

    private static ReviewSeedingOptions FromJsonObjectReview(JsonObject jo, ReviewSeedingOptions defaults)
    {
        return new ReviewSeedingOptions
        {
            ReviewPercentage = TryGetDecimal(jo, "ReviewPercentage", defaults.ReviewPercentage),
            ReplyPercentage = TryGetDecimal(jo, "ReplyPercentage", defaults.ReplyPercentage),
            GenerateComments = TryGetBool(jo, "GenerateComments", defaults.GenerateComments),
            PositiveComments = TryGetStringArray(jo, "PositiveComments", defaults.PositiveComments),
            NeutralComments = TryGetStringArray(jo, "NeutralComments", defaults.NeutralComments),
            NegativeComments = TryGetStringArray(jo, "NegativeComments", defaults.NegativeComments),
            ReplyTemplates = TryGetStringArray(jo, "ReplyTemplates", defaults.ReplyTemplates)
        };
    }

    private static ReviewSeedingOptions FromJsonElementReview(JsonElement el, ReviewSeedingOptions defaults)
    {
        return new ReviewSeedingOptions
        {
            ReviewPercentage = el.TryGetProperty("ReviewPercentage", out var r) ? TryDecimal(r, defaults.ReviewPercentage) : defaults.ReviewPercentage,
            ReplyPercentage = el.TryGetProperty("ReplyPercentage", out var rp) ? TryDecimal(rp, defaults.ReplyPercentage) : defaults.ReplyPercentage,
            GenerateComments = el.TryGetProperty("GenerateComments", out var g) && TryBool(g, defaults.GenerateComments),
            PositiveComments = el.TryGetProperty("PositiveComments", out var p) ? ToStringArray(p) : defaults.PositiveComments,
            NeutralComments = el.TryGetProperty("NeutralComments", out var n) ? ToStringArray(n) : defaults.NeutralComments,
            NegativeComments = el.TryGetProperty("NegativeComments", out var neg) ? ToStringArray(neg) : defaults.NegativeComments,
            ReplyTemplates = el.TryGetProperty("ReplyTemplates", out var rt) ? ToStringArray(rt) : defaults.ReplyTemplates
        };
    }

    private static ReviewSeedingOptions FromDictionaryReview(Dictionary<string, object> dict, ReviewSeedingOptions defaults)
    {
        return new ReviewSeedingOptions
        {
            ReviewPercentage = TryGetDecimal(dict, "ReviewPercentage", defaults.ReviewPercentage),
            ReplyPercentage = TryGetDecimal(dict, "ReplyPercentage", defaults.ReplyPercentage),
            GenerateComments = TryGetBool(dict, "GenerateComments", defaults.GenerateComments),
            PositiveComments = TryGetStringArray(dict, "PositiveComments", defaults.PositiveComments),
            NeutralComments = TryGetStringArray(dict, "NeutralComments", defaults.NeutralComments),
            NegativeComments = TryGetStringArray(dict, "NegativeComments", defaults.NegativeComments),
            ReplyTemplates = TryGetStringArray(dict, "ReplyTemplates", defaults.ReplyTemplates)
        };
    }

    #endregion

    #region Helper Methods for New Types

    private static int TryGetInt(JsonObject jo, string key, int fallback)
    {
        if (!jo.TryGetPropertyValue(key, out var node) || node is null)
            return fallback;
        return int.TryParse(node.ToString(), out var val) ? val : fallback;
    }

    private static int TryGetInt(Dictionary<string, object> dict, string key, int fallback)
    {
        if (!dict.TryGetValue(key, out var obj) || obj is null)
            return fallback;
        if (obj is int i) return i;
        return int.TryParse(obj.ToString(), out var val) ? val : fallback;
    }

    private static int TryInt(JsonElement el, int fallback)
    {
        if (el.ValueKind == JsonValueKind.Number)
            return el.GetInt32();
        return int.TryParse(el.ToString(), out var val) ? val : fallback;
    }

    private static decimal TryGetDecimal(JsonObject jo, string key, decimal fallback)
    {
        if (!jo.TryGetPropertyValue(key, out var node) || node is null)
            return fallback;
        return decimal.TryParse(node.ToString(), out var val) ? val : fallback;
    }

    private static decimal TryGetDecimal(Dictionary<string, object> dict, string key, decimal fallback)
    {
        if (!dict.TryGetValue(key, out var obj) || obj is null)
            return fallback;
        if (obj is decimal d) return d;
        if (obj is double dbl) return (decimal)dbl;
        if (obj is float f) return (decimal)f;
        return decimal.TryParse(obj.ToString(), out var val) ? val : fallback;
    }

    private static decimal TryDecimal(JsonElement el, decimal fallback)
    {
        if (el.ValueKind == JsonValueKind.Number)
            return el.GetDecimal();
        return decimal.TryParse(el.ToString(), out var val) ? val : fallback;
    }

    private static Dictionary<string, int> TryGetDictionary(JsonObject jo, string key, Dictionary<string, int> fallback)
    {
        if (!jo.TryGetPropertyValue(key, out var node) || node is null)
            return fallback;

        if (node is JsonObject obj)
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in obj)
            {
                if (kvp.Value != null && int.TryParse(kvp.Value.ToString(), out var intValue))
                {
                    result[kvp.Key] = intValue;
                }
            }
            return result.Count > 0 ? result : fallback;
        }

        return fallback;
    }

    private static Dictionary<string, int> TryGetDictionary(Dictionary<string, object> dict, string key, Dictionary<string, int> fallback)
    {
        if (!dict.TryGetValue(key, out var obj) || obj is null)
            return fallback;

        if (obj is Dictionary<string, int> typedDict)
            return typedDict;

        if (obj is Dictionary<string, object> objDict)
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in objDict)
            {
                if (kvp.Value is int i)
                    result[kvp.Key] = i;
                else if (int.TryParse(kvp.Value?.ToString(), out var intValue))
                    result[kvp.Key] = intValue;
            }
            return result.Count > 0 ? result : fallback;
        }

        return fallback;
    }

    private static Dictionary<string, int> TryDictionary(JsonElement el, Dictionary<string, int> fallback)
    {
        if (el.ValueKind != JsonValueKind.Object)
            return fallback;

        var result = new Dictionary<string, int>();
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Number)
            {
                result[prop.Name] = prop.Value.GetInt32();
            }
            else if (int.TryParse(prop.Value.ToString(), out var intValue))
            {
                result[prop.Name] = intValue;
            }
        }

        return result.Count > 0 ? result : fallback;
    }

    #endregion
}
