using System.Data.Common;

namespace YummyZoom.Application.FunctionalTests.Infrastructure.Database;

public interface ITestDatabase
{
    Task InitialiseAsync();

    DbConnection GetConnection();

    string GetConnectionString();

    Task ResetAsync();

    Task DisposeAsync();
}
