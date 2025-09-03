using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YummyZoom.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core tools. This allows creating ApplicationDbContext
/// without starting the ASP.NET host (and without initializing external services)
/// during migrations add/remove/update operations.
/// </summary>
public sealed class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            // Dummy connection string suitable for design-time metadata building.
            // EF tooling won't attempt to connect when only building the model.
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=DesignTimeOnly;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite());

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

