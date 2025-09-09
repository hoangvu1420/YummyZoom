using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using YummyZoom.Application.Common.Interfaces;

namespace YummyZoom.Infrastructure.Persistence;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        // Always create a new connection. Let ADO.NET connection pooling handle reuse.
        var connectionString = _configuration.GetConnectionString("YummyZoomDb");
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        // No-op: callers are responsible for disposing the connections they create via using blocks.
        GC.SuppressFinalize(this);
    }
}
