var builder = DistributedApplication.CreateBuilder(args);

var databaseName = "YummyZoomDb";
var isPublishMode = builder.ExecutionContext.IsPublishMode;

var redis = isPublishMode
    ? builder.AddConnectionString("redis")
    : builder.AddRedis("redis");

var web = builder.AddProject<Projects.Web>("web")
    .WithExternalHttpEndpoints();

if (isPublishMode)
{
    var postgres = builder.AddAzurePostgresFlexibleServer("postgres");
    var database = postgres.AddDatabase(databaseName);
    web.WithReference(database)
        .WaitFor(database);
}
else
{
    var postgres = builder.AddPostgres("postgres")
        // Use a PostGIS-enabled image so spatial types and functions are available.
        .WithImage("postgis/postgis", "16-3.4")
        .WithImageRegistry("docker.io")
        .WithPgAdmin()
        // Set the name of the default database to auto-create on container startup.
        .WithEnvironment("POSTGRES_DB", databaseName);
    var database = postgres.AddDatabase(databaseName);
    web.WithReference(database)
        .WaitFor(database);
}

if (isPublishMode)
{
    web.WithReference(redis);
}
else
{
    web.WithReference(redis)
        .WaitFor(redis);
}

builder.Build().Run();
