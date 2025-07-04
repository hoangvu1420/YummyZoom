using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using YummyZoom.Application.Common.Interfaces;

namespace YummyZoom.Infrastructure.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;
    private NpgsqlConnection? _connection;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
        {
            var connectionString = _configuration.GetConnectionString("YummyZoomDb");
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
        }
        return _connection;
    }

    public void Dispose()
    {
        if (_connection is not null && _connection.State == ConnectionState.Open)
        {
            _connection.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
