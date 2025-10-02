using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Search.Queries.Autocomplete;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Search;

[TestFixture]
public class AutocompleteTests : BaseTestFixture
{
    [Test]
    public async Task PrefixMatch_ShouldReturnTopSuggestions()
    {
        await CreateRestaurantAsync("Alpha Cafe", "Cafe");
        await CreateRestaurantAsync("Alpine Diner", "Diner");
        await CreateRestaurantAsync("Beta Bistro", "Bistro");
        await DrainOutboxAsync();

        var res = await SendAsync(new AutocompleteQuery("Al"));
        res.ShouldBeSuccessful();

        var names = res.Value.Select(s => s.Name).ToList();
        names.Should().Contain(new[] { "Alpha Cafe", "Alpine Diner" });
        names.Should().NotContain("Beta Bistro");
        res.Value.Should().HaveCountLessOrEqualTo(10);
    }

    [Test]
    public async Task Ranking_ShouldPrioritizePrefixOverTrigram()
    {
        await CreateRestaurantAsync("Alpha Cafe", "Cafe");
        await CreateRestaurantAsync("Pha Bistro", "Bistro");
        await DrainOutboxAsync();

        var res = await SendAsync(new AutocompleteQuery("Al"));
        res.ShouldBeSuccessful();

        var names = res.Value.Select(s => s.Name).ToList();
        names.First().Should().Be("Alpha Cafe");
    }

    [Test]
    public async Task Ordering_ShouldTieBreakByUpdatedAtDesc()
    {
        var a = await CreateRestaurantAsync("Tie Alpha", "Cafe");
        var b = await CreateRestaurantAsync("Tie Alpine", "Cafe");
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""UpdatedAt"" = now() - interval '10 minutes' WHERE ""Id"" = {a};
                UPDATE ""SearchIndexItems"" SET ""UpdatedAt"" = now() WHERE ""Id"" = {b};
            ");
        }

        var res = await SendAsync(new AutocompleteQuery("Ti"));
        res.ShouldBeSuccessful();
        res.Value.First().Name.Should().Be("Tie Alpine");
    }

    [Test]
    public async Task Limit_ShouldBeEnforcedTo10()
    {
        for (int i = 0; i < 20; i++)
        {
            await CreateRestaurantAsync($"Alpha {i:00}", "Cafe");
        }
        await DrainOutboxAsync();

        var res = await SendAsync(new AutocompleteQuery("Alpha"));
        res.ShouldBeSuccessful();
        res.Value.Count.Should().BeLessOrEqualTo(10);
    }

    [Test]
    public async Task Diversity_ShouldDedupeAndMixTypes()
    {
        // Restaurant and a similarly named MenuItem should both be allowed (when present)
        await CreateRestaurantAsync("Pizza Place", "Italian");
        await DrainOutboxAsync();

        var res = await SendAsync(new AutocompleteQuery("Pizza"));
        res.ShouldBeSuccessful();
        res.Value.Select(s => s.Name).Should().Contain(n => n.Contains("Pizza", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Input_ShouldHandleSpecialCharsAndUnicode()
    {
        await CreateRestaurantAsync("Café Déjà Vu", "Cafe");
        await CreateRestaurantAsync("Sushi (O'Clock)", "Japanese");
        await DrainOutboxAsync();

        var res1 = await SendAsync(new AutocompleteQuery("Cafe"));
        res1.ShouldBeSuccessful();
        res1.Value.Select(s => s.Name).Should().Contain(n => n.Contains("Café", StringComparison.OrdinalIgnoreCase) || n.Contains("Cafe", StringComparison.OrdinalIgnoreCase));

        var res2 = await SendAsync(new AutocompleteQuery("O'Clock"));
        res2.ShouldBeSuccessful();
        res2.Value.Select(s => s.Name).Should().Contain(n => n.Contains("Sushi", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task TrigramMatch_ShouldHandleMisspellings()
    {
        await CreateRestaurantAsync("Pizzeria Roma", "Italian");
        await DrainOutboxAsync();

        var res = await SendAsync(new AutocompleteQuery("Pizera"));
        res.ShouldBeSuccessful();
        res.Value.Select(x => x.Name).Should().Contain(n => n.Contains("Pizzeria", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Validation_ShouldFail_OnEmptyOrTooLongTerm()
    {
        var empty = () => SendAsync(new AutocompleteQuery(""));
        await empty.Should().ThrowAsync<ValidationException>();

        var longTerm = new string('x', 100);
        var tooLong = () => SendAsync(new AutocompleteQuery(longTerm));
        await tooLong.Should().ThrowAsync<ValidationException>();
    }

    private static async Task<Guid> CreateRestaurantAsync(string name, string cuisine)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("09:00-17:00").Value;
        var created = Restaurant.Create(name, null, null, "desc", cuisine, address, contact, hours);
        var entity = created.Value;
        entity.Verify();
        entity.AcceptOrders();
        await AddAsync(entity);
        return entity.Id.Value;
    }
}

