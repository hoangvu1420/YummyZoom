using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Bundles;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Seeders.RestaurantSeeders;

public class RestaurantBundleSeeder : ISeeder
{
    public string Name => "RestaurantBundle";
    public int Order => 112; // After Tags (105) and Restaurants (100) if still enabled

    public Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Always attempt; idempotency inside
        return Task.FromResult(true);
    }

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var logger = context.Logger;
        var opts = context.Configuration.GetRestaurantBundleOptions();

        var bundles = await LoadBundlesAsync(logger, opts, cancellationToken);
        if (bundles.Count == 0)
        {
            logger.LogInformation("No restaurant bundle files found.");
            return Result.Success();
        }

        // Preload tag lookups (Dietary only; others created as needed)
        var tags = await context.DbContext.Tags.AsNoTracking().ToListAsync(cancellationToken);
        var tagLookup = tags.GroupBy(t => (t.TagName, t.TagCategory), new TupleComparer())
                            .ToDictionary(g => g.Key, g => g.First().Id, new TupleComparer());

        var created = new Counters();
        var updated = new Counters();
        var skipped = new Counters();

        foreach (var (fileName, bundle) in bundles)
        {
            // Determine currency for this bundle
            var currency = string.IsNullOrWhiteSpace(bundle.DefaultCurrency)
                ? "USD"
                : bundle.DefaultCurrency!.Trim().ToUpperInvariant();

            // Validate
            var val = RestaurantBundleValidation.Validate(bundle);
            if (!val.IsValid)
            {
                foreach (var err in val.Errors)
                    logger.LogWarning("Bundle {File}: {Error}", fileName, err);
                continue;
            }

            // Upsert Tags from bundle
            if (bundle.Tags is { Count: > 0 })
            {
                foreach (var t in bundle.Tags)
                {
                    if (!TagCategoryExtensions.TryParse(t.TagCategory, out var cat))
                    {
                        logger.LogWarning("Invalid tag category in {File}: {Name}/{Category}", fileName, t.TagName, t.TagCategory);
                        continue;
                    }
                    var key = (t.TagName.Trim(), cat);
                    if (!tagLookup.ContainsKey(key))
                    {
                        if (opts.ReportOnly)
                        {
                            created.Tags++;
                        }
                        else
                        {
                            var createdTag = Tag.Create(t.TagName, cat, t.TagDescription);
                            if (createdTag.IsSuccess)
                            {
                                createdTag.Value.ClearDomainEvents();
                                context.DbContext.Tags.Add(createdTag.Value);
                                tagLookup[key] = createdTag.Value.Id;
                                created.Tags++;
                            }
                            else
                            {
                                logger.LogWarning("Failed to create tag {Name}: {Error}", t.TagName, createdTag.Error.Description);
                                skipped.Tags++;
                            }
                        }
                    }
                }
            }

            // Find or create Restaurant by Name
            var restaurant = await context.DbContext.Restaurants
                .FirstOrDefaultAsync(r => r.Name == bundle.Name, cancellationToken);
            if (restaurant is null)
            {
                if (opts.ReportOnly)
                {
                    created.Restaurants++;
                }
                else
                {
                    var res = Restaurant.Create(
                        bundle.Name,
                        bundle.LogoUrl,
                        bundle.BackgroundImageUrl,
                        bundle.Description ?? string.Empty,
                        bundle.CuisineType,
                        bundle.Address.Street,
                        bundle.Address.City,
                        bundle.Address.State,
                        bundle.Address.ZipCode,
                        bundle.Address.Country,
                        bundle.Contact.Phone,
                        bundle.Contact.Email,
                        bundle.BusinessHours,
                        bundle.Latitude,
                        bundle.Longitude);

                    if (res.IsFailure)
                    {
                        logger.LogWarning("Failed to create restaurant from {File}: {Error}", fileName, res.Error.Description);
                        skipped.Restaurants++;
                        continue;
                    }
                    if (bundle.IsVerified) res.Value.Verify();
                    if (bundle.IsAcceptingOrders) res.Value.AcceptOrders();
                    res.Value.ClearDomainEvents();
                    context.DbContext.Restaurants.Add(res.Value);
                    restaurant = res.Value;
                    created.Restaurants++;
                }
            }
            else
            {
                // Optionally update geo coordinates if provided
                if (!opts.ReportOnly && bundle.Latitude.HasValue && bundle.Longitude.HasValue)
                {
                    var needUpdate = restaurant.GeoCoordinates is null
                        || Math.Abs(restaurant.GeoCoordinates.Latitude - bundle.Latitude.Value) > 0.0000005
                        || Math.Abs(restaurant.GeoCoordinates.Longitude - bundle.Longitude.Value) > 0.0000005;
                    if (needUpdate)
                    {
                        var geoRes = restaurant.ChangeGeoCoordinates(bundle.Latitude.Value, bundle.Longitude.Value);
                        if (geoRes.IsFailure)
                        {
                            logger.LogWarning("Failed to update geo coordinates for {Restaurant}: {Error}", restaurant.Name, geoRes.Error.Description);
                        }
                    }
                }

                skipped.Restaurants++;
            }

            // Store slug-to-ID mapping in SharedData for other seeders (e.g., CouponBundleSeeder)
            if (restaurant is not null && !string.IsNullOrWhiteSpace(bundle.RestaurantSlug))
            {
                var slugMapKey = "RestaurantSlugMap";
                if (!context.SharedData.ContainsKey(slugMapKey))
                {
                    context.SharedData[slugMapKey] = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
                }
                var slugMap = (Dictionary<string, Guid>)context.SharedData[slugMapKey];
                slugMap[bundle.RestaurantSlug] = restaurant.Id.Value;
            }

            // Menu
            Menu? menu = null;
            if (!opts.ReportOnly)
            {
                menu = await context.DbContext.Menus.FirstOrDefaultAsync(m => m.RestaurantId == restaurant!.Id && m.Name == bundle.Menu.Name, cancellationToken);
            }
            if (menu is null)
            {
                if (opts.ReportOnly)
                {
                    created.Menus++;
                }
                else
                {
                    var createMenu = Menu.Create(restaurant!.Id, bundle.Menu.Name, bundle.Menu.Description, isEnabled: true);
                    if (createMenu.IsFailure)
                    {
                        logger.LogWarning("Failed to create menu for {Restaurant}: {Error}", restaurant!.Name, createMenu.Error.Description);
                        skipped.Menus++;
                        continue;
                    }
                    createMenu.Value.ClearDomainEvents();
                    context.DbContext.Menus.Add(createMenu.Value);
                    menu = createMenu.Value;
                    created.Menus++;
                }
            }
            else if (opts.UpdateDescriptions && !opts.ReportOnly)
            {
                var upd = menu.UpdateDetails(bundle.Menu.Name, bundle.Menu.Description);
                if (upd.IsSuccess) updated.Menus++;
            }

            // Categories
            var categoryMap = new Dictionary<string, MenuCategoryId>(StringComparer.OrdinalIgnoreCase);
            foreach (var cat in bundle.Menu.Categories.OrderBy(c => c.DisplayOrder))
            {
                MenuCategory? existingCat = null;
                if (!opts.ReportOnly)
                {
                    existingCat = await context.DbContext.MenuCategories
                        .FirstOrDefaultAsync(c => c.MenuId == menu!.Id && c.Name == cat.Name, cancellationToken);
                }

                if (existingCat is null)
                {
                    if (opts.ReportOnly)
                    {
                        created.Categories++;
                    }
                    else
                    {
                        var createdCat = MenuCategory.Create(menu!.Id, cat.Name, cat.DisplayOrder);
                        if (createdCat.IsFailure)
                        {
                            logger.LogWarning("Failed to create category {Category} for menu {Menu}", cat.Name, menu!.Name);
                            skipped.Categories++;
                            continue;
                        }
                        createdCat.Value.ClearDomainEvents();
                        context.DbContext.MenuCategories.Add(createdCat.Value);
                        categoryMap[cat.Name] = createdCat.Value.Id;
                        created.Categories++;
                    }
                }
                else
                {
                    categoryMap[existingCat.Name] = existingCat.Id;
                    if (existingCat.DisplayOrder != cat.DisplayOrder && !opts.ReportOnly)
                    {
                        var upd = existingCat.UpdateDisplayOrder(cat.DisplayOrder);
                        if (upd.IsSuccess) updated.Categories++;
                    }
                }
            }

            // Customization groups (upsert per restaurant)
            var groupIdByKey = new Dictionary<string, YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects.CustomizationGroupId>(StringComparer.OrdinalIgnoreCase);
            if (bundle.CustomizationGroups is { Count: > 0 })
            {
                // Load current groups for this restaurant
                var existingGroups = !opts.ReportOnly
                    ? await context.DbContext.CustomizationGroups.Where(g => g.RestaurantId == restaurant!.Id).ToListAsync(cancellationToken)
                    : new List<CustomizationGroup>();

                foreach (var g in bundle.CustomizationGroups)
                {
                    var ex = existingGroups.FirstOrDefault(x => x.GroupName.Equals(g.GroupKey, StringComparison.OrdinalIgnoreCase));
                    if (ex is null)
                    {
                        if (opts.ReportOnly)
                        {
                            created.Groups++;
                        }
                        else
                        {
                            var cg = CustomizationGroup.Create(restaurant!.Id, g.GroupKey, g.MinSelections, g.MaxSelections);
                            if (cg.IsFailure)
                            {
                                logger.LogWarning("Failed to create customization group {Group} for {Restaurant}", g.GroupKey, restaurant!.Name);
                                skipped.Groups++;
                                continue;
                            }
                            // Add choices
                            var order = 1;
                            foreach (var ch in g.Choices)
                            {
                                var display = ch.DisplayOrder ?? order;
                                var add = cg.Value.AddChoice(ch.Name, new Money(ch.PriceAdjustment, currency), ch.IsDefault, display);
                                if (add.IsFailure)
                                    logger.LogWarning("Failed to add choice {Choice} to group {Group}: {Error}", ch.Name, g.GroupKey, add.Error.Description);
                                order++;
                            }
                            cg.Value.ClearDomainEvents();
                            context.DbContext.CustomizationGroups.Add(cg.Value);
                            groupIdByKey[g.GroupKey] = cg.Value.Id;
                            created.Groups++;
                        }
                    }
                    else
                    {
                        groupIdByKey[g.GroupKey] = ex.Id;
                        // Ensure choices exist (create-missing only)
                        if (!opts.ReportOnly)
                        {
                            var existingChoiceNames = ex.Choices.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                            var order = ex.Choices.Any() ? ex.Choices.Max(c => c.DisplayOrder) + 1 : 1;
                            foreach (var ch in g.Choices)
                            {
                                if (existingChoiceNames.Contains(ch.Name)) continue;
                                var add = ex.AddChoice(ch.Name, new Money(ch.PriceAdjustment, currency), ch.IsDefault, ch.DisplayOrder ?? order);
                                if (add.IsSuccess) updated.Groups++; else logger.LogWarning("Failed to add missing choice {Choice} to existing group {Group}: {Error}", ch.Name, g.GroupKey, add.Error.Description);
                                order++;
                            }
                        }
                    }
                }
            }

            // Items
            // Preload all tags lookup by name (not just dietary - the JSON field name is misleading)
            var allTagLookup = tagLookup.ToDictionary(kv => kv.Key.Item1, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            foreach (var cat in bundle.Menu.Categories)
            {
                if (!categoryMap.TryGetValue(cat.Name, out var categoryId))
                    continue;

                // Existing items in this category by name
                var existingItemNames = !opts.ReportOnly
                    ? await context.DbContext.MenuItems.Where(i => i.MenuCategoryId == categoryId).Select(i => i.Name).ToListAsync(cancellationToken)
                    : new List<string>();

                foreach (var it in cat.Items)
                {
                    var exists = existingItemNames.Contains(it.Name);
                    if (!exists)
                    {
                        if (opts.ReportOnly)
                        {
                            created.Items++;
                        }
                        else
                        {
                            var price = new Money(it.BasePrice, currency);
                            var mi = MenuItem.Create(restaurant!.Id, categoryId, it.Name, it.Description, price, it.ImageUrl, it.IsAvailable);
                            if (mi.IsFailure)
                            {
                                logger.LogWarning("Failed to create item {Item} in category {Category}: {Error}", it.Name, cat.Name, mi.Error.Description);
                                skipped.Items++;
                                continue;
                            }

                            // Dietary tags
                            if (it.DietaryTags is { Count: > 0 })
                            {
                                var tagIds = new List<YummyZoom.Domain.TagEntity.ValueObjects.TagId>();
                                foreach (var tname in it.DietaryTags)
                                {
                                    if (allTagLookup.TryGetValue(tname, out var id))
                                        tagIds.Add(id);
                                    else
                                        logger.LogWarning("Tag not found: {TagName}. Define it globally or in bundle tags.", tname);
                                }
                                if (tagIds.Count > 0) mi.Value.SetDietaryTags(tagIds);
                            }

                            // Customization groups
                            if (it.CustomizationGroups is { Count: > 0 })
                            {
                                var order = 1;
                                foreach (var key in it.CustomizationGroups)
                                {
                                    if (!groupIdByKey.TryGetValue(key, out var gid))
                                    {
                                        logger.LogWarning("Missing customization group for item {Item}: {Group}", it.Name, key);
                                        continue;
                                    }
                                    var applied = AppliedCustomization.Create(gid, key, order);
                                    var assign = mi.Value.AssignCustomizationGroup(applied);
                                    if (assign.IsFailure)
                                        logger.LogWarning("Failed to assign group {Group} to item {Item}: {Error}", key, it.Name, assign.Error.Description);
                                    order++;
                                }
                            }

                            mi.Value.ClearDomainEvents();
                            context.DbContext.MenuItems.Add(mi.Value);
                            created.Items++;
                        }
                    }
                    else if (!opts.ReportOnly)
                    {
                        // Optional updates
                        var existing = await context.DbContext.MenuItems.FirstAsync(i => i.MenuCategoryId == categoryId && i.Name == it.Name, cancellationToken);
                        if (opts.UpdateDescriptions)
                        {
                            var upd = existing.UpdateDetails(it.Name, it.Description);
                            if (upd.IsSuccess) updated.Items++;
                        }
                        if (opts.UpdateBasePrices)
                        {
                            var upd = existing.UpdatePrice(new Money(it.BasePrice, existing.GetCurrency()));
                            if (upd.IsSuccess) updated.Items++;
                        }
                    }
                }
            }

            if (!opts.ReportOnly)
            {
                await context.DbContext.SaveChangesAsync(cancellationToken);
            }

            context.Logger.LogInformation("[{File}] Created R:{R} M:{M} C:{C} G:{G} I:{I} | Updated M:{UM} C:{UC} G:{UG} I:{UI} | Skipped R:{SR} M:{SM} C:{SC} I:{SI}",
                fileName, created.Restaurants, created.Menus, created.Categories, created.Groups, created.Items,
                updated.Menus, updated.Categories, updated.Groups, updated.Items,
                skipped.Restaurants, skipped.Menus, skipped.Categories, skipped.Items);
        }

        return Result.Success();
    }

    private static async Task<List<(string fileName, RestaurantBundle bundle)>> LoadBundlesAsync(ILogger logger, RestaurantBundleOptions opts, CancellationToken ct)
    {
        var list = new List<(string, RestaurantBundle)>();
        var asm = typeof(RestaurantBundleSeeder).Assembly;
        var names = asm.GetManifestResourceNames()
            .Where(n => n.EndsWith(".restaurant.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (opts.RestaurantGlobs is { Length: > 0 })
        {
            names = names.Where(n => opts.RestaurantGlobs.Any(glob => GlobMatch(Path.GetFileName(n), glob))).ToArray();
        }

        foreach (var resName in names)
        {
            try
            {
                await using var stream = asm.GetManifestResourceStream(resName)!;
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var bundle = JsonSerializer.Deserialize<RestaurantBundle>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (bundle is not null)
                {
                    var shortName = Path.GetFileName(resName);
                    list.Add((shortName, bundle));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read bundle resource: {Name}", resName);
            }
        }
        return list;
    }

    private static bool GlobMatch(string input, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return true;
        // Support simple * wildcard only
        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(input ?? string.Empty, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private sealed class Counters
    {
        public int Restaurants; public int Menus; public int Categories; public int Groups; public int Items; public int Tags;
    }

    private sealed class TupleComparer : IEqualityComparer<(string, TagCategory)>
    {
        public bool Equals((string, TagCategory) x, (string, TagCategory) y)
            => string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) && x.Item2 == y.Item2;
        public int GetHashCode((string, TagCategory) obj)
            => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1), obj.Item2.GetHashCode());
    }
}
