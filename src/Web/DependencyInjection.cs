using Azure.Identity;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Web.Services;
using Microsoft.AspNetCore.Mvc;

using NSwag;
using NSwag.Generation.Processors.Security;
using Asp.Versioning;
using Azure.Security.KeyVault.Secrets;

namespace YummyZoom.Web;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<CustomExceptionHandler>();

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        // Add API Versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            // Format the version as "v{major}.{minor}"
            options.GroupNameFormat = "'v'VVV";

            // Substitute the version in the route template
            options.SubstituteApiVersionInUrl = true;
        });

        builder.Services.AddOpenApiDocument(configure =>
        {
            configure.Title = "YummyZoom API";

            // Add JWT
            configure.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
        });
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(keyVaultUri))
        {
            return;
        }

        try
        {
            // Try to connect to the Key Vault to ensure it's accessible
            var credential = new DefaultAzureCredential();
            var client = new SecretClient(new Uri(keyVaultUri), credential);
            // Attempt to list secrets as a connectivity check (minimal call)
            using IEnumerator<SecretProperties> enumerator = client.GetPropertiesOfSecrets().GetEnumerator();
            enumerator.MoveNext();
                
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                credential);
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException($"Failed to connect to Azure Key Vault at '{keyVaultUri}'.", ex);
        }
    }
}
