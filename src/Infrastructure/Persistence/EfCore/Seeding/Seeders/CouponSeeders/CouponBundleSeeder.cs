using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Bundles;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Seeders.CouponSeeders;

/// <summary>
/// Seeds coupons from JSON bundle files located in Data/Coupons directory.
/// </summary>
public class CouponBundleSeeder : ISeeder
{
    public string Name => "CouponBundle";
    public int Order => 115; // After RestaurantBundle (112), before Orders (120)

    public Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Always attempt; idempotency is handled inside SeedAsync
        return Task.FromResult(true);
    }

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var logger = context.Logger;
        var options = context.Configuration.GetCouponBundleOptions();

        // Load bundle files
        var bundles = await LoadBundlesAsync(logger, options, cancellationToken);
        if (bundles.Count == 0)
        {
            logger.LogInformation("No coupon bundle files found in Data/Coupons directory.");
            return Result.Success();
        }

        // Track statistics
        var stats = new SeedingStats();

        foreach (var (fileName, bundle) in bundles)
        {
            var bundleStats = new SeedingStats();
            
            // Validate bundle
            var validation = CouponBundleValidation.Validate(bundle);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    logger.LogWarning("Bundle {File}: {Error}", fileName, error);
                }
                stats.Skipped += bundle.Coupons.Count;
                continue;
            }

            // Process each coupon in the bundle
            foreach (var coupon in bundle.Coupons)
            {
                await ProcessCouponAsync(context, bundle.RestaurantSlug, coupon, options, logger, bundleStats, cancellationToken);
            }
            
            // Log bundle summary
            logger.LogInformation(
                "Processed coupon bundle '{Slug}': {Created} created, {Updated} updated, {Skipped} skipped",
                bundle.RestaurantSlug, bundleStats.Created, bundleStats.Updated, bundleStats.Skipped);
            
            // Add to total stats
            stats.Created += bundleStats.Created;
            stats.Updated += bundleStats.Updated;
            stats.Skipped += bundleStats.Skipped;
        }

        // Log summary
        logger.LogInformation(
            "Coupon seeding completed: {Created} created, {Updated} updated, {Skipped} skipped",
            stats.Created, stats.Updated, stats.Skipped);

        return Result.Success();
    }

    private async Task ProcessCouponAsync(
        SeedingContext context,
        string restaurantSlug,
        CouponData coupon,
        CouponBundleOptions options,
        ILogger logger,
        SeedingStats stats,
        CancellationToken cancellationToken)
    {
        // Resolve restaurant by slug from SharedData (populated by RestaurantBundleSeeder)
        RestaurantId? restaurantId = null;
        
        // Try to get from slug map first
        if (context.SharedData.TryGetValue("RestaurantSlugMap", out var slugMapObj) 
            && slugMapObj is Dictionary<string, Guid> slugMap
            && slugMap.TryGetValue(restaurantSlug, out var restaurantGuid))
        {
            restaurantId = RestaurantId.Create(restaurantGuid);
        }
        
        // Fallback: lookup by name in database
        if (restaurantId is null)
        {
            var restaurant = await context.DbContext.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name.ToLower() == restaurantSlug.ToLower(), cancellationToken);

            if (restaurant is null)
            {
                logger.LogWarning("Restaurant with slug/name '{Slug}' not found. Skipping coupon '{Code}'", 
                    restaurantSlug, coupon.Code);
                stats.Skipped++;
                return;
            }
            
            restaurantId = restaurant.Id;
        }

        // Check if coupon already exists
        var normalizedCode = coupon.Code.ToUpperInvariant();
        var existing = await context.DbContext.Coupons
            .FirstOrDefaultAsync(c => c.RestaurantId == restaurantId && c.Code == normalizedCode, cancellationToken);

        if (existing is not null)
        {
            if (options.OverwriteExisting)
            {
                // Update description if option is enabled
                var updateResult = existing.UpdateDescription(coupon.Description);
                if (updateResult.IsSuccess)
                {
                    stats.Updated++;
                }
            }
            else
            {
                stats.Skipped++;
            }
            return;
        }

        if (options.ReportOnly)
        {
            stats.Created++;
            return;
        }

        // Build CouponValue
        var valueResult = await BuildCouponValueAsync(context, coupon, restaurantId, logger, cancellationToken);
        if (valueResult.IsFailure)
        {
            logger.LogWarning("Failed to build coupon value for '{Code}': {Error}", 
                coupon.Code, valueResult.Error.Description);
            stats.Skipped++;
            return;
        }

        // Build AppliesTo
        var appliesToResult = await BuildAppliesToAsync(context, coupon, restaurantId, logger, cancellationToken);
        if (appliesToResult.IsFailure)
        {
            logger.LogWarning("Failed to build AppliesTo for '{Code}': {Error}", 
                coupon.Code, appliesToResult.Error.Description);
            stats.Skipped++;
            return;
        }

        // Build optional minimum order amount
        Money? minOrderAmount = null;
        if (coupon.MinOrderAmount.HasValue)
        {
            minOrderAmount = new Money(coupon.MinOrderAmount.Value, coupon.MinOrderCurrency!);
        }

        // Create coupon
        var couponResult = Coupon.Create(
            restaurantId,
            coupon.Code,
            coupon.Description,
            valueResult.Value,
            appliesToResult.Value,
            coupon.ValidityStartDate,
            coupon.ValidityEndDate,
            minOrderAmount,
            coupon.TotalUsageLimit,
            coupon.UsageLimitPerUser,
            coupon.IsEnabled);

        if (couponResult.IsFailure)
        {
            logger.LogWarning("Failed to create coupon '{Code}': {Error}", 
                coupon.Code, couponResult.Error.Description);
            stats.Skipped++;
            return;
        }

        // Clear domain events and add to context
        couponResult.Value.ClearDomainEvents();
        context.DbContext.Coupons.Add(couponResult.Value);
        await context.DbContext.SaveChangesAsync(cancellationToken);

        stats.Created++;
    }

    private async Task<Result<CouponValue>> BuildCouponValueAsync(
        SeedingContext context,
        CouponData coupon,
        RestaurantId restaurantId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return coupon.ValueType.ToLowerInvariant() switch
        {
            "percentage" => CouponValue.CreatePercentage(coupon.Percentage!.Value),
            "fixedamount" => CouponValue.CreateFixedAmount(new Money(coupon.FixedAmount!.Value, coupon.FixedCurrency!)),
            "freeitem" => await BuildFreeItemValueAsync(context, coupon, restaurantId, logger, cancellationToken),
            _ => Result.Failure<CouponValue>(Error.Validation("Coupon.InvalidValueType", $"Unknown value type: {coupon.ValueType}"))
        };
    }

    private async Task<Result<CouponValue>> BuildFreeItemValueAsync(
        SeedingContext context,
        CouponData coupon,
        RestaurantId restaurantId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Find the menu item by name within this restaurant's menus
        var menuItem = await context.DbContext.MenuItems
            .AsNoTracking()
            .Join(
                context.DbContext.MenuCategories,
                item => item.MenuCategoryId,
                category => category.Id,
                (item, category) => new { item, category })
            .Join(
                context.DbContext.Menus,
                combined => combined.category.MenuId,
                menu => menu.Id,
                (combined, menu) => new { combined.item, menu })
            .Where(x => x.menu.RestaurantId == restaurantId && x.item.Name == coupon.FreeItemName)
            .Select(x => x.item)
            .FirstOrDefaultAsync(cancellationToken);

        if (menuItem is null)
        {
            return Result.Failure<CouponValue>(Error.NotFound(
                "Coupon.FreeItemNotFound",
                $"Menu item '{coupon.FreeItemName}' not found in restaurant"));
        }

        return CouponValue.CreateFreeItem(menuItem.Id);
    }

    private async Task<Result<AppliesTo>> BuildAppliesToAsync(
        SeedingContext context,
        CouponData coupon,
        RestaurantId restaurantId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        switch (coupon.Scope.ToLowerInvariant())
        {
            case "wholeorder":
                return AppliesTo.CreateForWholeOrder();

            case "specificitems":
                var itemIds = await ResolveMenuItemIdsAsync(context, coupon.ItemNames!, restaurantId, logger, cancellationToken);
                if (itemIds.Count == 0)
                {
                    return Result.Failure<AppliesTo>(Error.NotFound(
                        "Coupon.NoItemsFound",
                        "No menu items found matching the specified names"));
                }
                return AppliesTo.CreateForSpecificItems(itemIds);

            case "specificcategories":
                var categoryIds = await ResolveMenuCategoryIdsAsync(context, coupon.CategoryNames!, restaurantId, logger, cancellationToken);
                if (categoryIds.Count == 0)
                {
                    return Result.Failure<AppliesTo>(Error.NotFound(
                        "Coupon.NoCategoriesFound",
                        "No menu categories found matching the specified names"));
                }
                return AppliesTo.CreateForSpecificCategories(categoryIds);

            default:
                return Result.Failure<AppliesTo>(Error.Validation(
                    "Coupon.InvalidScope",
                    $"Unknown scope: {coupon.Scope}"));
        }
    }

    private async Task<List<MenuItemId>> ResolveMenuItemIdsAsync(
        SeedingContext context,
        List<string> itemNames,
        RestaurantId restaurantId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var menuItems = await context.DbContext.MenuItems
            .AsNoTracking()
            .Join(
                context.DbContext.MenuCategories,
                item => item.MenuCategoryId,
                category => category.Id,
                (item, category) => new { item, category })
            .Join(
                context.DbContext.Menus,
                combined => combined.category.MenuId,
                menu => menu.Id,
                (combined, menu) => new { combined.item, menu })
            .Where(x => x.menu.RestaurantId == restaurantId && itemNames.Contains(x.item.Name))
            .Select(x => x.item.Id)
            .ToListAsync(cancellationToken);

        if (menuItems.Count < itemNames.Count)
        {
            logger.LogWarning("Some menu items were not found. Expected {Expected}, found {Found}",
                itemNames.Count, menuItems.Count);
        }

        return menuItems;
    }

    private async Task<List<MenuCategoryId>> ResolveMenuCategoryIdsAsync(
        SeedingContext context,
        List<string> categoryNames,
        RestaurantId restaurantId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var categories = await context.DbContext.MenuCategories
            .AsNoTracking()
            .Join(
                context.DbContext.Menus,
                category => category.MenuId,
                menu => menu.Id,
                (category, menu) => new { category, menu })
            .Where(x => x.menu.RestaurantId == restaurantId && categoryNames.Contains(x.category.Name))
            .Select(x => x.category.Id)
            .ToListAsync(cancellationToken);

        if (categories.Count < categoryNames.Count)
        {
            logger.LogWarning("Some menu categories were not found. Expected {Expected}, found {Found}",
                categoryNames.Count, categories.Count);
        }

        return categories;
    }

    private async Task<List<(string FileName, CouponBundle Bundle)>> LoadBundlesAsync(
        ILogger logger,
        CouponBundleOptions options,
        CancellationToken cancellationToken)
    {
        var bundles = new List<(string, CouponBundle)>();

        // Load from embedded resources
        var asm = typeof(CouponBundleSeeder).Assembly;
        var resourceNames = asm.GetManifestResourceNames()
            .Where(n => n.EndsWith(".coupon.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // Apply glob filtering if specified
        if (options.CouponGlobs is { Length: > 0 })
        {
            resourceNames = resourceNames
                .Where(n => options.CouponGlobs.Any(glob => MatchesGlob(Path.GetFileName(n), glob)))
                .ToArray();
        }

        foreach (var resourceName in resourceNames)
        {
            try
            {
                await using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream is null)
                {
                    logger.LogWarning("Failed to load embedded resource: {Name}", resourceName);
                    continue;
                }

                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync(cancellationToken);
                var bundle = JsonSerializer.Deserialize<CouponBundle>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (bundle is not null)
                {
                    var shortName = Path.GetFileName(resourceName);
                    bundles.Add((shortName, bundle));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load coupon bundle from embedded resource: {Name}", resourceName);
            }
        }

        return bundles;
    }

    private bool MatchesGlob(string fileName, string pattern)
    {
        // Simple glob matching (* wildcard only)
        if (pattern == "*" || pattern == "*.*")
            return true;

        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private class SeedingStats
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
    }
}
