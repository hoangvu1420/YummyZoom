using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Seeders.RestaurantSeeders;

public class TagSeeder : ISeeder
{
    public string Name => "Tag";
    public int Order => 105; // After restaurants, before menus

    public async Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
        => !await context.DbContext.Tags.AnyAsync(cancellationToken);

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await LoadTemplatesAsync();
            var seeded = 0;

            // Load existing (name,category) set to ensure idempotency
            var existing = await context.DbContext.Tags
                .Select(t => new { t.TagName, Category = t.TagCategory })
                .ToListAsync(cancellationToken);

            foreach (var tt in templates.Tags)
            {
                if (!TagCategoryExtensions.TryParse(tt.TagCategory, out var category))
                {
                    context.Logger.LogWarning("Skipping tag with invalid category: {Name} ({Category})", tt.TagName, tt.TagCategory);
                    continue;
                }

                if (existing.Any(e => e.TagName == tt.TagName && e.Category == category))
                    continue;

                var created = Tag.Create(tt.TagName, category, tt.TagDescription);
                if (created.IsFailure)
                {
                    context.Logger.LogWarning("Failed to create tag {Name}: {Error}", tt.TagName, created.Error.Description);
                    continue;
                }

                created.Value.ClearDomainEvents();
                context.DbContext.Tags.Add(created.Value);
                seeded++;
            }

            if (seeded > 0)
            {
                await context.DbContext.SaveChangesAsync(cancellationToken);
            }

            context.Logger.LogInformation("[Tags] Seeded {Count} tags.", seeded);
            return Result.Success();
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "[Tags] Tag seeding failed.");
            return Result.Failure(Error.Failure("Seeding.Tag", "Tag seeding failed"));
        }
    }

    private static async Task<TagTemplatesDoc> LoadTemplatesAsync()
    {
        var asm = typeof(TagSeeder).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("TagsTemplates.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return new TagTemplatesDoc();
        }

        await using var stream = asm.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var doc = JsonSerializer.Deserialize<TagTemplatesDoc>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return doc ?? new TagTemplatesDoc();
    }
}

internal sealed class TagTemplatesDoc
{
    public List<TagTemplate> Tags { get; set; } = new();
}

internal sealed class TagTemplate
{
    public string TagName { get; set; } = string.Empty;
    public string TagCategory { get; set; } = string.Empty;
    public string? TagDescription { get; set; }
}
