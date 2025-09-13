using YummyZoom.Application;
using YummyZoom.Infrastructure;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Web;
using YummyZoom.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.AddKeyVaultIfConfigured();
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

app.UseAuthentication();
app.UseAuthorization();

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
app.MapHub<YummyZoom.Web.Realtime.Hubs.CustomerOrdersHub>("/hubs/customer-orders");

// Conditionally map TeamCart hub behind feature flag and Redis readiness
var teamCartAvailability = app.Services.GetRequiredService<YummyZoom.Web.Services.ITeamCartFeatureAvailability>();
if (teamCartAvailability.Enabled && teamCartAvailability.RealTimeReady)
{
    app.MapHub<YummyZoom.Web.Realtime.Hubs.TeamCartHub>("/hubs/teamcart");
}

app.Run();

public partial class Program { }
