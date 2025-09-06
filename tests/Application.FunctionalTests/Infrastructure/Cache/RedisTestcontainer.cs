using System;
using System.Threading.Tasks;
using Testcontainers.Redis;

namespace YummyZoom.Application.FunctionalTests.Infrastructure.Cache;

public class RedisTestcontainer : IAsyncDisposable
{
    private readonly RedisContainer _container;
    private string _connectionString = string.Empty;

    public RedisTestcontainer()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithAutoRemove(true)
            .Build();
    }

    public async Task StartAsync()
    {
        await _container.StartAsync();

        var raw = _container.GetConnectionString();
        _connectionString = Normalize(raw);
    }

    public string GetConnectionString() => _connectionString;

    private static string Normalize(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host) && uri.Port > 0)
        {
            return $"{uri.Host}:{uri.Port}";
        }

        // Fallback: strip common prefixes if present
        return input
            .Replace("redis://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("rediss://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim('/');
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

