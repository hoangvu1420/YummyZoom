using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.Search.Queries.Autocomplete;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using Error = YummyZoom.SharedKernel.Error;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Web.ApiContractTests.Search;

/// <summary>
/// Contract tests for GET /api/v1/search/autocomplete
/// Verifies binding of term, success body, and ProblemDetails mapping.
/// </summary>
public class AutocompleteContractTests
{
    [Test]
    public async Task Autocomplete_WithValidTerm_Returns200_WithSuggestions()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var list = new List<SuggestionDto>
        {
            new(Guid.NewGuid(), "restaurant", "Cafe Aroma"),
            new(Guid.NewGuid(), "menu-item", "Margherita Pizza")
        };

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<AutocompleteQuery>();
            var q = (AutocompleteQuery)req;
            q.Term.Should().Be("piz");
            return Result.Success((IReadOnlyList<SuggestionDto>)list);
        });

        var path = "/api/v1/search/autocomplete?term=piz";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType?.MediaType.Should().StartWith("application/json");

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().Be(2);

        var first = root[0];
        first.GetProperty("type").GetString().Should().Be("restaurant");
        first.GetProperty("name").GetString().Should().Be("Cafe Aroma");

        factory.Sender.LastRequest.Should().BeOfType<AutocompleteQuery>();
    }

    [Test]
    public async Task Autocomplete_WhenValidationFails_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        factory.Sender.RespondWith(_ =>
            Result.Failure<IReadOnlyList<SuggestionDto>>(Error.Validation("Search.Autocomplete.Invalid", "Term required")));

        var path = "/api/v1/search/autocomplete?term="; // empty term -> failure simulated by stub
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(400);
        prob.Title.Should().Be("Search");
    }

    [Test]
    public async Task Autocomplete_WhenTermMissing_MapsToEmptyString_InRequest()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<AutocompleteQuery>();
            var q = (AutocompleteQuery)req;
            q.Term.Should().Be(""); // default from request DTO when not provided
            return Result.Success((IReadOnlyList<SuggestionDto>)Array.Empty<SuggestionDto>());
        });

        var path = "/api/v1/search/autocomplete"; // no term param
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetArrayLength().Should().Be(0);
    }
}

