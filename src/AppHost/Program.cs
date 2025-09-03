var builder = DistributedApplication.CreateBuilder(args);

var databaseName = "YummyZoomDb";

var postgres = builder
    .AddPostgres("postgres")
    // Use a PostGIS-enabled image so spatial types and functions are available.
    .WithImage("postgis/postgis", "16-3.4")
    .WithImageRegistry("docker.io")
    .WithPgAdmin()
    // Set the name of the default database to auto-create on container startup.
    .WithEnvironment("POSTGRES_DB", databaseName);

var database = postgres.AddDatabase(databaseName);

builder.AddProject<Projects.Web>("web")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
