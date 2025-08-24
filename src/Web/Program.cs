using YummyZoom.Application;
using YummyZoom.Infrastructure;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.AddKeyVaultIfConfigured();
builder.AddFirebaseIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseOpenApi(settings =>
{
    // The path needs to be versioned for NSwag to find the correct document
    settings.Path = "/api/{documentName}/specification.json";
});

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    // Configure multiple document URLs, one for each version
    var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions.Reverse())
    {
        settings.DocumentPath = $"/api/{description.GroupName}/specification.json";
        settings.DocumentTitle = $"YummyZoom API {description.ApiVersion}";
    }
});
app.UseStaticFiles();

app.UseExceptionHandler(options => { });

app.Map("/", () => Results.Redirect("/api"));

app.MapDefaultEndpoints();

// Map versioned endpoints.
app.MapVersionedEndpoints();

// Map SignalR hubs
app.MapHub<YummyZoom.Web.Realtime.Hubs.RestaurantOrdersHub>("/hubs/restaurant-orders");

app.Run();

public partial class Program { }
