using System.Reflection;
using Asp.Versioning;

namespace YummyZoom.Web.Infrastructure;

public static class WebApplicationExtensions
{
    public static RouteGroupBuilder MapGroup(this IEndpointRouteBuilder app, EndpointGroupBase group)
    {
        var className = group.GetType().Name;
        var routeName = className.ToKebabCase();

        return app
            .MapGroup($"/{routeName}")
            .WithGroupName(className)
            .WithTags(className);
    }

    public static WebApplication MapVersionedEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var versionedGroup = app
            .MapGroup("/api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet);

        var endpointGroupType = typeof(EndpointGroupBase);
        var assembly = Assembly.GetExecutingAssembly();
        var endpointGroupTypes = assembly.GetExportedTypes()
            .Where(t => t.IsSubclassOf(endpointGroupType));

        foreach (var type in endpointGroupTypes)
        {
            if (Activator.CreateInstance(type) is EndpointGroupBase instance)
            {
                instance.Map(versionedGroup);
            }
        }

        return app;
    }

    // Obsolete the old MapEndpoints method to prevent accidental use
    [Obsolete("Use MapVersionedEndpoints() instead.", true)]
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpointGroupType = typeof(EndpointGroupBase);

        var assembly = Assembly.GetExecutingAssembly();

        var endpointGroupTypes = assembly.GetExportedTypes()
            .Where(t => t.IsSubclassOf(endpointGroupType));

        foreach (var type in endpointGroupTypes)
        {
            if (Activator.CreateInstance(type) is EndpointGroupBase instance)
            {
                instance.Map(app);
            }
        }

        return app;
    }
}
