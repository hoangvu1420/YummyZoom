# Codebase Documentation

This document provides an overview of the codebase structure and content.

## Directory Structure

- YummyZoom/
  - Plans/
  - src/
    - AppHost/
      - AppHost.csproj
      - appsettings.Development.json
      - appsettings.json
      - Program.cs
      - Properties/
        - launchSettings.json
    - Application/
      - Application.csproj
      - DependencyInjection.cs
      - GlobalUsings.cs
      - Common/
        - Behaviours/
          - AuthorizationBehaviour.cs
          - LoggingBehaviour.cs
          - PerformanceBehaviour.cs
          - UnhandledExceptionBehaviour.cs
          - ValidationBehaviour.cs
        - Exceptions/
          - ForbiddenAccessException.cs
          - ValidationException.cs
        - Interfaces/
          - IApplicationDbContext.cs
          - IIdentityService.cs
          - IUser.cs
          - IUserAggregateRepository.cs
        - Mappings/
          - MappingExtensions.cs
        - Models/
          - LookupDto.cs
          - PaginatedList.cs
          - Result.cs
        - Security/
          - AuthorizeAttribute.cs
      - TodoItems/
        - Commands/
          - CreateTodoItem/
            - CreateTodoItem.cs
            - CreateTodoItemCommandValidator.cs
          - DeleteTodoItem/
            - DeleteTodoItem.cs
          - UpdateTodoItem/
            - UpdateTodoItem.cs
            - UpdateTodoItemCommandValidator.cs
          - UpdateTodoItemDetail/
            - UpdateTodoItemDetail.cs
        - EventHandlers/
          - TodoItemCompletedEventHandler.cs
          - TodoItemCreatedEventHandler.cs
        - Queries/
          - GetTodoItemsWithPagination/
            - GetTodoItemsWithPagination.cs
            - GetTodoItemsWithPaginationQueryValidator.cs
            - TodoItemBriefDto.cs
      - TodoLists/
        - Commands/
          - CreateTodoList/
            - CreateTodoList.cs
            - CreateTodoListCommandValidator.cs
          - DeleteTodoList/
            - DeleteTodoList.cs
          - PurgeTodoLists/
            - PurgeTodoLists.cs
          - UpdateTodoList/
            - UpdateTodoList.cs
            - UpdateTodoListCommandValidator.cs
        - Queries/
          - GetTodos/
            - GetTodos.cs
            - TodoItemDto.cs
            - TodoListDto.cs
            - TodosVm.cs
      - Users/
        - Commands/
          - AssignRoleToUser/
            - AssignRoleToUserCommand.cs
            - AssignRoleToUserCommandHandler.cs
            - AssignRoleToUserCommandValidator.cs
          - RegisterUser/
            - RegisterUserCommand.cs
            - RegisterUserCommandHandler.cs
            - RegisterUserCommandValidator.cs
            - RegisterUserResponse.cs
          - RemoveRoleFromUser/
            - RemoveRoleFromUserCommand.cs
            - RemoveRoleFromUserCommandHandler.cs
            - RemoveRoleFromUserCommandValidator.cs
        - EventHandlers/
          - RoleAssignmentAddedToUserEventHandler.cs
          - RoleAssignmentRemovedFromUserEventHandler.cs
      - WeatherForecasts/
        - Queries/
          - GetWeatherForecasts/
            - GetWeatherForecastsQuery.cs
            - WeatherForecast.cs
    - Domain/
      - Domain.csproj
      - GlobalUsings.cs
      - Common/
        - Errors/
          - Errors.Authentication.cs
          - Errors.Color.cs
          - Errors.TodoItem.cs
          - Errors.TodoList.cs
          - Errors.User.cs
        - Models/
          - AggregateRoot.cs
          - AggregateRootId.cs
          - Entity.cs
          - IAuditableEntity.cs
          - IDomainEvent.cs
          - IHasDomainEvent.cs
          - ValueObject.cs
        - ValueObjects/
          - Address.cs
          - AverageRating.cs
          - Rating.cs
      - Constants/
      - TodoListAggregate/
        - TodoList.cs
        - Entities/
          - TodoItem.cs
        - Enums/
          - PriorityLevel.cs
        - Errors/
          - TodoItemErrors.cs
          - TodoListErrors.cs
        - Events/
          - TodoItemCompletedEvent.cs
          - TodoItemCreatedEvent.cs
          - TodoItemDeletedEvent.cs
        - ValueObjects/
          - Colour.cs
          - TodoItemId.cs
          - TodoListId.cs
      - UserAggregate/
        - User.cs
        - Entities/
          - PaymentMethod.cs
        - Errors/
          - UserErrors.cs
        - Events/
          - RoleAssignmentAddedToUserEvent.cs
          - RoleAssignmentRemovedFromUserEvent.cs
        - ValueObjects/
          - PaymentMethodId.cs
          - RoleAssignment.cs
          - UserId.cs
    - Infrastructure/
      - DependencyInjection.cs
      - GlobalUsings.cs
      - Infrastructure.csproj
      - Data/
        - ApplicationDbContext.cs
        - ApplicationDbContextInitialiser.cs
        - Configurations/
          - TodoListConfiguration.cs
          - UserConfiguration.cs
        - Interceptors/
          - AuditableEntityInterceptor.cs
          - DispatchDomainEventsInterceptor.cs
      - Identity/
        - ApplicationUser.cs
        - IdentityResultExtensions.cs
        - IdentityService.cs
      - Persistence/
        - Repositories/
          - UserAggregateRepository.cs
    - ServiceDefaults/
      - Extensions.cs
      - ServiceDefaults.csproj
    - SharedKernel/
      - Error.cs
      - ErrorType.cs
      - Result.cs
      - SharedKernel.csproj
      - ValidationError.cs
      - Constants/
        - Policies.cs
        - Roles.cs
    - Web/
      - appsettings.Development.json
      - appsettings.json
      - DependencyInjection.cs
      - GlobalUsings.cs
      - Program.cs
      - Web.csproj
      - Web.http
      - Endpoints/
        - TodoItems.cs
        - TodoLists.cs
        - Users.cs
        - WeatherForecasts.cs
      - Infrastructure/
        - CustomExceptionHandler.cs
        - CustomResults.cs
        - EndpointExtensions.cs
        - EndpointGroupBase.cs
        - IEndpointRouteBuilderExtensions.cs
        - MethodInfoExtensions.cs
        - ResultExtensions.cs
        - WebApplicationExtensions.cs
      - logs/
      - Properties/
        - launchSettings.json
      - Services/
        - CurrentUser.cs
      - wwwroot/
  - tests/
    - Application.FunctionalTests/
      - Application.FunctionalTests.csproj
      - appsettings.json
      - BaseTestFixture.cs
      - CustomWebApplicationFactory.cs
      - GlobalUsings.cs
      - ITestDatabase.cs
      - PostgreSQLTestcontainersTestDatabase.cs
      - PostgreSQLTestDatabase.cs
      - ResultAssertions.cs
      - TestDatabaseFactory.cs
      - Testing.cs
      - TodoItems/
        - Commands/
          - CreateTodoItemTests.cs
          - DeleteTodoItemTests.cs
          - UpdateNonExistentItemTests.cs
          - UpdateTodoItemDetailTests.cs
          - UpdateTodoItemTests.cs
        - Queries/
          - GetTodoItemsWithPaginationTests.cs
      - TodoLists/
        - Commands/
          - CreateTodoListTests.cs
          - DeleteTodoListTests.cs
          - PurgeTodoListsTests.cs
          - UpdateTodoListTests.cs
        - Queries/
          - GetTodosTests.cs
      - Users/
        - RegisterUserTests.cs
        - RoleAssignmentTests.cs
    - Application.UnitTests/
      - Application.UnitTests.csproj
      - Common/
        - Behaviours/
          - RequestLoggerTests.cs
        - Exceptions/
          - ValidationExceptionTests.cs
        - Mappings/
          - MappingTests.cs
    - Domain.UnitTests/
      - Domain.UnitTests.csproj
      - Common/
        - ValueObjects/
          - AddressTests.cs
      - TodoListAggregate/
        - TodoListTests.cs
      - UserAggregate/
        - UserAggregateTests.cs
        - Entities/
          - PaymentMethodTests.cs
        - Errors/
          - UserErrorsTests.cs
        - ValueObjects/
          - PaymentMethodIdTests.cs
          - RoleAssignmentTests.cs
          - UserIdTests.cs
      - ValueObjects/
        - ColourTests.cs
    - Infrastructure.IntegrationTests/
      - GlobalUsings.cs
      - Infrastructure.IntegrationTests.csproj


## Code Snippets

### azure.yaml

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

# Name of the application.
name: clean-architecture-azd
services:
  web:
    language: csharp
    project: ./src/Web
    host: appservice

```

### global.json

```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
```

### .devcontainer\devcontainer.json

```json
{
    "name": "Azure Developer CLI",
    "image": "mcr.microsoft.com/devcontainers/python:3.10-bullseye",
    "features": {
        // See https://containers.dev/features for list of features
        "ghcr.io/devcontainers/features/docker-in-docker:2": {
        },
        "ghcr.io/azure/azure-dev/azd:latest": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "GitHub.vscode-github-actions",
                "ms-azuretools.azure-dev",
                "ms-azuretools.vscode-azurefunctions",
                "ms-azuretools.vscode-bicep",
                "ms-azuretools.vscode-docker"
                // Include other VSCode language extensions if needed
                // Right click on an extension inside VSCode to add directly to devcontainer.json, or copy the extension ID
            ]
        }
    },
    "forwardPorts": [
        // Forward ports if needed for local development
    ],
    "postCreateCommand": "",
    "remoteUser": "vscode",
    "hostRequirements": {
        "memory": "8gb"
    }
}

```

### infra\abbreviations.json

```json
{
    "analysisServicesServers": "as",
    "apiManagementService": "apim-",
    "appConfigurationStores": "appcs-",
    "appManagedEnvironments": "cae-",
    "appContainerApps": "ca-",
    "authorizationPolicyDefinitions": "policy-",
    "automationAutomationAccounts": "aa-",
    "blueprintBlueprints": "bp-",
    "blueprintBlueprintsArtifacts": "bpa-",
    "cacheRedis": "redis-",
    "cdnProfiles": "cdnp-",
    "cdnProfilesEndpoints": "cdne-",
    "cognitiveServicesAccounts": "cog-",
    "cognitiveServicesFormRecognizer": "cog-fr-",
    "cognitiveServicesTextAnalytics": "cog-ta-",
    "cognitiveServicesSpeech": "cog-sp-",
    "computeAvailabilitySets": "avail-",
    "computeCloudServices": "cld-",
    "computeDiskEncryptionSets": "des",
    "computeDisks": "disk",
    "computeDisksOs": "osdisk",
    "computeGalleries": "gal",
    "computeSnapshots": "snap-",
    "computeVirtualMachines": "vm",
    "computeVirtualMachineScaleSets": "vmss-",
    "containerInstanceContainerGroups": "ci",
    "containerRegistryRegistries": "cr",
    "containerServiceManagedClusters": "aks-",
    "databricksWorkspaces": "dbw-",
    "dataFactoryFactories": "adf-",
    "dataLakeAnalyticsAccounts": "dla",
    "dataLakeStoreAccounts": "dls",
    "dataMigrationServices": "dms-",
    "dBforMySQLServers": "mysql-",
    "devicesIotHubs": "iot-",
    "devicesProvisioningServices": "provs-",
    "devicesProvisioningServicesCertificates": "pcert-",
    "documentDBDatabaseAccounts": "cosmos-",
    "eventGridDomains": "evgd-",
    "eventGridDomainsTopics": "evgt-",
    "eventGridEventSubscriptions": "evgs-",
    "eventHubNamespaces": "evhns-",
    "eventHubNamespacesEventHubs": "evh-",
    "hdInsightClustersHadoop": "hadoop-",
    "hdInsightClustersHbase": "hbase-",
    "hdInsightClustersKafka": "kafka-",
    "hdInsightClustersMl": "mls-",
    "hdInsightClustersSpark": "spark-",
    "hdInsightClustersStorm": "storm-",
    "hybridComputeMachines": "arcs-",
    "insightsActionGroups": "ag-",
    "insightsComponents": "appi-",
    "keyVaultVaults": "kv-",
    "kubernetesConnectedClusters": "arck",
    "kustoClusters": "dec",
    "kustoClustersDatabases": "dedb",
    "loadTesting": "lt-",
    "logicIntegrationAccounts": "ia-",
    "logicWorkflows": "logic-",
    "machineLearningServicesWorkspaces": "mlw-",
    "managedIdentityUserAssignedIdentities": "id-",
    "managementManagementGroups": "mg-",
    "migrateAssessmentProjects": "migr-",
    "networkApplicationGateways": "agw-",
    "networkApplicationSecurityGroups": "asg-",
    "networkAzureFirewalls": "afw-",
    "networkBastionHosts": "bas-",
    "networkConnections": "con-",
    "networkDnsZones": "dnsz-",
    "networkExpressRouteCircuits": "erc-",
    "networkFirewallPolicies": "afwp-",
    "networkFirewallPoliciesWebApplication": "waf",
    "networkFirewallPoliciesRuleGroups": "wafrg",
    "networkFrontDoors": "fd-",
    "networkFrontdoorWebApplicationFirewallPolicies": "fdfp-",
    "networkLoadBalancersExternal": "lbe-",
    "networkLoadBalancersInternal": "lbi-",
    "networkLoadBalancersInboundNatRules": "rule-",
    "networkLocalNetworkGateways": "lgw-",
    "networkNatGateways": "ng-",
    "networkNetworkInterfaces": "nic-",
    "networkNetworkSecurityGroups": "nsg-",
    "networkNetworkSecurityGroupsSecurityRules": "nsgsr-",
    "networkNetworkWatchers": "nw-",
    "networkPrivateDnsZones": "pdnsz-",
    "networkPrivateLinkServices": "pl-",
    "networkPublicIPAddresses": "pip-",
    "networkPublicIPPrefixes": "ippre-",
    "networkRouteFilters": "rf-",
    "networkRouteTables": "rt-",
    "networkRouteTablesRoutes": "udr-",
    "networkTrafficManagerProfiles": "traf-",
    "networkVirtualNetworkGateways": "vgw-",
    "networkVirtualNetworks": "vnet-",
    "networkVirtualNetworksSubnets": "snet-",
    "networkVirtualNetworksVirtualNetworkPeerings": "peer-",
    "networkVirtualWans": "vwan-",
    "networkVpnGateways": "vpng-",
    "networkVpnGatewaysVpnConnections": "vcn-",
    "networkVpnGatewaysVpnSites": "vst-",
    "notificationHubsNamespaces": "ntfns-",
    "notificationHubsNamespacesNotificationHubs": "ntf-",
    "operationalInsightsWorkspaces": "log-",
    "portalDashboards": "dash-",
    "postgreSQLServers": "psql-",
    "postgreSQLServersDatabases": "psqldb-",
    "powerBIDedicatedCapacities": "pbi-",
    "purviewAccounts": "pview-",
    "recoveryServicesVaults": "rsv-",
    "resourcesResourceGroups": "rg-",
    "searchSearchServices": "srch-",
    "serviceBusNamespaces": "sb-",
    "serviceBusNamespacesQueues": "sbq-",
    "serviceBusNamespacesTopics": "sbt-",
    "serviceEndPointPolicies": "se-",
    "serviceFabricClusters": "sf-",
    "signalRServiceSignalR": "sigr",
    "sqlManagedInstances": "sqlmi-",
    "sqlServers": "sql-",
    "sqlServersDataWarehouse": "sqldw-",
    "sqlServersDatabases": "sqldb-",
    "sqlServersDatabasesStretch": "sqlstrdb-",
    "storageStorageAccounts": "st",
    "storageStorageAccountsVm": "stvm",
    "storSimpleManagers": "ssimp",
    "streamAnalyticsCluster": "asa-",
    "synapseWorkspaces": "syn",
    "synapseWorkspacesAnalyticsWorkspaces": "synw",
    "synapseWorkspacesSqlPoolsDedicated": "syndp",
    "synapseWorkspacesSqlPoolsSpark": "synsp",
    "timeSeriesInsightsEnvironments": "tsi-",
    "webServerFarms": "plan-",
    "webSitesAppService": "app-",
    "webSitesAppServiceEnvironment": "ase-",
    "webSitesFunctions": "func-",
    "webStaticSites": "stapp-"
}

```

### infra\main.parameters.json

```json
{
    "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
      "environmentName": {
        "value": "${AZURE_ENV_NAME}"
      },
      "location": {
        "value": "${AZURE_LOCATION}"
      },
      "principalId": {
        "value": "${AZURE_PRINCIPAL_ID}"
      },
      "dbAdminPassword": {
        "value": "$(secretOrRandomPassword ${AZURE_KEY_VAULT_NAME} dbAdminPassword)"
      },
      "dbAppUserPassword": {
        "value": "$(secretOrRandomPassword ${AZURE_KEY_VAULT_NAME} dbAppUserPassword)"
      }
    }
}

```

### src\AppHost\AppHost.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

  <Sdk Name="Aspire.AppHost.Sdk" Version="9.0.0" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireHost>true</IsAspireHost>
    <RootNamespace>YummyZoom.AppHost</RootNamespace>
    <AssemblyName>YummyZoom.AppHost</AssemblyName>
    <BuildingWithAspire>true</BuildingWithAspire>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" />
    <PackageReference Include="Aspire.Hosting.PostgreSQL" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Web\Web.csproj" />
  </ItemGroup>

</Project>

```

### src\AppHost\appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

```

### src\AppHost\appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  }
}

```

### src\AppHost\Program.cs

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var databaseName = "YummyZoomDb";

var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    // Set the name of the default database to auto-create on container startup.
    .WithEnvironment("POSTGRES_DB", databaseName);

var database = postgres.AddDatabase(databaseName);

builder.AddProject<Projects.Web>("web")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();

```

### src\AppHost\Properties\launchSettings.json

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:17078;http://localhost:15010",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21118",
        "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22071"
      }
    },
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:15010",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "http://localhost:19152",
        "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "http://localhost:20152"
      }
    }
  }
}

```

### src\Application\Application.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>YummyZoom.Application</RootNamespace>
    <AssemblyName>YummyZoom.Application</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Ardalis.GuardClauses" />
    <PackageReference Include="AutoMapper" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\SharedKernel\SharedKernel.csproj" />
  </ItemGroup>

</Project>

```

### src\Application\DependencyInjection.cs

```csharp
﻿using System.Reflection;
using YummyZoom.Application.Common.Behaviours;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace YummyZoom.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        });
    }
}

```

### src\Application\GlobalUsings.cs

```csharp
﻿global using Ardalis.GuardClauses;
global using AutoMapper;
global using AutoMapper.QueryableExtensions;
global using Microsoft.EntityFrameworkCore;
global using FluentValidation;
global using MediatR;

```

### src\Application\Common\Behaviours\AuthorizationBehaviour.cs

```csharp
﻿using System.Reflection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Security;

namespace YummyZoom.Application.Common.Behaviours;

public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public AuthorizationBehaviour(
        IUser user,
        IIdentityService identityService)
    {
        _user = user;
        _identityService = identityService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>();

        IEnumerable<AuthorizeAttribute> attributes = authorizeAttributes as AuthorizeAttribute[] ?? authorizeAttributes.ToArray();
        if (attributes.Any())
        {
            // Must be authenticated user
            if (_user.Id == null)
            {
                throw new UnauthorizedAccessException();
            }

            // Role-based authorization
            var authorizeAttributesWithRoles = attributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles));

            if (authorizeAttributesWithRoles.Any())
            {
                var authorized = false;

                foreach (var roles in authorizeAttributesWithRoles.Select(a => a.Roles.Split(',')))
                {
                    foreach (var role in roles)
                    {
                        var isInRole = await _identityService.IsInRoleAsync(_user.Id, role.Trim());
                        if (isInRole)
                        {
                            authorized = true;
                            break;
                        }
                    }
                }

                // Must be a member of at least one role in roles
                if (!authorized)
                {
                    throw new ForbiddenAccessException();
                }
            }

            // Policy-based authorization
            var authorizeAttributesWithPolicies = attributes.Where(a => !string.IsNullOrWhiteSpace(a.Policy));
            if (authorizeAttributesWithPolicies.Any())
            {
                foreach (var policy in authorizeAttributesWithPolicies.Select(a => a.Policy))
                {
                    var authorized = await _identityService.AuthorizeAsync(_user.Id, policy);

                    if (!authorized)
                    {
                        throw new ForbiddenAccessException();
                    }
                }
            }
        }

        // User is authorized / authorization not required
        return await next();
    }
}

```

### src\Application\Common\Behaviours\LoggingBehaviour.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace YummyZoom.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger _logger;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public LoggingBehaviour(ILogger<TRequest> logger, IUser user, IIdentityService identityService)
    {
        _logger = logger;
        _user = user;
        _identityService = identityService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userIdString = _user.Id ?? string.Empty; 
        string? userName = string.Empty;

        if (!string.IsNullOrEmpty(userIdString))
        {
            if (Guid.TryParse(userIdString, out var userGuid))
            {
                userName = await _identityService.GetUserNameAsync(userGuid.ToString());
            }
            else
            {
                _logger.LogWarning("UserId is not a valid Guid: {UserId}", userIdString);
            }
        }

        _logger.LogInformation("YummyZoom Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName, userIdString, userName, request);

        return await next();
    }
}

```

### src\Application\Common\Behaviours\PerformanceBehaviour.cs

```csharp
﻿using System.Diagnostics;
using YummyZoom.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace YummyZoom.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public PerformanceBehaviour(
        ILogger<TRequest> logger,
        IUser user,
        IIdentityService identityService)
    {
        _timer = new Stopwatch();

        _logger = logger;
        _user = user;
        _identityService = identityService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _user.Id ?? string.Empty;
            var userName = string.Empty;

            if (!string.IsNullOrEmpty(userId))
            {
                userName = await _identityService.GetUserNameAsync(userId);
            }

            _logger.LogWarning("YummyZoom Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@UserId} {@UserName} {@Request}",
                requestName, elapsedMilliseconds, userId, userName, request);
        }

        return response;
    }
}

```

### src\Application\Common\Behaviours\UnhandledExceptionBehaviour.cs

```csharp
﻿using Microsoft.Extensions.Logging;

namespace YummyZoom.Application.Common.Behaviours;

public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger;

    public UnhandledExceptionBehaviour(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogError(ex, "YummyZoom Request: Unhandled Exception for Request {Name} {@Request}", requestName, request);

            throw;
        }
    }
}

```

### src\Application\Common\Behaviours\ValidationBehaviour.cs

```csharp
﻿using ValidationException = YummyZoom.Application.Common.Exceptions.ValidationException;

namespace YummyZoom.Application.Common.Behaviours;

public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
     where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v =>
                    v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
                throw new ValidationException(failures);
        }
        return await next();
    }
}

```

### src\Application\Common\Exceptions\ForbiddenAccessException.cs

```csharp
﻿namespace YummyZoom.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base() { }
}

```

### src\Application\Common\Exceptions\ValidationException.cs

```csharp
﻿using FluentValidation.Results;

namespace YummyZoom.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}

```

### src\Application\Common\Interfaces\IApplicationDbContext.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate;

namespace YummyZoom.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

```

### src\Application\Common\Interfaces\IIdentityService.cs

```csharp
﻿using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<Result<Guid>> CreateUserAsync(string email, string password, string name);

    Task<Result> UpdateEmailAsync(string userId, string newEmail);

    Task<Result> UpdateProfileAsync(string userId, string name, string? phoneNumber);

    Task<Result> DeleteUserAsync(string userId);

    Task<Result> AddUserToRoleAsync(Guid userId, string role);

    Task<Result> RemoveUserFromRoleAsync(Guid userId, string role);
}

```

### src\Application\Common\Interfaces\IUser.cs

```csharp
﻿using YummyZoom.Domain.UserAggregate.ValueObjects; 

namespace YummyZoom.Application.Common.Interfaces;

public interface IUser
{
    string? Id { get; } 
    UserId? DomainId { get; } 
}

```

### src\Application\Common\Interfaces\IUserAggregateRepository.cs

```csharp
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces;

public interface IUserAggregateRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default); 
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    // Potentially: Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken = default);
}

```

### src\Application\Common\Mappings\MappingExtensions.cs

```csharp
﻿using YummyZoom.Application.Common.Models;

namespace YummyZoom.Application.Common.Mappings;

public static class MappingExtensions
{
    public static Task<PaginatedList<TDestination>> PaginatedListAsync<TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize) where TDestination : class
        => PaginatedList<TDestination>.CreateAsync(queryable.AsNoTracking(), pageNumber, pageSize);

    public static Task<List<TDestination>> ProjectToListAsync<TDestination>(this IQueryable queryable, IConfigurationProvider configuration) where TDestination : class
        => queryable.ProjectTo<TDestination>(configuration).AsNoTracking().ToListAsync();
}

```

### src\Application\Common\Models\LookupDto.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Application.Common.Models;

public class LookupDto
{
    public int Id { get; init; }

    public string? Title { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoList, LookupDto>();
            CreateMap<TodoItem, LookupDto>();
        }
    }
}

```

### src\Application\Common\Models\PaginatedList.cs

```csharp
﻿namespace YummyZoom.Application.Common.Models;

public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}

```

### src\Application\Common\Models\Result.cs

```csharp
﻿namespace YummyZoom.Application.Common.Models;

public class Result
{
    internal Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
    }

    public bool Succeeded { get; init; }

    public string[] Errors { get; init; }

    public static Result Success()
    {
        return new Result(true, Array.Empty<string>());
    }

    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, errors);
    }
}

```

### src\Application\Common\Security\AuthorizeAttribute.cs

```csharp
﻿namespace YummyZoom.Application.Common.Security;

/// <summary>
/// Specifies the class this attribute is applied to requires authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class. 
    /// </summary>
    public AuthorizeAttribute() { }

    /// <summary>
    /// Gets or sets a comma delimited list of roles that are allowed to access the resource.
    /// </summary>
    public string Roles { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy name that determines access to the resource.
    /// </summary>
    public string Policy { get; set; } = string.Empty;
}

```

### src\Application\TodoItems\Commands\CreateTodoItem\CreateTodoItem.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoItems.Commands.CreateTodoItem;

public record CreateTodoItemCommand : IRequest<Result<Guid>>
{
    public Guid ListId { get; init; }
    public string? Title { get; init; }
    public string? Note { get; init; }
    public PriorityLevel Priority { get; init; }
    public DateTime? Reminder { get; init; }
}

public class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var listId = TodoListId.Create(request.ListId);
        var todoList = await _context.TodoLists
            .FindAsync([listId], cancellationToken);

        if (todoList is null)
            return Result.Failure<Guid>(TodoListErrors.NotFound(request.ListId));

        var todoItem = TodoItem.Create(
            request.Title,
            request.Note,
            request.Priority,
            request.Reminder);

        todoList.AddItem(todoItem);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(todoItem.Id.Value);
    }
}

```

### src\Application\TodoItems\Commands\CreateTodoItem\CreateTodoItemCommandValidator.cs

```csharp
﻿namespace YummyZoom.Application.TodoItems.Commands.CreateTodoItem;

public class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(v => v.Title)
            .MaximumLength(200)
            .NotEmpty();
    }
}

```

### src\Application\TodoItems\Commands\DeleteTodoItem\DeleteTodoItem.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TodoListAggregate.Events;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoItems.Commands.DeleteTodoItem;

public record DeleteTodoItemCommand(Guid ListId, Guid Id) : IRequest<Result<Unit>>;

public class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public DeleteTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoList = await _context.TodoLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == TodoListId.Create(request.ListId), cancellationToken);

        if (todoList is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.ListId));

        var item = todoList.Items.FirstOrDefault(i => i.Id.Value == request.Id);
        
        if (item is null)
            return Result.Failure<Unit>(TodoItemErrors.NotFound(request.Id));

        todoList.RemoveItem(item);
        item.AddDomainEvent(new TodoItemDeletedEvent(item));

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}

```

### src\Application\TodoItems\Commands\UpdateTodoItem\UpdateTodoItem.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;

public record UpdateTodoItemCommand : IRequest<Result<Unit>>
{
    public Guid Id { get; init; }
    public Guid ListId { get; init; }
    public string? Title { get; init; }
    public bool IsDone { get; init; }
}

public class UpdateTodoItemCommandHandler : IRequestHandler<UpdateTodoItemCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(UpdateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoList = await _context.TodoLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == TodoListId.Create(request.ListId), cancellationToken);

        if (todoList is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.ListId));

        var item = todoList.Items.FirstOrDefault(i => i.Id.Value == request.Id);
        
        if (item is null)
            return Result.Failure<Unit>(TodoItemErrors.NotFound(request.Id));

        if (request.Title is not null)
            item.UpdateTitle(request.Title);

        if (request.IsDone)
            item.Complete();
        else
            item.Incomplete();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}

```

### src\Application\TodoItems\Commands\UpdateTodoItem\UpdateTodoItemCommandValidator.cs

```csharp
﻿namespace YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;

public class UpdateTodoItemCommandValidator : AbstractValidator<UpdateTodoItemCommand>
{
    public UpdateTodoItemCommandValidator()
    {
        RuleFor(v => v.Title)
            .MaximumLength(200)
            .NotEmpty();
    }
}

```

### src\Application\TodoItems\Commands\UpdateTodoItemDetail\UpdateTodoItemDetail.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoItems.Commands.UpdateTodoItemDetail;

public record UpdateTodoItemDetailCommand : IRequest<Result<Unit>>
{
    public Guid ListId { get; init; }
    public Guid Id { get; init; }
    public PriorityLevel Priority { get; init; }
    public string? Note { get; init; }
    public DateTime? Reminder { get; init; }
}

public class UpdateTodoItemDetailCommandHandler : IRequestHandler<UpdateTodoItemDetailCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoItemDetailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(UpdateTodoItemDetailCommand request, CancellationToken cancellationToken)
    {
        var todoList = await _context.TodoLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == TodoListId.Create(request.ListId), cancellationToken);

        if (todoList is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.ListId));

        var item = todoList.Items.FirstOrDefault(i => i.Id.Value == request.Id);
        
        if (item is null)
            return Result.Failure<Unit>(TodoItemErrors.NotFound(request.Id));

        item.UpdatePriority(request.Priority);
        item.UpdateNote(request.Note);
        item.UpdateReminder(request.Reminder);

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}

```

### src\Application\TodoItems\EventHandlers\TodoItemCompletedEventHandler.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate.Events;
using Microsoft.Extensions.Logging;

namespace YummyZoom.Application.TodoItems.EventHandlers;

public class TodoItemCompletedEventHandler : INotificationHandler<TodoItemCompletedEvent>
{
    private readonly ILogger<TodoItemCompletedEventHandler> _logger;

    public TodoItemCompletedEventHandler(ILogger<TodoItemCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TodoItemCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("YummyZoom Domain Event: {DomainEvent}", notification.GetType().Name);

        return Task.CompletedTask;
    }
}

```

### src\Application\TodoItems\EventHandlers\TodoItemCreatedEventHandler.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate.Events;
using Microsoft.Extensions.Logging;

namespace YummyZoom.Application.TodoItems.EventHandlers;

public class TodoItemCreatedEventHandler : INotificationHandler<TodoItemCreatedEvent>
{
    private readonly ILogger<TodoItemCreatedEventHandler> _logger;

    public TodoItemCreatedEventHandler(ILogger<TodoItemCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TodoItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("YummyZoom Domain Event: {DomainEvent}", notification.GetType().Name);

        return Task.CompletedTask;
    }
}

```

### src\Application\TodoItems\Queries\GetTodoItemsWithPagination\GetTodoItemsWithPagination.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;

public record GetTodoItemsWithPaginationQuery : IRequest<Result<PaginatedList<TodoItemBriefDto>>>
{
    public Guid ListId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetTodoItemsWithPaginationQueryHandler : IRequestHandler<GetTodoItemsWithPaginationQuery, Result<PaginatedList<TodoItemBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTodoItemsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<TodoItemBriefDto>>> Handle(GetTodoItemsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var listId = TodoListId.Create(request.ListId);
        
        // Check if the list exists
        var listExists = await _context.TodoLists
            .AsNoTracking()
            .AnyAsync(l => l.Id == listId, cancellationToken);

        if (!listExists)
            return Result.Failure<PaginatedList<TodoItemBriefDto>>(TodoListErrors.NotFound(request.ListId));

        // Query for the items with projection and apply pagination directly on the IQueryable
        var itemsQuery = _context.TodoLists
            .AsNoTracking()
            .Where(l => l.Id == listId)
            .SelectMany(l => l.Items)
            .OrderBy(x => x.Title)
            .ProjectTo<TodoItemBriefDto>(_mapper.ConfigurationProvider);

        var paginatedList = await PaginatedList<TodoItemBriefDto>.CreateAsync(
            itemsQuery, 
            request.PageNumber, 
            request.PageSize);
        
        return Result.Success(paginatedList);
    }
}

```

### src\Application\TodoItems\Queries\GetTodoItemsWithPagination\GetTodoItemsWithPaginationQueryValidator.cs

```csharp
﻿namespace YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;

public class GetTodoItemsWithPaginationQueryValidator : AbstractValidator<GetTodoItemsWithPaginationQuery>
{
    public GetTodoItemsWithPaginationQueryValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("ListId is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}

```

### src\Application\TodoItems\Queries\GetTodoItemsWithPagination\TodoItemBriefDto.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;

public class TodoItemBriefDto
{
    public Guid Id { get; init; }

    public string? Title { get; init; }

    public bool IsDone { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoItem, TodoItemBriefDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.Value))
                .ForMember(d => d.IsDone, opt => opt.MapFrom(s => s.IsDone));
        }
    }
}

```

### src\Application\TodoLists\Commands\CreateTodoList\CreateTodoList.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoLists.Commands.CreateTodoList;

public record CreateTodoListCommand : IRequest<Result<Guid>>
{
    public string? Title { get; init; }
}

public class CreateTodoListCommandHandler : IRequestHandler<CreateTodoListCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = TodoList.Create(request.Title ?? string.Empty, Color.White);

        _context.TodoLists.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id.Value);
    }
}

```

### src\Application\TodoLists\Commands\CreateTodoList\CreateTodoListCommandValidator.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;

namespace YummyZoom.Application.TodoLists.Commands.CreateTodoList;

public class CreateTodoListCommandValidator : AbstractValidator<CreateTodoListCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateTodoListCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Title)
            .NotEmpty()
            .MaximumLength(200)
            .MustAsync(BeUniqueTitle)
                .WithMessage("'{PropertyName}' must be unique.")
                .WithErrorCode("Unique");
    }

    public async Task<bool> BeUniqueTitle(string title, CancellationToken cancellationToken)
    {
        return !await _context.TodoLists
            .AnyAsync(l => l.Title == title, cancellationToken);
    }
}

```

### src\Application\TodoLists\Commands\DeleteTodoList\DeleteTodoList.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoLists.Commands.DeleteTodoList;

public record DeleteTodoListCommand(Guid Id) : IRequest<Result<Unit>>;

public class DeleteTodoListCommandHandler : IRequestHandler<DeleteTodoListCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public DeleteTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(DeleteTodoListCommand request, CancellationToken cancellationToken)
    {
        var id = TodoListId.Create(request.Id);
        var entity = await _context.TodoLists
            .Where(l => l.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.Id));

        _context.TodoLists.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}

```

### src\Application\TodoLists\Commands\PurgeTodoLists\PurgeTodoLists.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TodoLists.Commands.PurgeTodoLists;

[Authorize(Roles = Roles.Administrator)]
[Authorize(Policy = Policies.CanPurge)]
public record PurgeTodoListsCommand : IRequest<Result<Unit>>;

public class PurgeTodoListsCommandHandler : IRequestHandler<PurgeTodoListsCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public PurgeTodoListsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(PurgeTodoListsCommand request, CancellationToken cancellationToken)
    {
        _context.TodoLists.RemoveRange(_context.TodoLists);

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}

```

### src\Application\TodoLists\Commands\UpdateTodoList\UpdateTodoList.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoLists.Commands.UpdateTodoList;

public record UpdateTodoListCommand : IRequest<Result<Unit>>
{
    public Guid Id { get; init; }

    public string? Title { get; init; }
}

public class UpdateTodoListCommandHandler : IRequestHandler<UpdateTodoListCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(UpdateTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoLists
            .FindAsync([TodoListId.Create(request.Id)], cancellationToken);

        if (entity is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.Id));

        entity.UpdateTitle(request.Title ?? string.Empty);

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}

```

### src\Application\TodoLists\Commands\UpdateTodoList\UpdateTodoListCommandValidator.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;

namespace YummyZoom.Application.TodoLists.Commands.UpdateTodoList;

public class UpdateTodoListCommandValidator : AbstractValidator<UpdateTodoListCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoListCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Title)
            .NotEmpty()
            .MaximumLength(200)
            .MustAsync(BeUniqueTitle)
                .WithMessage("'{PropertyName}' must be unique.")
                .WithErrorCode("Unique");
    }

    public async Task<bool> BeUniqueTitle(UpdateTodoListCommand model, string title, CancellationToken cancellationToken)
    {
        var id = YummyZoom.Domain.TodoListAggregate.ValueObjects.TodoListId.Create(model.Id);
        return !await _context.TodoLists
            .Where(l => l.Id != id)
            .AnyAsync(l => l.Title == title, cancellationToken);
    }
}

```

### src\Application\TodoLists\Queries\GetTodos\GetTodos.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.TodoLists.Queries.GetTodos;

[Authorize]
public record GetTodosQuery : IRequest<Result<TodosVm>>;

public class GetTodosQueryHandler : IRequestHandler<GetTodosQuery, Result<TodosVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTodosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<TodosVm>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        var vm = new TodosVm
        {
            PriorityLevels = Enum.GetValues<PriorityLevel>()
                .Cast<PriorityLevel>()
                .Select(p => new LookupDto { Id = (int)p, Title = p.ToString() })
                .ToList(),

            Lists = await _context.TodoLists
                .AsNoTracking()
                .ProjectTo<TodoListDto>(_mapper.ConfigurationProvider)
                .OrderBy(t => t.Title)
                .ToListAsync(cancellationToken)
        };
        
        return Result.Success(vm);
    }
}

```

### src\Application\TodoLists\Queries\GetTodos\TodoItemDto.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Application.TodoLists.Queries.GetTodos;

public class TodoItemDto
{
    public Guid Id { get; init; }

    public string? Title { get; init; }

    public bool Done { get; init; }

    public int Priority { get; init; }

    public string? Note { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoItem, TodoItemDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.Value))
                .ForMember(d => d.Priority, opt => opt.MapFrom(s => (int)s.Priority));
        }
    }
}

```

### src\Application\TodoLists\Queries\GetTodos\TodoListDto.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate;

namespace YummyZoom.Application.TodoLists.Queries.GetTodos;

public class TodoListDto
{
    public TodoListDto()
    {
        Items = Array.Empty<TodoItemDto>();
    }

    public Guid Id { get; init; }

    public string? Title { get; init; }

    public string? Colour { get; init; }

    public IReadOnlyCollection<TodoItemDto> Items { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoList, TodoListDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.Value));
        }
    }
}

```

### src\Application\TodoLists\Queries\GetTodos\TodosVm.cs

```csharp
﻿using YummyZoom.Application.Common.Models;

namespace YummyZoom.Application.TodoLists.Queries.GetTodos;

public class TodosVm
{
    public IReadOnlyCollection<LookupDto> PriorityLevels { get; init; } = Array.Empty<LookupDto>();

    public IReadOnlyCollection<TodoListDto> Lists { get; init; } = Array.Empty<TodoListDto>();
}

```

### src\Application\Users\Commands\AssignRoleToUser\AssignRoleToUserCommand.cs

```csharp
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Users.Commands.AssignRoleToUser;

[Authorize(Roles = Roles.Administrator)]
public record AssignRoleToUserCommand(
    Guid UserId,
    string RoleName,
    string? TargetEntityId,
    string? TargetEntityType) : IRequest<Result>;

```

### src\Application\Users\Commands\AssignRoleToUser\AssignRoleToUserCommandHandler.cs

```csharp
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand, Result>
{
    private readonly IUserAggregateRepository _userAggregateRepository;

    public AssignRoleToUserCommandHandler(IUserAggregateRepository userAggregateRepository)
    {
        _userAggregateRepository = userAggregateRepository;
    }

    public async Task<Result> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var userId = UserId.Create(request.UserId);

        var userAggregate = await _userAggregateRepository.GetByIdAsync(userId, cancellationToken);
        if (userAggregate is null)
        {
            return Result.Failure(UserErrors.UserNotFound(request.UserId));
        }

        var roleAssignmentResult = RoleAssignment.Create(
            request.RoleName,
            request.TargetEntityId,
            request.TargetEntityType);

        if (roleAssignmentResult.IsFailure)
        {
            return Result.Failure(roleAssignmentResult.Error);
        }

        var addRoleResult = userAggregate.AddRole(roleAssignmentResult.Value);
        if (addRoleResult.IsFailure)
        {
            return Result.Failure(addRoleResult.Error);
        }

        await _userAggregateRepository.UpdateAsync(userAggregate, cancellationToken);

        return Result.Success();
    }
}

```

### src\Application\Users\Commands\AssignRoleToUser\AssignRoleToUserCommandValidator.cs

```csharp
using FluentValidation;

namespace YummyZoom.Application.Users.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty();

        RuleFor(v => v.RoleName)
            .NotEmpty();

        // If TargetEntityId is provided, TargetEntityType must also be provided, and vice versa.
        RuleFor(v => v)
            .Must(v =>
                (string.IsNullOrWhiteSpace(v.TargetEntityId) && string.IsNullOrWhiteSpace(v.TargetEntityType)) ||
                (!string.IsNullOrWhiteSpace(v.TargetEntityId) && !string.IsNullOrWhiteSpace(v.TargetEntityType)))
            .WithMessage("If TargetEntityId is provided, TargetEntityType must also be provided, and vice versa.");
    }
}

```

### src\Application\Users\Commands\RegisterUser\RegisterUserCommand.cs

```csharp
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand : IRequest<Result<Guid>>
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? Name { get; init; }
}

```

### src\Application\Users\Commands\RegisterUser\RegisterUserCommandHandler.cs

```csharp
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IIdentityService _identityService;

    public RegisterUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.CreateUserAsync(
            request.Email!,
            request.Password!,
            request.Name!);

        return result;
    }
}

```

### src\Application\Users\Commands\RegisterUser\RegisterUserCommandValidator.cs

```csharp
namespace YummyZoom.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(v => v.Name)
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
            .NotEmpty().WithMessage("Name is required.");

        RuleFor(v => v.Email)
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.")
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
            // Add more complex password rules (uppercase, lowercase, number, symbol) if required by identity settings
    }
}

```

### src\Application\Users\Commands\RegisterUser\RegisterUserResponse.cs

```csharp
namespace YummyZoom.Application.Users.Commands.RegisterUser;

public class RegisterUserResponse
{
    public Guid UserId { get; set; }
}

```

### src\Application\Users\Commands\RemoveRoleFromUser\RemoveRoleFromUserCommand.cs

```csharp
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Users.Commands.RemoveRoleFromUser;

[Authorize(Roles = Roles.Administrator)]
public record RemoveRoleFromUserCommand(
    Guid UserId,
    string RoleName,
    string? TargetEntityId,
    string? TargetEntityType) : IRequest<Result>;

```

### src\Application\Users\Commands\RemoveRoleFromUser\RemoveRoleFromUserCommandHandler.cs

```csharp
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand, Result>
{
    private readonly IUserAggregateRepository _userAggregateRepository;

    public RemoveRoleFromUserCommandHandler(IUserAggregateRepository userAggregateRepository)
    {
        _userAggregateRepository = userAggregateRepository;
    }

    public async Task<Result> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var userId = UserId.Create(request.UserId);

        var userAggregate = await _userAggregateRepository.GetByIdAsync(userId, cancellationToken);
        if (userAggregate is null)
        {
            return Result.Failure(Domain.UserAggregate.Errors.UserErrors.UserNotFound(request.UserId));
        }

        var removeRoleResult = userAggregate.RemoveRole(
            request.RoleName,
            request.TargetEntityId,
            request.TargetEntityType);

        if (removeRoleResult.IsFailure)
        {
            return Result.Failure(removeRoleResult.Error);
        }

        await _userAggregateRepository.UpdateAsync(userAggregate, cancellationToken);

        return Result.Success();
    }
}

```

### src\Application\Users\Commands\RemoveRoleFromUser\RemoveRoleFromUserCommandValidator.cs

```csharp
using FluentValidation;

namespace YummyZoom.Application.Users.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandValidator : AbstractValidator<RemoveRoleFromUserCommand>
{
    public RemoveRoleFromUserCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty();

        RuleFor(v => v.RoleName)
            .NotEmpty();

        // If TargetEntityId is provided, TargetEntityType must also be provided, and vice versa.
        RuleFor(v => v)
            .Must(v =>
                (string.IsNullOrWhiteSpace(v.TargetEntityId) && string.IsNullOrWhiteSpace(v.TargetEntityType)) ||
                (!string.IsNullOrWhiteSpace(v.TargetEntityId) && !string.IsNullOrWhiteSpace(v.TargetEntityType)))
            .WithMessage("If TargetEntityId is provided, TargetEntityType must also be provided, and vice versa.");
    }
}

```

### src\Application\Users\EventHandlers\RoleAssignmentAddedToUserEventHandler.cs

```csharp
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace YummyZoom.Application.Users.EventHandlers;

public class RoleAssignmentAddedToUserEventHandler : INotificationHandler<RoleAssignmentAddedToUserEvent>
{
    private readonly IIdentityService _identityService;

    public RoleAssignmentAddedToUserEventHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(RoleAssignmentAddedToUserEvent notification, CancellationToken cancellationToken)
    {
        // Translate domain RoleAssignment to Identity role string
        var identityRole = notification.RoleAssignment.RoleName;

        // Call IdentityService to add the user to the Identity role
        await _identityService.AddUserToRoleAsync(notification.UserId.Value, identityRole);
    }
}

```

### src\Application\Users\EventHandlers\RoleAssignmentRemovedFromUserEventHandler.cs

```csharp
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace YummyZoom.Application.Users.EventHandlers;

public class RoleAssignmentRemovedFromUserEventHandler : INotificationHandler<RoleAssignmentRemovedFromUserEvent>
{
    private readonly IIdentityService _identityService;

    public RoleAssignmentRemovedFromUserEventHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(RoleAssignmentRemovedFromUserEvent notification, CancellationToken cancellationToken)
    {
        // Translate domain RoleAssignment to Identity role string
        var identityRole = notification.RoleAssignment.RoleName;

        // Call IdentityService to remove the user from the Identity role
        await _identityService.RemoveUserFromRoleAsync(notification.UserId.Value, identityRole);
    }
}

```

### src\Application\WeatherForecasts\Queries\GetWeatherForecasts\GetWeatherForecastsQuery.cs

```csharp
﻿using YummyZoom.SharedKernel;

namespace YummyZoom.Application.WeatherForecasts.Queries.GetWeatherForecasts;

public record GetWeatherForecastsQuery : IRequest<Result<IEnumerable<WeatherForecast>>>;

public class GetWeatherForecastsQueryHandler : IRequestHandler<GetWeatherForecastsQuery, Result<IEnumerable<WeatherForecast>>>
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<Result<IEnumerable<WeatherForecast>>> Handle(GetWeatherForecastsQuery request, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        var rng = new Random();

        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.UtcNow.AddDays(index),
            TemperatureC = rng.Next(-20, 55),
            Summary = Summaries[rng.Next(Summaries.Length)]
        });
        
        return Result.Success(forecasts);
    }
}

```

### src\Application\WeatherForecasts\Queries\GetWeatherForecasts\WeatherForecast.cs

```csharp
﻿namespace YummyZoom.Application.WeatherForecasts.Queries.GetWeatherForecasts;

public class WeatherForecast
{
    public DateTime Date { get; init; }

    public int TemperatureC { get; init; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; init; }
}

```

### src\Domain\Domain.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>YummyZoom.Domain</RootNamespace>
    <AssemblyName>YummyZoom.Domain</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="ErrorOr" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\SharedKernel\SharedKernel.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Folder Include="Constants\" />
  </ItemGroup>
  
</Project>

```

### src\Domain\GlobalUsings.cs

```csharp
﻿global using YummyZoom.Domain.Common.Models;
global using YummyZoom.Domain.Common.Errors;

```

### src\Domain\Common\Errors\Errors.Authentication.cs

```csharp
﻿using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class Authentication
    {
        public static Error InvalidCredentials => Error.Validation(
            code: "Authentication.InvalidCredentials",
            description: "Invalid email or password.");
    }
}

```

### src\Domain\Common\Errors\Errors.Color.cs

```csharp
using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class Color
    {
        public static Error Unsupported => Error.Validation(
            code: "Color.Unsupported",
            description: "Color is unsupported");
    }
}

```

### src\Domain\Common\Errors\Errors.TodoItem.cs

```csharp
using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class TodoItem
    {
        public static Error InvalidTodoItemId => Error.Validation(
            code: "TodoItem.InvalidId",
            description: "TodoItem ID is invalid");

        public static Error NotFound => Error.NotFound(
            code: "TodoItem.NotFound",
            description: "TodoItem with given ID does not exist");
    }
}

```

### src\Domain\Common\Errors\Errors.TodoList.cs

```csharp
using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class TodoList
    {
        public static Error InvalidTodoListId => Error.Validation(
            code: "TodoList.InvalidId",
            description: "TodoList ID is invalid");

        public static Error NotFound => Error.NotFound(
            code: "TodoList.NotFound",
            description: "TodoList with given ID does not exist");
    }
}

```

### src\Domain\Common\Errors\Errors.User.cs

```csharp
﻿using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class User
    {
        public static Error DuplicateEmail => Error.Conflict(
            code: "User.DuplicateEmail",
            description: "Email is already in use.");
    }
}

```

### src\Domain\Common\Models\AggregateRoot.cs

```csharp
﻿namespace YummyZoom.Domain.Common.Models;

public abstract class AggregateRoot<TId, TIdType> : Entity<TId>
    where TId : AggregateRootId<TIdType>
{
    public new AggregateRootId<TIdType> Id { get; protected set; }

    protected AggregateRoot(TId id)
    {
        Id = id;
    }

#pragma warning disable CS8618
    protected AggregateRoot()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\Common\Models\AggregateRootId.cs

```csharp
﻿namespace YummyZoom.Domain.Common.Models;

public abstract class AggregateRootId<TId> : ValueObject
{
    public abstract TId Value { get; protected set; }
}

```

### src\Domain\Common\Models\Entity.cs

```csharp
﻿namespace YummyZoom.Domain.Common.Models;

public abstract class Entity<TId> : IEquatable<Entity<TId>>, IHasDomainEvent, IAuditableEntity
    where TId : ValueObject
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public TId Id { get; protected set; }
    
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    protected Entity(TId id)
    {
        Id = id;
    }
    
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Id.Equals(entity.Id);
    }

    public bool Equals(Entity<TId>? other)
    {
        return Equals((object?)other);
    }

    public static bool operator ==(Entity<TId> left, Entity<TId> right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId> left, Entity<TId> right)
    {
        return !Equals(left, right);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

#pragma warning disable CS8618
    protected Entity()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\Common\Models\IAuditableEntity.cs

```csharp
namespace YummyZoom.Domain.Common.Models;

public interface IAuditableEntity
{
    DateTimeOffset Created { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset LastModified { get; set; }
    string? LastModifiedBy { get; set; }
}

```

### src\Domain\Common\Models\IDomainEvent.cs

```csharp
﻿using MediatR;

namespace YummyZoom.Domain.Common.Models;

public interface IDomainEvent : INotification
{
}

```

### src\Domain\Common\Models\IHasDomainEvent.cs

```csharp
﻿namespace YummyZoom.Domain.Common.Models;

public interface IHasDomainEvent
{
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }
    public void ClearDomainEvents();
}

```

### src\Domain\Common\Models\ValueObject.cs

```csharp
﻿namespace YummyZoom.Domain.Common.Models;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        return Equals((object?)other);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var valueObject = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
    }
    
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
    
    public static bool operator ==(ValueObject a, ValueObject b)
    {
        return Equals(a, b);
    }

    public static bool operator !=(ValueObject a, ValueObject b)
    {
        return !Equals(a, b);
    }
}

```

### src\Domain\Common\ValueObjects\Address.cs

```csharp

namespace YummyZoom.Domain.Common.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public string Country { get; private set; }
    public string? Label { get; private set; } // Optional
    public string? DeliveryInstructions { get; private set; } // Optional

    private Address(
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string? label,
        string? deliveryInstructions)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
        Label = label;
        DeliveryInstructions = deliveryInstructions;
    }

    public static Address Create(
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string? label = null,
        string? deliveryInstructions = null)
    {
        // Structural/Format validation is handled in higher layers (e.g., Application)
        // Domain layer assumes valid input for creating the Value Object.
        return new Address(
            street,
            city,
            state,
            zipCode,
            country,
            label,
            deliveryInstructions);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
        yield return Label ?? NullPlaceholder; 
        yield return DeliveryInstructions ?? NullPlaceholder; 
    }

    private static readonly object NullPlaceholder = new object();

#pragma warning disable CS8618
    // For EF Core
    private Address()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\Common\ValueObjects\AverageRating.cs

```csharp
﻿namespace YummyZoom.Domain.Common.ValueObjects;

public sealed class AverageRating : ValueObject
{
    private double _value;

    private AverageRating(double value, int numRatings)
    {
        Value = value;
        NumRatings = numRatings;
    }

    public double? Value { get => NumRatings > 0 ? _value : null; private set => _value = value!.Value; }
    public int NumRatings { get; private set; }

    public static AverageRating CreateNew(double rating = 0, int numRatings = 0)
    {
        return new AverageRating(rating, numRatings);
    }

    public void AddNewRating(Rating rating)
    {
        Value = ((Value * NumRatings) + rating.Value) / ++NumRatings;
    }

    public void RemoveRating(Rating rating)
    {
        Value = ((Value * NumRatings) - rating.Value) / --NumRatings;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value!;
        yield return NumRatings;
    }

#pragma warning disable CS8618
    private AverageRating()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\Common\ValueObjects\Rating.cs

```csharp
﻿namespace YummyZoom.Domain.Common.ValueObjects;

public sealed class Rating : ValueObject
{
    public Rating(int value)
    {
        Value = value;
    }

    public int Value { get; private set; }

    public static Rating Create(int value)
    {
        // TODO: Enforce invariants
        return new Rating(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
#pragma warning disable CS8618
    private Rating()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\TodoListAggregate\TodoList.cs

```csharp
﻿﻿using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Domain.TodoListAggregate;

public class TodoList : AggregateRoot<TodoListId, Guid>
{
    private readonly List<TodoItem> _items = [];

    public string? Title { get; private set; }
    public Color Color { get; private set; } 
    public IReadOnlyList<TodoItem> Items => _items.AsReadOnly();

    // Private constructor for creating new instances
    private TodoList(
        TodoListId id,
        string title,
        Color colour) : base(id) 
    {
        Title = title;
        Color = colour;
    }

    // Factory method for creating a new TodoList with a generated ID
    public static TodoList Create(string title, Color? colour = null)
    {
        return new TodoList(TodoListId.CreateUnique(), title, colour ?? Color.White);
    }

    public static TodoList Create(
        TodoListId id,
        string title,
        Color colour)
    {
        return new TodoList(id, title, colour);
    }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateColour(Color colour)
    {
        Color = colour;
    }

    public void AddItem(TodoItem item)
    {
        _items.Add(item);
    }

    public void RemoveItem(TodoItem item)
    {
        _items.Remove(item);
    }

    public void UpdateItem(TodoItem item)
    {
        var index = _items.FindIndex(i => i.Id == item.Id);
        _items[index] = item;
    }

#pragma warning disable CS8618
    // For EF Core
    private TodoList()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\TodoListAggregate\Entities\TodoItem.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Domain.TodoListAggregate.Entities;

public class TodoItem : Entity<TodoItemId>
{
    public string? Title { get; private set; }
    public string? Note { get; private set; }
    public PriorityLevel Priority { get; private set; }
    public DateTime? Reminder { get; private set; }
    public bool IsDone { get; private set; } 

    // Private constructor for creating new instances
    private TodoItem(
        TodoItemId id,
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder) : base(id) // Pass the ID to the base Entity constructor
    {
        Title = title;
        Note = note;
        Priority = priority;
        Reminder = reminder;
        IsDone = false;
    }

    // Factory method for creating a new TodoItem with a generated ID
    public static TodoItem Create(
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder)
    {
        return new TodoItem(TodoItemId.CreateUnique(), title, note, priority, reminder);
    }

    // Factory method for creating a TodoItem with a specific ID (e.g., for hydration from persistence)
    public static TodoItem Create(
        TodoItemId id,
        string? title,
        string? note,
        PriorityLevel priority,
        DateTime? reminder)
    {
        return new TodoItem(id, title, note, priority, reminder);
    }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateNote(string? note)
    {
        Note = note;
    }

    public void UpdatePriority(PriorityLevel priority)
    {
        Priority = priority;
    }

    public void UpdateReminder(DateTime? reminder)
    {
        Reminder = reminder;
    }

    public void Complete()
    {
        if (!IsDone)
        {
            IsDone = true;
        }
    }

    public void Incomplete()
    {
        if (IsDone)
        {
            IsDone = false;
        }
    }

#pragma warning disable CS8618
    // For EF Core
    private TodoItem()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\TodoListAggregate\Enums\PriorityLevel.cs

```csharp
﻿namespace YummyZoom.Domain.TodoListAggregate.Enums;

public enum PriorityLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

```

### src\Domain\TodoListAggregate\Errors\TodoItemErrors.cs

```csharp
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TodoListAggregate.Errors;

public static class TodoItemErrors
{
    public static Error AlreadyCompleted(Guid todoItemId)
    {
        return Error.Problem( 
        "TodoItems.AlreadyCompleted",
        $"The todo item with Id = '{todoItemId}' is already completed.");
    }

    public static Error NotFound(Guid todoItemId)
    {
        return Error.NotFound( 
        "TodoItems.NotFound",
        $"The to-do item with the Id = '{todoItemId}' was not found");
    }

    public static Error InvalidTodoItemId(string todoItemId)
    {
        return Error.Validation( 
        "TodoItems.InvalidTodoItemId",
        $"The to-do item with the Id = '{todoItemId}' is invalid.");
    }
}

```

### src\Domain\TodoListAggregate\Errors\TodoListErrors.cs

```csharp
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TodoListAggregate.Errors;

public static class TodoListErrors
{
    public static Error NotFound(Guid todoListId)
    {
        return Error.NotFound( 
        "TodoLists.NotFound",
        $"The to-do list with the Id = '{todoListId}' was not found");
    }

    public static Error InvalidTodoListId(string todoListId)
    {
        return Error.Validation( 
        "TodoLists.InvalidTodoListId",
        $"The to-do list with the Id = '{todoListId}' is invalid.");
    }
}

```

### src\Domain\TodoListAggregate\Events\TodoItemCompletedEvent.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Domain.TodoListAggregate.Events;

public record TodoItemCompletedEvent(TodoItem Item) : IDomainEvent;

```

### src\Domain\TodoListAggregate\Events\TodoItemCreatedEvent.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Domain.TodoListAggregate.Events;

public record TodoItemCreatedEvent(TodoItem Item) : IDomainEvent;

```

### src\Domain\TodoListAggregate\Events\TodoItemDeletedEvent.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Domain.TodoListAggregate.Events;

public record TodoItemDeletedEvent(TodoItem Item) : IDomainEvent;

```

### src\Domain\TodoListAggregate\ValueObjects\Colour.cs

```csharp
﻿namespace YummyZoom.Domain.TodoListAggregate.ValueObjects;

public class Color : ValueObject
{
    public string Code { get; private set; }

    private Color(string code)
    {
        Code = code;
    }

    public static Color Create(string code)
    {
        return string.IsNullOrWhiteSpace(code) ? new Color("#000000") : new Color(code);
    }

    public static Color White => new("#FFFFFF");

    public static Color Red => new("#FF5733");

    public static Color Orange => new("#FFC300");

    public static Color Yellow => new("#FFFF66");

    public static Color Green => new("#CCFF99");

    public static Color Blue => new("#6666FF");

    public static Color Purple => new("#9966CC");

    public static Color Grey => new("#999999");

    public static implicit operator string(Color colour)
    {
        return colour.ToString();
    }

    public static explicit operator Color(string code)
    {
        return Create(code);
    }

    public override string ToString()
    {
        return Code;
    }

    protected static IEnumerable<Color> SupportedColours
    {
        get
        {
            yield return White;
            yield return Red;
            yield return Orange;
            yield return Yellow;
            yield return Green;
            yield return Blue;
            yield return Purple;
            yield return Grey;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }

#pragma warning disable CS8618
    private Color()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\TodoListAggregate\ValueObjects\TodoItemId.cs

```csharp
using YummyZoom.SharedKernel;
using YummyZoom.Domain.TodoListAggregate.Errors;

namespace YummyZoom.Domain.TodoListAggregate.ValueObjects;

public sealed class TodoItemId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private TodoItemId(Guid value)
    {
        Value = value;
    }

    public static TodoItemId CreateUnique()
    {
        return new TodoItemId(Guid.NewGuid());
    }

    public static TodoItemId Create(Guid value)
    {
        return new TodoItemId(value);
    }

    public static Result<TodoItemId> Create(string value)
    {
        return Guid.TryParse(value, out var guid) ? 
            Result.Success(new TodoItemId(guid)) :
            Result.Failure<TodoItemId>(TodoItemErrors.InvalidTodoItemId(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private TodoItemId()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\TodoListAggregate\ValueObjects\TodoListId.cs

```csharp
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TodoListAggregate.ValueObjects;

public sealed class TodoListId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private TodoListId(Guid value)
    {
        Value = value;
    }

    public static TodoListId CreateUnique()
    {
        return new TodoListId(Guid.NewGuid());
    }

    public static TodoListId Create(Guid value)
    {
        return new TodoListId(value);
    }

    public static Result<TodoListId> Create(string value)
    {
        return Guid.TryParse(value, out var guid)
            ? Result.Success(new TodoListId(guid))
            : Result.Failure<TodoListId>(TodoListErrors.InvalidTodoListId(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private TodoListId()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\UserAggregate\User.cs

```csharp
using YummyZoom.Domain.Common.ValueObjects; 
using YummyZoom.Domain.UserAggregate.Entities; 
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects; 
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Domain.UserAggregate;

public sealed class User : AggregateRoot<UserId, Guid>
{
    private readonly List<RoleAssignment> _userRoles = [];
    private readonly List<Address> _addresses = [];
    private readonly List<PaymentMethod> _paymentMethods = [];

    public string Name { get; private set; }
    public string Email { get; private set; } // Unique identifier for login
    public string? PhoneNumber { get; private set; } // Optional

    public IReadOnlyList<RoleAssignment> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyList<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyList<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    private User(
        UserId id,
        string name,
        string email,
        string? phoneNumber,
        List<RoleAssignment> userRoles,
        List<Address> addresses,
        List<PaymentMethod> paymentMethods)
        : base(id)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        _userRoles = userRoles ?? [];
        _addresses = addresses;
        _paymentMethods = paymentMethods;
    }

    public static Result<User> Create(
        string name,
        string email,
        string? phoneNumber,
        List<RoleAssignment>? userRoles = null) 
    {
        // If userRoles is null or empty, create a default Customer role
        if (userRoles == null || userRoles.Count == 0)
        {
            var customerRoleResult = RoleAssignment.Create(Roles.Customer);
            if (customerRoleResult.IsFailure)
            {
                return Result.Failure<User>(customerRoleResult.Error);
            }
            
            userRoles = [customerRoleResult.Value];
        }

        var user = new User(
            UserId.CreateUnique(),
            name,
            email,
            phoneNumber,
            userRoles,
            [],
            []);

        // Add domain event
        // user.AddDomainEvent(new UserCreated(user));

        return Result.Success(user);
    }
    
    public static Result<User> Create(
        UserId id,
        string name,
        string email,
        string? phoneNumber,
        List<RoleAssignment>? userRoles = null) 
    {
        // If userRoles is null or empty, create a default Customer role
        if (userRoles == null || userRoles.Count == 0)
        {
            var customerRoleResult = RoleAssignment.Create(Roles.Customer);
            if (customerRoleResult.IsFailure)
            {
                return Result.Failure<User>(customerRoleResult.Error);
            }
            
            userRoles = [customerRoleResult.Value];
        }

        var user = new User(
            id,
            name,
            email,
            phoneNumber,
            userRoles,
            [],
            []);

        // Add domain event
        // user.AddDomainEvent(new UserCreated(user));

        return Result.Success(user);
    }

    public Result AddAddress(Address address)
    {
        _addresses.Add(address);
        return Result.Success();
    }

    public Result RemoveAddress(Address address)
    {
        // Remove address based on value equality
        var removed = _addresses.Remove(address);

        if (!removed)
        {
            // Need a more specific error here if Address equality is not sufficient
            // For now, reusing a general error or assuming success if address exists
            // return Result.Failure(UserErrors.AddressNotFound(...)); // If Address had an ID
        }

        return Result.Success();
    }

    public Result AddPaymentMethod(PaymentMethod paymentMethod)
    {
        // Assuming basic validity check is sufficient at this level for now
        // The PaymentMethod object is assumed to be valid and non-null by the time it reaches the domain.

        _paymentMethods.Add(paymentMethod);
        return Result.Success();
    }

    public Result RemovePaymentMethod(PaymentMethodId paymentMethodId)
    {
        var paymentMethodToRemove = _paymentMethods.FirstOrDefault(pm => pm.Id.Value == paymentMethodId.Value);

        if (paymentMethodToRemove is null)
        {
            return Result.Failure(UserErrors.PaymentMethodNotFound(paymentMethodId.Value));
        }

        _paymentMethods.Remove(paymentMethodToRemove);
        return Result.Success();
    }

    public Result UpdateProfile(string name, string? phoneNumber)
    {
        // No domain-specific invariants to check for profile update at this level.
        Name = name;
        PhoneNumber = phoneNumber;
        return Result.Success();
    }

    public Result UpdateEmail(string email)
    {
        // Email is an identifier, so it's handled separately from regular profile updates
        Email = email;
        return Result.Success();
    }

    public Result AddRole(RoleAssignment roleAssignment) 
    {
        // Check if the role assignment already exists based on Value Object equality.
        if (_userRoles.Contains(roleAssignment))
        {
            // Role assignment already exists, consider this a success or return a specific error
            // For now, we'll treat adding an existing role assignment as a successful no-op.
            return Result.Success();
        }

        _userRoles.Add(roleAssignment);
        AddDomainEvent(new RoleAssignmentAddedToUserEvent((UserId)Id, roleAssignment));
        return Result.Success();
    }

    public Result RemoveRole(
        string roleName,
        string? targetEntityId = null,
        string? targetEntityType = null) // Change parameter types
    {
        // Create a RoleAssignment object to use for comparison
        var roleAssignmentToRemoveResult = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);

        if (roleAssignmentToRemoveResult.IsFailure)
        {
            // Handle invalid input for creating RoleAssignment
            return Result.Failure(roleAssignmentToRemoveResult.Error);
        }

        var roleAssignmentToRemove = roleAssignmentToRemoveResult.Value;

        // Invariant: A user must have at least one RoleAssignment.
        if (_userRoles.Count == 1 && _userRoles.Contains(roleAssignmentToRemove))
        {
            return Result.Failure(UserErrors.CannotRemoveLastRole);
        }

        // Find and remove the role assignment
        var removed = _userRoles.Remove(roleAssignmentToRemove);

        if (!removed)
        {
            // Role assignment not found
            return Result.Failure(UserErrors.RoleNotFound(roleName)); 
        }

        AddDomainEvent(new RoleAssignmentRemovedFromUserEvent((UserId)Id, roleAssignmentToRemove));
        return Result.Success();
    }


#pragma warning disable CS8618
    // For EF Core
    private User()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\UserAggregate\Entities\PaymentMethod.cs

```csharp
using YummyZoom.Domain.Common.Models; // Assuming Entity is here
using YummyZoom.Domain.UserAggregate.ValueObjects; // For PaymentMethodId

namespace YummyZoom.Domain.UserAggregate.Entities;

public sealed class PaymentMethod : Entity<PaymentMethodId>
{
    public string Type { get; private set; }
    public string TokenizedDetails { get; private set; }
    public bool IsDefault { get; private set; }

    private PaymentMethod(
        PaymentMethodId id,
        string type,
        string tokenizedDetails,
        bool isDefault)
        : base(id)
    {
        Type = type;
        TokenizedDetails = tokenizedDetails;
        IsDefault = isDefault;
    }

    public static PaymentMethod Create(
        string type,
        string tokenizedDetails,
        bool isDefault)
    {
        // Basic validation (more complex validation handled in User aggregate)
        // Assuming type and tokenizedDetails are not null/empty is handled in Application layer
        return new PaymentMethod(
            PaymentMethodId.CreateUnique(),
            type,
            tokenizedDetails,
            isDefault);
    }

    // Factory method to create a PaymentMethod with an existing ID (e.g., for persistence)
    public static PaymentMethod Create(
        PaymentMethodId id,
        string type,
        string tokenizedDetails,
        bool isDefault)
    {
         // Basic validation (more complex validation handled in User aggregate)
        // Assuming type and tokenizedDetails are not null/empty is handled in Application layer
        return new PaymentMethod(
            id,
            type,
            tokenizedDetails,
            isDefault);
    }


    // Methods to update payment method details or set as default can be added here
    // These methods should return Result and contain relevant business logic/invariants
    public void SetAsDefault()
    {
        IsDefault = true;
    }

#pragma warning disable CS8618
    // For EF Core
    private PaymentMethod()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\UserAggregate\Errors\UserErrors.cs

```csharp
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UserAggregate.Errors;

public static class UserErrors
{
    public static Error InvalidUserId(string value) => Error.Validation(
        "User.InvalidUserId",
        $"User ID '{value}' is not a valid GUID.");

    public static Error MustHaveAtLeastOneRole => Error.Validation(
        "User.MustHaveAtLeastOneRole",
        "User must have at least one role.");
    
    public static Error UserNotFound(Guid userId) => Error.NotFound(
        "User.UserNotFound",
        $"User with ID '{userId}' not found.");
    
    public static Error RoleAssignmentFailed(string message) => Error.Failure(
        "User.RoleAssignmentFailed",
        $"Role assignment failed: {message}");
    
    public static Error RoleRemovalFailed(string message) => Error.Failure(
        "User.RoleRemovalFailed",
        $"Role removal failed: {message}");

    // Add other user-related errors here as needed

    public static Error RegistrationFailed(string message) => Error.Failure(
        "User.RegistrationFailed",
        $"User registration failed: {message}");

    public static Error EmailUpdateFailed(string message) => Error.Failure(
        "User.EmailUpdateFailed",
        $"Email update failed: {message}");

    public static Error ProfileUpdateFailed(string message) => Error.Failure(
        "User.ProfileUpdateFailed",
        $"Profile update failed: {message}");

    public static Error DuplicateEmail(string email) => Error.Validation(
        "User.DuplicateEmail",
        $"Email '{email}' is already registered.");

    public static Error DeletionFailed(string message) => Error.Failure(
        "User.DeletionFailed",
        $"User deletion failed: {message}");
    
    public static Error RoleNotFound(string role) => Error.NotFound(
        "User.RoleNotFound",
        $"Role '{role}' not found for the user.");

    public static Error CannotRemoveLastRole => Error.Validation(
        "User.CannotRemoveLastRole",
        "Cannot remove the last role from the user.");

    public static Error AddressNotFound(Guid addressId) => Error.NotFound(
        "User.AddressNotFound",
        $"Address with ID '{addressId}' not found for the user.");

    public static Error PaymentMethodNotFound(Guid paymentMethodId) => Error.NotFound(
        "User.PaymentMethodNotFound",
        $"Payment method with ID '{paymentMethodId}' not found for the user.");

    public static Error InvalidPaymentMethod => Error.Validation(
        "User.InvalidPaymentMethod",
        "Payment method is invalid.");

    public static Error InvalidRoleName => Error.Validation(
        "RoleAssignment.InvalidRoleName",
        "Role name cannot be empty.");

    public static Error InvalidRoleTarget => Error.Validation(
        "RoleAssignment.InvalidTarget",
        "Target Entity ID and Type must both be provided or both be null/empty.");
}

```

### src\Domain\UserAggregate\Events\RoleAssignmentAddedToUserEvent.cs

```csharp
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record RoleAssignmentAddedToUserEvent(UserId UserId, RoleAssignment RoleAssignment) : IDomainEvent;

```

### src\Domain\UserAggregate\Events\RoleAssignmentRemovedFromUserEvent.cs

```csharp
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record RoleAssignmentRemovedFromUserEvent(UserId UserId, RoleAssignment RoleAssignment) : IDomainEvent;

```

### src\Domain\UserAggregate\ValueObjects\PaymentMethodId.cs

```csharp
using YummyZoom.SharedKernel;
using YummyZoom.Domain.UserAggregate.Errors; 

namespace YummyZoom.Domain.UserAggregate.ValueObjects;

public sealed class PaymentMethodId : ValueObject
{
    public Guid Value { get; private set; }

    private PaymentMethodId(Guid value)
    {
        Value = value;
    }

    public static PaymentMethodId CreateUnique()
    {
        return new PaymentMethodId(Guid.NewGuid());
    }

    public static PaymentMethodId Create(Guid value)
    {
        return new PaymentMethodId(value);
    }

    public static Result<PaymentMethodId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            // Assuming UserErrors class will have an InvalidPaymentMethodId error
            return Result.Failure<PaymentMethodId>(UserErrors.InvalidPaymentMethod); // Reusing InvalidPaymentMethod for now, can create a specific one if needed
        }

        return Result.Success(new PaymentMethodId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    // For EF Core
    private PaymentMethodId()
    {
    }
}

```

### src\Domain\UserAggregate\ValueObjects\RoleAssignment.cs

```csharp
using YummyZoom.SharedKernel; 
using YummyZoom.Domain.UserAggregate.Errors;

namespace YummyZoom.Domain.UserAggregate.ValueObjects;

public sealed class RoleAssignment : ValueObject
{
    public string RoleName { get; private set; }
    public string? TargetEntityId { get; private set; } // Optional String ID
    public string? TargetEntityType { get; private set; } // Optional String

    private RoleAssignment(
        string roleName,
        string? targetEntityId,
        string? targetEntityType)
    {
        RoleName = roleName;
        TargetEntityId = targetEntityId;
        TargetEntityType = targetEntityType;
    }

    public static Result<RoleAssignment> Create(
        string roleName,
        string? targetEntityId = null,
        string? targetEntityType = null)
    {
        // Basic validation for roleName handled in Application layer.
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Result.Failure<RoleAssignment>(UserErrors.InvalidRoleName);
        }

        // Domain invariant: If TargetEntityId is provided, TargetEntityType must also be provided, and vice versa.
        if ((!string.IsNullOrWhiteSpace(targetEntityId) && string.IsNullOrWhiteSpace(targetEntityType)) ||
            (string.IsNullOrWhiteSpace(targetEntityId) && !string.IsNullOrWhiteSpace(targetEntityType)))
        {
             return Result.Failure<RoleAssignment>(UserErrors.InvalidRoleTarget);
        }

        return Result.Success(new RoleAssignment(
            roleName,
            targetEntityId,
            targetEntityType));
    }

    private static readonly object NullPlaceholder = new object();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RoleName;
        yield return TargetEntityId ?? NullPlaceholder;
        yield return TargetEntityType ?? NullPlaceholder;
    }

#pragma warning disable CS8618
    // For EF Core
    private RoleAssignment()
    {
    }
#pragma warning restore CS8618
}

```

### src\Domain\UserAggregate\ValueObjects\UserId.cs

```csharp
using YummyZoom.Domain.UserAggregate.Errors; // Assuming UserErrors will be in this namespace
using YummyZoom.SharedKernel;
using YummyZoom.Domain.Common.Models; // Correct namespace for AggregateRootId

namespace YummyZoom.Domain.UserAggregate.ValueObjects;

public sealed class UserId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private UserId(Guid value)
    {
        Value = value;
    }

    public static UserId CreateUnique()
    {
        return new UserId(Guid.NewGuid());
    }

    public static UserId Create(Guid value)
    {
        return new UserId(value);
    }

    public static Result<UserId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            // Assuming UserErrors class will have an InvalidUserId error
            return Result.Failure<UserId>(UserErrors.InvalidUserId(value));
        }

        return Result.Success(new UserId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private UserId()
    {
    }
#pragma warning restore CS8618
}

```

### src\Infrastructure\DependencyInjection.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Interceptors;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.Infrastructure.Persistence.Repositories; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("YummyZoomDb");
        Guard.Against.Null(connectionString, message: "Connection string 'YummyZoomDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.RefreshTokenExpiration = TimeSpan.FromDays(7);
            });

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
            })
            .AddDefaultTokenProviders()
            .AddRoles<IdentityRole<Guid>>() 
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IUserAggregateRepository, UserAggregateRepository>(); 

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));
    }
}

```

### src\Infrastructure\GlobalUsings.cs

```csharp
﻿global using Ardalis.GuardClauses;
```

### src\Infrastructure\Infrastructure.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>YummyZoom.Infrastructure</RootNamespace>
    <AssemblyName>YummyZoom.Infrastructure</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Azure.Identity" />
    <PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\SharedKernel\SharedKernel.csproj" />
  </ItemGroup>

</Project>

```

### src\Infrastructure\Data\ApplicationDbContext.cs

```csharp
﻿using System.Reflection;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.UserAggregate;

namespace YummyZoom.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<User> DomainUsers => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

```

### src\Infrastructure\Data\ApplicationDbContextInitialiser.cs

```csharp
﻿using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();

        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        string[] roles = [Roles.Administrator, Roles.Customer, Roles.RestaurantOwner];

        foreach (var roleName in roles)
        {
            if (_roleManager.Roles.All(r => r.Name != roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // Default users
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            await _userManager.AddToRolesAsync(administrator, [Roles.Administrator]);
        }

        // Default data
        // Seed, if necessary
        if (!_context.TodoLists.Any())
        {
            var todoList = TodoList.Create("Todo List", Color.White);
            todoList.AddItem(TodoItem.Create("Make a todo list 📃", null, PriorityLevel.None, null));
            todoList.AddItem(TodoItem.Create("Check off the first item ✅", null, PriorityLevel.None, null));
            todoList.AddItem(TodoItem.Create("Realise you've already done two things on the list! 🤯", null, PriorityLevel.None, null));
            todoList.AddItem(TodoItem.Create("Reward yourself with a nice, long nap 🏆", null, PriorityLevel.None, null));

            _context.TodoLists.Add(todoList);
            await _context.SaveChangesAsync();
        }
    }
}

```

### src\Infrastructure\Data\Configurations\TodoListConfiguration.cs

```csharp
﻿using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class TodoListConfiguration : IEntityTypeConfiguration<TodoList>
{
    public void Configure(EntityTypeBuilder<TodoList> builder)
    {
        ConfigureTodoListsTable(builder);
        ConfigureTodoItemsTable(builder);
    }

    private static void ConfigureTodoListsTable(EntityTypeBuilder<TodoList> builder)
    {
        builder.ToTable("TodoLists");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => TodoListId.Create(value));

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.OwnsOne(t => t.Color, cb =>
        {
            cb.Property(c => c.Code)
                .HasColumnName("Colour")
                .HasMaxLength(10)
                .IsRequired();
        });
    }

    private static void ConfigureTodoItemsTable(EntityTypeBuilder<TodoList> builder)
    {
        builder.OwnsMany(t => t.Items, ib =>
        {
            ib.ToTable("TodoItems");

            ib.WithOwner().HasForeignKey("TodoListId");

            ib.HasKey("TodoListId", "Id");

            ib.Property(i => i.Id)
                .HasColumnName("TodoItemId")
                .ValueGeneratedNever()
                .HasConversion(
                    id => id.Value,
                    value => TodoItemId.Create(value));

            ib.Property(i => i.Title)
                .HasMaxLength(200);

            ib.Property(i => i.Note)
                .HasMaxLength(1000);

            ib.Property(i => i.Priority)
                .HasConversion<int>();

            ib.Property(i => i.Reminder);

            ib.Property(i => i.IsDone);
        });

        builder
            .Metadata
            .FindNavigation(nameof(TodoList.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

```

### src\Infrastructure\Data\Configurations\UserConfiguration.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("DomainUsers"); 

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => UserId.Create(value));

        builder.Property(u => u.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50) 
            .IsRequired(false);

        // --- Configure owned collection of RoleAssignment Value Objects ---
        // This maps UserRoles to a separate UserRoles table
        builder.OwnsMany(u => u.UserRoles, roleBuilder =>
        {
            roleBuilder.ToTable("UserRoles");

            // Foreign key back to the Users table
            roleBuilder.WithOwner().HasForeignKey("UserId");

            // Properties of the RoleAssignment VO
            roleBuilder.Property(ur => ur.RoleName)
                .HasMaxLength(100)
                .IsRequired();

            roleBuilder.Property(ur => ur.TargetEntityId) 
                .HasMaxLength(100)
                .IsRequired(false); 

            roleBuilder.Property(ur => ur.TargetEntityType)
                .HasMaxLength(100)
                .IsRequired(false); 

            // Composite Primary Key for the UserRoles table
            // This ensures a user doesn't have the exact same role assignment twice.
            roleBuilder.HasKey("UserId", "RoleName");

            // Index for querying roles by TargetEntityId (e.g., find all owners of a restaurant)
            roleBuilder.HasIndex("TargetEntityId", "TargetEntityType", "RoleName");
        });

        // --- Configure owned collection of Address Value Objects ---
        // This maps Addresses to a separate UserAddresses table
        builder.OwnsMany(u => u.Addresses, addressBuilder =>
        {
            addressBuilder.ToTable("UserAddresses");
            addressBuilder.WithOwner().HasForeignKey("UserId");

            // Since Address is a value object without an ID property, we need a key for EF Core's owned entities
            // Using an index for the collection - EF Core will create a shadow property Id by default
            addressBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);
            addressBuilder.HasKey("Id");

            // Configure Address properties
            addressBuilder.Property(a => a.Label).HasMaxLength(100).IsRequired(false);
            addressBuilder.Property(a => a.Street).HasMaxLength(255).IsRequired();
            addressBuilder.Property(a => a.City).HasMaxLength(100).IsRequired();
            addressBuilder.Property(a => a.State).HasMaxLength(100).IsRequired(false);
            addressBuilder.Property(a => a.ZipCode).HasMaxLength(20).IsRequired();
            addressBuilder.Property(a => a.Country).HasMaxLength(100).IsRequired();
            addressBuilder.Property(a => a.DeliveryInstructions).HasMaxLength(500).IsRequired(false);
        });

        // --- Configure owned collection of PaymentMethod Child Entities ---
        builder.OwnsMany(u => u.PaymentMethods, paymentBuilder =>
        {
            paymentBuilder.ToTable("UserPaymentMethods");
            paymentBuilder.WithOwner().HasForeignKey("UserId"); // Establishes the FK to Users table

            // PaymentMethod has its own Id, which is its primary key in this table.
            // Assuming PaymentMethodId is globally unique.
            paymentBuilder.HasKey(pm => pm.Id);

            paymentBuilder.Property(i => i.Id)
                .HasColumnName("PaymentMethodId")
                .ValueGeneratedNever()
                .HasConversion(
                    id => id.Value,
                    value => PaymentMethodId.Create(value));

            paymentBuilder.Property(pm => pm.Type)
                .HasMaxLength(50)
                .IsRequired();

            paymentBuilder.Property(pm => pm.TokenizedDetails)
                .HasMaxLength(500) 
                .IsRequired();

            paymentBuilder.Property(pm => pm.IsDefault)
                .IsRequired();
        });
    }
}

```

### src\Infrastructure\Data\Interceptors\AuditableEntityInterceptor.cs

```csharp
﻿using YummyZoom.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IUser _user;
    private readonly TimeProvider _dateTime;

    public AuditableEntityInterceptor(
        IUser user,
        TimeProvider dateTime)
    {
        _user = user;
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                var utcNow = _dateTime.GetUtcNow();
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = _user.Id;
                    entry.Entity.Created = utcNow;

                    // Handle self-registration scenario for User entity
                    if (entry.Entity is YummyZoom.Domain.UserAggregate.User userEntity && string.IsNullOrEmpty(entry.Entity.CreatedBy))
                    {
                        entry.Entity.CreatedBy = userEntity.Id.Value.ToString();
                        entry.Entity.LastModifiedBy = userEntity.Id.Value.ToString(); // Also set LastModifiedBy to self
                    }
                    else
                    {
                        entry.Entity.LastModifiedBy = _user.Id;
                    }
                }
                else // For EntityState.Modified
                {
                    entry.Entity.LastModifiedBy = _user.Id;
                }
                entry.Entity.LastModified = utcNow;
            }
        }
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r => 
            r.TargetEntry != null && 
            r.TargetEntry.Metadata.IsOwned() && 
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}

```

### src\Infrastructure\Data\Interceptors\DispatchDomainEventsInterceptor.cs

```csharp
﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Data.Interceptors;

public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;

    public DispatchDomainEventsInterceptor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();

        return base.SavingChanges(eventData, result);

    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(eventData.Context);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public async Task DispatchDomainEvents(DbContext? context)
    {
        if (context == null) return;

        var entities = context.ChangeTracker
            .Entries<IHasDomainEvent>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent);
    }
}

```

### src\Infrastructure\Identity\ApplicationUser.cs

```csharp
﻿using Microsoft.AspNetCore.Identity;

namespace YummyZoom.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
}

```

### src\Infrastructure\Identity\IdentityResultExtensions.cs

```csharp
﻿using YummyZoom.Application.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace YummyZoom.Infrastructure.Identity;

public static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result)
    {
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description));
    }
}

```

### src\Infrastructure\Identity\IdentityService.cs

```csharp
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserAggregateRepository _userAggregateRepository;
    private readonly ApplicationDbContext _dbContext;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        IUserAggregateRepository userAggregateRepository,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _userAggregateRepository = userAggregateRepository;
        _dbContext = dbContext;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<Result<Guid>> CreateUserAsync(string email, string password, string name)
    {
        // Create the identity user with the provided email
        var identityUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
        };

        // Use a resilient execution strategy for database operations
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        
        try
        {
            // Execute the user creation within a transaction
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                
                try
                {
                    // Step 1: Create the identity user
                    var identityResult = await _userManager.CreateAsync(identityUser, password);
                    
                    if (!identityResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return HandleIdentityErrors(identityResult, email);
                    }

                    // Step 2: Create the domain user
                    Result<UserId> domainUserIdResult = UserId.Create(identityUser.Id);
                    if (domainUserIdResult.IsFailure) 
                    {
                        await transaction.RollbackAsync();
                        // Handle error if Identity user ID is not a valid GUID string
                        return Result.Failure<Guid>(domainUserIdResult.Error); 
                    }
                    var domainUserId = domainUserIdResult.Value;

                    // Create initial role assignment (e.g., Customer)
                    var initialRoleAssignmentResult = RoleAssignment.Create(Roles.Customer);
                    if (initialRoleAssignmentResult.IsFailure)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure<Guid>(initialRoleAssignmentResult.Error);
                    }
                    var initialRoleAssignment = initialRoleAssignmentResult.Value;

                    // Add the user to the Customer role in Identity system
                    var roleAssignResult = await _userManager.AddToRoleAsync(identityUser, initialRoleAssignment.RoleName);
                    if (!roleAssignResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                        return Result.Failure<Guid>(UserErrors.RegistrationFailed($"Failed to assign role: {errors}"));
                    }

                    // Create the domain user aggregate
                    var userAggregateResult = User.Create(
                        domainUserId,
                        name, 
                        email,
                        null, 
                        new List<RoleAssignment> { initialRoleAssignment }); 

                    if (userAggregateResult.IsFailure)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure<Guid>(userAggregateResult.Error);
                    }
                    var userAggregate = userAggregateResult.Value;

                    await _userAggregateRepository.AddAsync(userAggregate);
                    await _dbContext.SaveChangesAsync();

                    // Step 3: Commit transaction if everything succeeded
                    await transaction.CommitAsync();
                    return Result.Success(userAggregate.Id.Value); 
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Use the updated RegistrationFailed error
                    return Result.Failure<Guid>(UserErrors.RegistrationFailed(ex.Message));
                }
            });
        }
        catch (Exception ex)
        {
            // Use the updated RegistrationFailed error
            return Result.Failure<Guid>(UserErrors.RegistrationFailed(ex.Message));
        }
    }

    // Helper method to handle identity errors
    private static Result<Guid> HandleIdentityErrors(IdentityResult identityResult, string email)
    {
        // Check for duplicate email error
        if (identityResult.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            // Use the updated DuplicateEmail error
            return Result.Failure<Guid>(UserErrors.DuplicateEmail(email));
        }

        // General registration failure
        var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
        // Use the updated RegistrationFailed error
        return Result.Failure<Guid>(UserErrors.RegistrationFailed(errors));
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> AddUserToRoleAsync(Guid userId, string role)
    {
        var identityUser = await _userManager.FindByIdAsync(userId.ToString());
        if (identityUser == null)
        {
            return Result.Failure(UserErrors.UserNotFound(userId));
        }

        var result = await _userManager.AddToRoleAsync(identityUser, role);

        if (result.Succeeded)
        {
            return Result.Success();
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure(UserErrors.RoleAssignmentFailed($"Failed to add role '{role}': {errors}"));
    }

    public async Task<Result> RemoveUserFromRoleAsync(Guid userId, string role)
    {
        var identityUser = await _userManager.FindByIdAsync(userId.ToString());
        if (identityUser == null)
        {
            return Result.Failure(UserErrors.UserNotFound(userId));
        }

        var result = await _userManager.RemoveFromRoleAsync(identityUser, role);

        if (result.Succeeded)
        {
            return Result.Success();
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure(UserErrors.RoleRemovalFailed($"Failed to remove role '{role}': {errors}"));
    }

    public async Task<Result> UpdateEmailAsync(string userId, string newEmail)
    {
        // Use a resilient execution strategy for database operations
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        
        try
        {
            // Execute the email update within a transaction
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                
                try
                {
                    // Step 1: Find and update the Identity user
                    var identityUser = await _userManager.FindByIdAsync(userId);
                    if (identityUser == null)
                    {
                        return Result.Failure(UserErrors.InvalidUserId(userId));
                    }

                    // Check if the email is already in use
                    var existingUserWithEmail = await _userManager.FindByEmailAsync(newEmail);
                    if (existingUserWithEmail != null && existingUserWithEmail.Id.ToString() != userId)
                    {
                        return Result.Failure(UserErrors.DuplicateEmail(newEmail));
                    }

                    // Update email in Identity (note: this also updates normalized email)
                    var emailUpdateResult = await _userManager.SetEmailAsync(identityUser, newEmail);
                    if (!emailUpdateResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", emailUpdateResult.Errors.Select(e => e.Description));
                        return Result.Failure(UserErrors.EmailUpdateFailed(errors));
                    }

                    // Also update UserName since we're using email as the username
                    var usernameUpdateResult = await _userManager.SetUserNameAsync(identityUser, newEmail);
                    if (!usernameUpdateResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", usernameUpdateResult.Errors.Select(e => e.Description));
                        return Result.Failure(UserErrors.EmailUpdateFailed(errors));
                    }

                    // Step 2: Find and update the domain user
                    var domainUserId = UserId.Create(Guid.Parse(userId));
                    var domainUser = await _userAggregateRepository.GetByIdAsync(domainUserId);
                    
                    if (domainUser is null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(UserErrors.InvalidUserId(userId));
                    }

                    // Update email in domain model
                    domainUser.UpdateEmail(newEmail);
                    
                    // Save changes to domain user
                    await _userAggregateRepository.UpdateAsync(domainUser);
                    await _dbContext.SaveChangesAsync();

                    // Step 3: Commit transaction if everything succeeded
                    await transaction.CommitAsync();
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure(UserErrors.EmailUpdateFailed(ex.Message));
                }
            });
        }
        catch (Exception ex)
        {
            return Result.Failure(UserErrors.EmailUpdateFailed(ex.Message));
        }
    }

    public async Task<Result> UpdateProfileAsync(string userId, string name, string? phoneNumber)
    {
        // Use a resilient execution strategy for database operations
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        
        try
        {
            // Execute the profile update within a transaction
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                
                try
                {
                    // Step 1: Find and update the Identity user
                    var identityUser = await _userManager.FindByIdAsync(userId);
                    if (identityUser == null)
                    {
                        return Result.Failure(UserErrors.InvalidUserId(userId));
                    }

                    // Update phone number in Identity if provided (used for 2FA)
                    if (identityUser.PhoneNumber != phoneNumber)
                    {
                        var phoneUpdateResult = await _userManager.SetPhoneNumberAsync(identityUser, phoneNumber);
                        if (!phoneUpdateResult.Succeeded)
                        {
                            await transaction.RollbackAsync();
                            var errors = string.Join(", ", phoneUpdateResult.Errors.Select(e => e.Description));
                            return Result.Failure(UserErrors.ProfileUpdateFailed(errors));
                        }
                    }

                    // Step 2: Find and update the domain user
                    var domainUserId = UserId.Create(Guid.Parse(userId));
                    var domainUser = await _userAggregateRepository.GetByIdAsync(domainUserId);
                    
                    if (domainUser is null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(UserErrors.InvalidUserId(userId));
                    }

                    // Update name and phone in domain model
                    domainUser.UpdateProfile(name, phoneNumber);
                    
                    // Save changes to domain user
                    await _userAggregateRepository.UpdateAsync(domainUser);
                    await _dbContext.SaveChangesAsync();

                    // Step 3: Commit transaction if everything succeeded
                    await transaction.CommitAsync();
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure(UserErrors.ProfileUpdateFailed(ex.Message));
                }
            });
        }
        catch (Exception ex)
        {
            return Result.Failure(UserErrors.ProfileUpdateFailed(ex.Message));
        }
    }

    private async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            return Result.Success();
        }
        
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure(UserErrors.DeletionFailed(errors));
    }
}

```

### src\Infrastructure\Persistence\Repositories\UserAggregateRepository.cs

```csharp
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore; // Required for EF Core async methods

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class UserAggregateRepository : IUserAggregateRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserAggregateRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.DomainUsers.AddAsync(user, cancellationToken);
        // Note: SaveChangesAsync is typically called by a Unit of Work pattern
        // or explicitly after the command handler completes its operations.
        // Adding it here would make the repository less flexible if multiple
        // operations need to be part of the same transaction.
        // For now, we assume SaveChangesAsync is handled elsewhere (e.g., by IdentityService's transaction).
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Ensure case-insensitive comparison if needed by the database collation or requirements
        return await _dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase), cancellationToken);
    }

    public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        // FindAsync is suitable for finding by primary key
        return await _dbContext.DomainUsers.FindAsync([userId], cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        // EF Core tracks changes, so just marking the entity as Modified
        // or relying on change tracking is usually sufficient.
        // The actual update SQL is generated during SaveChangesAsync.
        _dbContext.DomainUsers.Update(user); 
        // Again, SaveChangesAsync is assumed to be handled elsewhere.
        return Task.CompletedTask; // Update itself is synchronous in terms of EF tracking
    }
}

```

### src\ServiceDefaults\Extensions.cs

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}

```

### src\ServiceDefaults\ServiceDefaults.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireSharedProject>true</IsAspireSharedProject>
    <RootNamespace>YummyZoom.ServiceDefaults</RootNamespace>
    <AssemblyName>YummyZoom.ServiceDefaults</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
  </ItemGroup>

</Project>

```

### src\SharedKernel\Error.cs

```csharp

namespace YummyZoom.SharedKernel;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new(
        "General.Null",
        "Null value was provided",
        ErrorType.Failure);

    public Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    public static Error Failure(string code, string description)
    {
        return new(code, description, ErrorType.Failure);
    }

    public static Error NotFound(string code, string description)
    {
        return new(code, description, ErrorType.NotFound);
    }

    public static Error Problem(string code, string description)
    {
        return new(code, description, ErrorType.Problem);
    }

    public static Error Conflict(string code, string description)
    {
        return new(code, description, ErrorType.Conflict);
    }

    public static Error Validation(string code, string description)
    {
        return new(code, description, ErrorType.Validation);
    }
}

```

### src\SharedKernel\ErrorType.cs

```csharp
namespace YummyZoom.SharedKernel;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    Problem = 2,
    NotFound = 3,
    Conflict = 4
}

```

### src\SharedKernel\Result.cs

```csharp
using System.Diagnostics.CodeAnalysis;

namespace YummyZoom.SharedKernel;

public class Result
{
    public Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None ||
            !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success()
    {
        return new(true, Error.None);
    }

    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new(value, true, Error.None);
    }

    public static Result Failure(Error error)
    {
        return new(false, error);
    }

    public static Result<TValue> Failure<TValue>(Error error)
    {
        return new(default, false, error);
    }
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can't be accessed.");

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    public static Result<TValue> ValidationFailure(Error error)
    {
        return new(default, false, error);
    }
}

```

### src\SharedKernel\SharedKernel.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>YummyZoom.SharedKernel</RootNamespace>
    <AssemblyName>YummyZoom.SharedKernel</AssemblyName>
  </PropertyGroup>

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>

```

### src\SharedKernel\ValidationError.cs

```csharp
namespace YummyZoom.SharedKernel;

public sealed record ValidationError : Error
{
    public ValidationError(Error[] errors)
        : base(
            "Validation.General",
            "One or more validation errors occurred",
            ErrorType.Validation)
    {
        Errors = errors;
    }

    public Error[] Errors { get; }

    public static ValidationError FromResults(IEnumerable<Result> results)
    {
        return new(results.Where(r => r.IsFailure).Select(r => r.Error).ToArray());
    }
}

```

### src\SharedKernel\Constants\Policies.cs

```csharp
﻿namespace YummyZoom.SharedKernel.Constants;

public abstract class Policies
{
    public const string CanPurge = nameof(CanPurge);
}

```

### src\SharedKernel\Constants\Roles.cs

```csharp
﻿namespace YummyZoom.SharedKernel.Constants;

public abstract class Roles
{
    public const string Administrator = nameof(Administrator);

    public const string Customer = nameof(Customer);

    public const string RestaurantOwner = nameof(RestaurantOwner);
}

```

### src\Web\appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "YummyZoomDb": "Server=127.0.0.1;Port=5432;Database=YummyZoomDb;Username=admin;Password=password;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore.SpaProxy": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}

```

### src\Web\appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}

```

### src\Web\DependencyInjection.cs

```csharp
﻿using Azure.Identity;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Web.Services;
using Microsoft.AspNetCore.Mvc;

using NSwag;
using NSwag.Generation.Processors.Security;

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

        builder.Services.AddOpenApiDocument((configure, sp) =>
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
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}

```

### src\Web\GlobalUsings.cs

```csharp
global using Ardalis.GuardClauses;
global using YummyZoom.Web.Infrastructure;
global using MediatR;

```

### src\Web\Program.cs

```csharp
using YummyZoom.Application;
using YummyZoom.Infrastructure;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Web;

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

app.UseOpenApi(settings =>
{
    settings.Path = "/api/specification.json";
});

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});
app.UseStaticFiles();

app.UseExceptionHandler(options => { });

app.Map("/", () => Results.Redirect("/api"));

app.MapDefaultEndpoints();
app.MapEndpoints();

app.Run();

public partial class Program { }

```

### src\Web\Web.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <RootNamespace>YummyZoom.Web</RootNamespace>
    <AssemblyName>YummyZoom.Web</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
    <ProjectReference Include="..\ServiceDefaults\ServiceDefaults.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" />
    <PackageReference Include="Azure.Identity" />
    <PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="NSwag.AspNetCore" />
    <PackageReference Include="NSwag.MSBuild">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentValidation.AspNetCore" />
  </ItemGroup>

  <ItemGroup>
    <Folder Include="Models\Responses\" />
  </ItemGroup>

</Project>

```

### src\Web\Web.http

```http
﻿# For more info on HTTP files go to https://aka.ms/vs/httpfile
@Web_HostAddress = https://localhost:5001

@Email=administrator@localhost
@Password=Administrator1!
@BearerToken=<YourToken>

# POST Users Register
POST {{Web_HostAddress}}/api/Users/Register
Content-Type: application/json

{
  "email": "{{Email}}",
  "password": "{{Password}}"
}

###

# POST Users Login
POST {{Web_HostAddress}}/api/Users/Login
Content-Type: application/json

{
  "email": "{{Email}}",
  "password": "{{Password}}"
}

###

# POST Users Refresh
POST {{Web_HostAddress}}/api/Users/Refresh
Authorization: Bearer {{BearerToken}}
Content-Type: application/json

{
  "refreshToken": ""
}

###

# GET WeatherForecast
GET {{Web_HostAddress}}/api/WeatherForecasts
Authorization: Bearer {{BearerToken}}

###

# GET TodoLists
GET {{Web_HostAddress}}/api/TodoLists
Authorization: Bearer {{BearerToken}}

###

# POST TodoLists
POST {{Web_HostAddress}}/api/TodoLists
Authorization: Bearer {{BearerToken}}
Content-Type: application/json

// CreateTodoListCommand
{
  "Title": "Backlog"
}

###

# PUT TodoLists
PUT {{Web_HostAddress}}/api/TodoLists/1
Authorization: Bearer {{BearerToken}}
Content-Type: application/json

// UpdateTodoListCommand
{
  "Id": 1,
  "Title": "Product Backlog"
}

###

# DELETE TodoLists
DELETE {{Web_HostAddress}}/api/TodoLists/1
Authorization: Bearer {{BearerToken}}

###

# GET TodoItems
@PageNumber = 1
@PageSize = 10
GET {{Web_HostAddress}}/api/TodoItems?ListId=1&PageNumber={{PageNumber}}&PageSize={{PageSize}}

Authorization: Bearer {{BearerToken}}

###

# POST TodoItems
POST {{Web_HostAddress}}/api/TodoItems
Authorization: Bearer {{BearerToken}}
Content-Type: application/json

// CreateTodoItemCommand
{
  "ListId": 1,
  "Title": "Eat a burrito 🌯"
}

###

#PUT TodoItems UpdateItemDetails
PUT {{Web_HostAddress}}/api/TodoItems/UpdateItemDetails?Id=1
Authorization: Bearer {{BearerToken}}
Content-Type: application/json

// UpdateTodoItemDetailCommand
{
  "Id": 1,
  "ListId": 1,
  "Priority": 3,
  "Note": "This is a good idea!"
}

###

# PUT TodoItems
PUT {{Web_HostAddress}}/api/TodoItems/1
Authorization: Bearer {{BearerToken}}
Content-Type: application/json

// UpdateTodoItemCommand
{
  "Id": 1,
  "Title": "Eat a yummy burrito 🌯",
  "Done": true
}

###

# DELETE TodoItem
DELETE {{Web_HostAddress}}/api/TodoItems/1
Authorization: Bearer {{BearerToken}}

###
```

### src\Web\Endpoints\TodoItems.cs

```csharp
﻿using YummyZoom.Application.Common.Models;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Commands.DeleteTodoItem;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItemDetail;
using YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;

namespace YummyZoom.Web.Endpoints;

public class TodoItems : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .RequireAuthorization();

        // GET /api/TodoItems
        group.MapGet(GetTodoItemsWithPagination)
            .WithStandardResults<PaginatedList<TodoItemBriefDto>>();

        // POST /api/TodoItems
        group.MapPost(CreateTodoItem)
            .WithStandardResults<Guid>();

        // PUT /api/TodoItems/{listId}/{id}
        group.MapPut(UpdateTodoItem, "{listId}/{id}")
            .WithStandardResults();

        // PUT /api/TodoItems/UpdateDetail/{listId}/{id}
        group.MapPut(UpdateTodoItemDetail, "UpdateDetail/{listId}/{id}")
            .WithStandardResults();

        // DELETE /api/TodoItems/{listId}/{id}
        group.MapDelete(DeleteTodoItem, "{listId}/{id}")
            .WithStandardResults();
    }

    private async Task<IResult> GetTodoItemsWithPagination(ISender sender, [AsParameters] GetTodoItemsWithPaginationQuery query)
    {
        var result = await sender.Send(query);

        return result.ToIResult();
    }

    private async Task<IResult> CreateTodoItem(ISender sender, CreateTodoItemCommand command)
    {
        var result = await sender.Send(command);

        return result.IsSuccess
            ? TypedResults.Created($"/{nameof(TodoItems)}/{result.Value}", result.Value)
            : result.ToIResult();
    }

    private async Task<IResult> UpdateTodoItem(ISender sender, Guid listId, Guid id, UpdateTodoItemCommand command)
    {
        if (id != command.Id || listId != command.ListId) return TypedResults.BadRequest();

        var result = await sender.Send(command);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }

    private async Task<IResult> UpdateTodoItemDetail(ISender sender, Guid listId, Guid id, UpdateTodoItemDetailCommand command)
    {
        if (id != command.Id || listId != command.ListId) return TypedResults.BadRequest();
        
        var result = await sender.Send(command);
        
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }

    private async Task<IResult> DeleteTodoItem(ISender sender, Guid listId, Guid id)
    {
        var result = await sender.Send(new DeleteTodoItemCommand(listId, id));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }
}

```

### src\Web\Endpoints\TodoLists.cs

```csharp
﻿using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Application.TodoLists.Commands.DeleteTodoList;
using YummyZoom.Application.TodoLists.Commands.UpdateTodoList;
using YummyZoom.Application.TodoLists.Queries.GetTodos;

namespace YummyZoom.Web.Endpoints;

public class TodoLists : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .RequireAuthorization();

        // GET /api/TodoLists
        group.MapGet(GetTodoLists)
            .WithStandardResults<TodosVm>();

        // POST /api/TodoLists
        group.MapPost(CreateTodoList)
            .WithStandardResults<Guid>();

        // PUT /api/TodoLists/{id}
        group.MapPut(UpdateTodoList, "{id}")
            .WithStandardResults();

        // DELETE /api/TodoLists/{id}
        group.MapDelete(DeleteTodoList, "{id}")
            .WithStandardResults();
    }

    private async Task<IResult> GetTodoLists(ISender sender)
    {
        var result = await sender.Send(new GetTodosQuery());

        return result.ToIResult();
    }

    private async Task<IResult> CreateTodoList(ISender sender, CreateTodoListCommand command)
    {
        var result = await sender.Send(command);

        return result.IsSuccess 
            ? TypedResults.Created($"/{nameof(TodoLists)}/{result.Value}", result.Value) 
            : result.ToIResult();
    }

    private async Task<IResult> UpdateTodoList(ISender sender, Guid id, UpdateTodoListCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        
        var result = await sender.Send(command);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }

    private async Task<IResult> DeleteTodoList(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteTodoListCommand(id));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToIResult();
    }
}

```

### src\Web\Endpoints\Users.cs

```csharp
﻿using YummyZoom.Application.Users.Commands.RegisterUser;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Users.Commands.AssignRoleToUser;
using YummyZoom.Application.Users.Commands.RemoveRoleFromUser;

namespace YummyZoom.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        // Keep other identity API endpoints
        group.MapIdentityApi<ApplicationUser>();

        // Add custom registration endpoint
        group.MapPost("/register-custom", async ([FromBody] RegisterUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            
            return result.IsSuccess
                ? Results.Ok(new RegisterUserResponse { UserId = result.Value })
                : result.ToIResult();
        })
        .WithName("RegisterUserCustom")
        .WithStandardResults<RegisterUserResponse>();

        // Add endpoint for assigning roles
        group.MapPost("/assign-role", async ([FromBody] AssignRoleToUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("AssignRoleToUser");

        // Add endpoint for removing roles
        group.MapPost("/remove-role", async ([FromBody] RemoveRoleFromUserCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("RemoveRoleFromUser");
    }
}

```

### src\Web\Endpoints\WeatherForecasts.cs

```csharp
﻿using YummyZoom.Application.WeatherForecasts.Queries.GetWeatherForecasts;

namespace YummyZoom.Web.Endpoints;

public class WeatherForecasts : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        // GET /api/WeatherForecasts
        group.MapGet(GetWeatherForecasts)
            .WithStandardResults<IEnumerable<WeatherForecast>>();
    }

    public async Task<IResult> GetWeatherForecasts(ISender sender)
    {
        var result = await sender.Send(new GetWeatherForecastsQuery());
        
        return result.ToIResult();
    }
}

```

### src\Web\Infrastructure\CustomExceptionHandler.cs

```csharp
﻿using YummyZoom.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace YummyZoom.Web.Infrastructure;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;

    public CustomExceptionHandler()
    {
        // Register known exception types and handlers.
        _exceptionHandlers = new()
            {
                { typeof(ValidationException), HandleValidationException },
                { typeof(NotFoundException), HandleNotFoundException },
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
            };
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }

        return false;
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    {
        var exception = (NotFoundException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = exception.Message
        });
    }

    private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });
    }

    private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        });
    }
}

```

### src\Web\Infrastructure\CustomResults.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using YummyZoom.SharedKernel;

namespace YummyZoom.Web.Infrastructure
{
    internal static class CustomResults
    {
        public static IResult Problem(Result result)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => CreateValidationProblem(result.Error),
                ErrorType.NotFound => CreateNotFoundProblem(result.Error),
                ErrorType.Conflict => CreateConflictProblem(result.Error),
                ErrorType.Failure => CreateFailureProblem(result.Error),
                _ => CreateServerErrorProblem(result.Error)
            };
        }

        public static IResult Problem<T>(Result<T> result)
        {
            return Problem((Result)result);
        }

        private static IResult CreateValidationProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static IResult CreateNotFoundProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static IResult CreateConflictProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static IResult CreateFailureProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static IResult CreateServerErrorProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static string GetTitle(Error error)
        {
            var parts = error.Code.Split('.');
            return parts.Length > 0 ? parts[0] : "Error";
        }
    }
}

```

### src\Web\Infrastructure\EndpointExtensions.cs

```csharp
namespace YummyZoom.Web.Infrastructure;

public static class EndpointExtensions
{
    public static RouteHandlerBuilder WithStandardResults<T>(this RouteHandlerBuilder builder)
    {
        return builder
            .Produces<T>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
    
    public static RouteHandlerBuilder WithStandardResults(this RouteHandlerBuilder builder)
    {
        return builder
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}

```

### src\Web\Infrastructure\EndpointGroupBase.cs

```csharp
﻿namespace YummyZoom.Web.Infrastructure;

public abstract class EndpointGroupBase
{
    public abstract void Map(WebApplication app);
}

```

### src\Web\Infrastructure\IEndpointRouteBuilderExtensions.cs

```csharp
﻿using System.Diagnostics.CodeAnalysis;

namespace YummyZoom.Web.Infrastructure;

public static class IEndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapGet(
        this IEndpointRouteBuilder builder,
        Delegate handler,
        [StringSyntax("Route")] string pattern = "")
    {
        Guard.Against.AnonymousMethod(handler);
        return builder
            .MapGet(pattern, handler)
            .WithName(handler.Method.Name);
    }

    public static RouteHandlerBuilder MapPost(
        this IEndpointRouteBuilder builder,
        Delegate handler,
        [StringSyntax("Route")] string pattern = "")
    {
        Guard.Against.AnonymousMethod(handler);
        return builder
            .MapPost(pattern, handler)
            .WithName(handler.Method.Name);
    }

    public static RouteHandlerBuilder MapPut(
        this IEndpointRouteBuilder builder,
        Delegate handler,
        [StringSyntax("Route")] string pattern)
    {
        Guard.Against.AnonymousMethod(handler);
        return builder
            .MapPut(pattern, handler)
            .WithName(handler.Method.Name);
    }

    public static RouteHandlerBuilder MapDelete(
        this IEndpointRouteBuilder builder,
        Delegate handler,
        [StringSyntax("Route")] string pattern)
    {
        Guard.Against.AnonymousMethod(handler);
        return builder
            .MapDelete(pattern, handler)
            .WithName(handler.Method.Name);
    }
}

```

### src\Web\Infrastructure\MethodInfoExtensions.cs

```csharp
﻿using System.Reflection;

namespace YummyZoom.Web.Infrastructure;

public static class MethodInfoExtensions
{
    public static bool IsAnonymous(this MethodInfo method)
    {
        var invalidChars = new[] { '<', '>' };
        return method.Name.Any(invalidChars.Contains);
    }

    public static void AnonymousMethod(this IGuardClause guardClause, Delegate input)
    {
        if (input.Method.IsAnonymous())
            throw new ArgumentException("The endpoint name must be specified when using anonymous handlers.");
    }
}
```

### src\Web\Infrastructure\ResultExtensions.cs

```csharp
using YummyZoom.SharedKernel;

namespace YummyZoom.Web.Infrastructure
{
    internal static class ResultExtensions
    {
        public static TOut Match<TOut>(
            this Result result,
            Func<TOut> onSuccess,
            Func<Result, TOut> onFailure)
            => result.IsSuccess ? onSuccess() : onFailure(result);

        public static TOut Match<TIn, TOut>(
            this Result<TIn> result,
            Func<TIn, TOut> onSuccess,
            Func<Result<TIn>, TOut> onFailure)
            => result.IsSuccess ? onSuccess(result.Value) : onFailure(result);

        public static IResult ToIResult<T>(this Result<T> r)
            => r.Match(Results.Ok, CustomResults.Problem);

        public static Task<IResult> ToIResultAsync<T>(this Task<Result<T>> task)
            => task.ContinueWith(t => t.Result.ToIResult());
    }
}

```

### src\Web\Infrastructure\WebApplicationExtensions.cs

```csharp
﻿using System.Reflection;

namespace YummyZoom.Web.Infrastructure;

public static class WebApplicationExtensions
{
    public static RouteGroupBuilder MapGroup(this WebApplication app, EndpointGroupBase group)
    {
        var groupName = group.GetType().Name;

        return app
            .MapGroup($"/api/{groupName}")
            .WithGroupName(groupName)
            .WithTags(groupName);
    }

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

```

### src\Web\Properties\launchSettings.json

```json
﻿{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:61846",
      "sslPort": 44312
    }
  },
  "profiles": {
    "YummyZoom.Web": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
      "IIS Express": {
        "commandName": "IISExpress",
        "launchBrowser": true,
        "environmentVariables": {
          "ASPNETCORE_ENVIRONMENT": "Development"
        }
      }
    }
}

```

### src\Web\Services\CurrentUser.cs

```csharp
﻿using System.Security.Claims;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.ValueObjects; 

namespace YummyZoom.Web.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public UserId? DomainId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaim, out var guidValue) && guidValue != Guid.Empty)
            {
                try
                {
                    return UserId.Create(guidValue);
                }
                catch (ArgumentException) 
                {
                    // Log this occurrence if necessary
                    return null;
                }
            }
            return null;
        }
    }
}

```

### tests\Application.FunctionalTests\Application.FunctionalTests.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <RootNamespace>YummyZoom.Application.FunctionalTests</RootNamespace>
        <AssemblyName>YummyZoom.Application.FunctionalTests</AssemblyName>
    </PropertyGroup>

    <ItemGroup>
      <None Remove="appsettings.json" />
    </ItemGroup>

    <ItemGroup>
      <Content Include="appsettings.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
        <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
      </Content>
    </ItemGroup>
    <ItemGroup>
      <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
      <PackageReference Include="Microsoft.Data.SqlClient" />
      <PackageReference Include="Microsoft.NET.Test.Sdk" />
      <PackageReference Include="nunit" />
      <PackageReference Include="NUnit.Analyzers">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      </PackageReference>
      <PackageReference Include="NUnit3TestAdapter" />
      <PackageReference Include="coverlet.collector">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      </PackageReference>
      <PackageReference Include="FluentAssertions" />
      <PackageReference Include="Moq" />
      <PackageReference Include="Respawn" />
      <PackageReference Include="System.Configuration.ConfigurationManager" />
      <PackageReference Include="Testcontainers.PostgreSql" />
    </ItemGroup>
    
    <ItemGroup>
        <ProjectReference Include="..\..\src\Web\Web.csproj" />
    </ItemGroup>

</Project>

```

### tests\Application.FunctionalTests\appsettings.json

```json
{
  "ConnectionStrings": {
    "YummyZoomDb": "Server=127.0.0.1;Port=5432;Database=YummyZoomTestDb;Username=admin;Password=password;"
  }
}
```

### tests\Application.FunctionalTests\BaseTestFixture.cs

```csharp
﻿namespace YummyZoom.Application.FunctionalTests;

using static Testing;

[TestFixture]
public abstract class BaseTestFixture
{
    [SetUp]
    public async Task TestSetUp()
    {
        await ResetState();
    }
}

```

### tests\Application.FunctionalTests\CustomWebApplicationFactory.cs

```csharp
﻿using System.Data.Common;
using YummyZoom.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests;

using static Testing;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection;
    private readonly string _connectionString;

    public CustomWebApplicationFactory(DbConnection connection, string connectionString)
    {
        _connection = connection;
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:YummyZoomDb", _connectionString);
        builder.ConfigureTestServices(services =>
        {
            services
                .RemoveAll<IUser>()
                .AddTransient(provider =>
                {
                    var userIdGuid = GetUserId(); 
                    var mock = new Mock<IUser>();
                    mock.Setup(s => s.Id).Returns(userIdGuid?.ToString());
                    if (userIdGuid.HasValue)
                    {
                        mock.Setup(s => s.DomainId).Returns(UserId.Create(userIdGuid.Value));
                    }
                    return mock.Object;
                });
        });
    }
}

```

### tests\Application.FunctionalTests\GlobalUsings.cs

```csharp
﻿global using Ardalis.GuardClauses;
global using FluentAssertions;
global using Moq;
global using NUnit.Framework;
```

### tests\Application.FunctionalTests\ITestDatabase.cs

```csharp
﻿using System.Data.Common;

namespace YummyZoom.Application.FunctionalTests;

public interface ITestDatabase
{
    Task InitialiseAsync();

    DbConnection GetConnection();

    string GetConnectionString();

    Task ResetAsync();

    Task DisposeAsync();
}

```

### tests\Application.FunctionalTests\PostgreSQLTestcontainersTestDatabase.cs

```csharp
﻿using System.Data.Common;
using YummyZoom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace YummyZoom.Application.FunctionalTests;

public class PostgreSQLTestcontainersTestDatabase : ITestDatabase
{
    private const string DefaultDatabase = "YummyZoomTestDb";
    private readonly PostgreSqlContainer _container;
    private DbConnection _connection = null!;
    private string _connectionString = null!;
    private Respawner _respawner = null!;

    public PostgreSQLTestcontainersTestDatabase()
    {
        _container = new PostgreSqlBuilder()
            .WithAutoRemove(true)
            .Build();
    }

    public async Task InitialiseAsync()
    {
        await _container.StartAsync();
        await _container.ExecScriptAsync($"CREATE DATABASE {DefaultDatabase}");

        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = DefaultDatabase
        };

        _connectionString = builder.ConnectionString;

        _connection = new NpgsqlConnection(_connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ApplicationDbContext(options);

        await context.Database.MigrateAsync();

        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
        await _connection.CloseAsync();
    }

    public DbConnection GetConnection()
    {
        return _connection;
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }

    public async Task ResetAsync()
    {
        await _connection.OpenAsync();
        await _respawner.ResetAsync(_connection);
        await _connection.CloseAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }
}

```

### tests\Application.FunctionalTests\PostgreSQLTestDatabase.cs

```csharp
﻿using System.Data.Common;
using YummyZoom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;

namespace YummyZoom.Application.FunctionalTests;

public class PostgreSQLTestDatabase : ITestDatabase
{
    private readonly string _connectionString = null!;
    private NpgsqlConnection _connection = null!;
    private Respawner _respawner = null!;

    public PostgreSQLTestDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("YummyZoomDb");

        Guard.Against.Null(connectionString);

        _connectionString = connectionString;
    }

    public async Task InitialiseAsync()
    {
        _connection = new NpgsqlConnection(_connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ApplicationDbContext(options);

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
        await _connection.CloseAsync();
    }

    public DbConnection GetConnection()
    {
        return _connection;
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }

    public async Task ResetAsync()
    {
        await _connection.OpenAsync();
        await _respawner.ResetAsync(_connection);
        await _connection.CloseAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

```

### tests\Application.FunctionalTests\ResultAssertions.cs

```csharp
using FluentAssertions.Primitives;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.FunctionalTests;

public static class ResultAssertions
{
    public static void ShouldBeSuccessful<T>(this Result<T> result)
    {
        result.IsSuccess.Should().BeTrue("the operation should have succeeded");
    }

    public static void ShouldBeFailure<T>(this Result<T> result, string? errorCode = null)
    {
        result.IsSuccess.Should().BeFalse("the operation should have failed");
        if (errorCode != null)
            result.Error.Code.Should().Be(errorCode, $"the error code should be '{errorCode}'");
    }

    public static T ValueOrFail<T>(this Result<T> result)
    {
        result.ShouldBeSuccessful();
        return result.Value;
    }
    
    public static ObjectAssertions ShouldHaveValue<T>(this Result<T> result)
    {
        result.ShouldBeSuccessful();
        return result.Value.Should();
    }
}

```

### tests\Application.FunctionalTests\TestDatabaseFactory.cs

```csharp
﻿namespace YummyZoom.Application.FunctionalTests;

public static class TestDatabaseFactory
{
    public static async Task<ITestDatabase> CreateAsync()
    {
        // Testcontainers requires Docker. To use a local PostgreSQL database instead,
        // switch to `PostgreSQLTestDatabase` and update appsettings.json.
        var database = new PostgreSQLTestcontainersTestDatabase();

        await database.InitialiseAsync();

        return database;
    }
}

```

### tests\Application.FunctionalTests\Testing.cs

```csharp
﻿using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests;

[SetUpFixture]
public partial class Testing
{
    private static ITestDatabase _database = null!;
    private static CustomWebApplicationFactory _factory = null!;
    private static IServiceScopeFactory _scopeFactory = null!;
    private static Guid? _userId; 

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        _database = await TestDatabaseFactory.CreateAsync();

        _factory = new CustomWebApplicationFactory(_database.GetConnection(), _database.GetConnectionString());

        _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();

        // Explicitly seed roles needed for tests
        using var scope = _scopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        string[] roles = { Roles.Administrator, Roles.Customer, Roles.RestaurantOwner };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }
    }

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        return await mediator.Send(request);
    }

    // Helper method to unwrap Result<T> to T if needed
    public static async Task<T> SendAndUnwrapAsync<T>(IRequest<Result<T>> request)
    {
        var result = await SendAsync(request);
        return result.ValueOrFail();
    }

    public static async Task SendAsync(IBaseRequest request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        await mediator.Send(request);
    }

    public static Guid? GetUserId() 
    {
        return _userId;
    }

    public static async Task<Guid> RunAsDefaultUserAsync() 
    {
        return await RunAsUserAsync("test@local", "Testing1234!", Array.Empty<string>());
    }

    public static async Task<Guid> RunAsAdministratorAsync() 
    {
        return await RunAsUserAsync("administrator@local", "Administrator1234!", new[] { Roles.Administrator });
    }

    public static async Task<Guid> RunAsUserAsync(string userName, string password, string[] roles) 
    {
        await EnsureRolesExistAsync(roles);
        
        using var scope = _scopeFactory.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = userName, Email = userName };

        var result = await userManager.CreateAsync(user, password);

        if (roles.Any())
        {
            await userManager.AddToRolesAsync(user, roles);
        }

        if (result.Succeeded)
        {
            _userId = user.Id; 

            return _userId.Value; 
        }

        var errors = string.Join(Environment.NewLine, result.ToApplicationResult().Errors);

        throw new Exception($"Unable to create {userName}.{Environment.NewLine}{errors}");
    }

    public static async Task ResetState()
    {
        try
        {
            await _database.ResetAsync();
        }
        catch (Exception) 
        {
        }

        _userId = null;
    }

    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.FindAsync<TEntity>(keyValues);
    }

    public static T GetService<T>()
        where T : notnull
    {
        using var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Add(entity);

        await context.SaveChangesAsync();
    }

    public static async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Set<TEntity>().CountAsync();
    }

    public static IServiceScope CreateScope()
    {
        return _scopeFactory.CreateScope();
    }
    
    // Helper to ensure roles exist, can be called by other helpers
    public static async Task EnsureRolesExistAsync(params string[]? roleNames)
    {
        if (roleNames == null || roleNames.Length == 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope(); // Essential: new scope for this operation
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in roleNames)
        {
            if (string.IsNullOrWhiteSpace(roleName)) continue; // Skip empty role names

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!result.Succeeded)
                {
                    // Log or handle more gracefully if this is a common test setup issue
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Warning: Failed to create role {roleName} during test setup. Errors: {errors}");
                }
            }
        }
    }
    
    public static async Task SetupForUserRegistrationTestsAsync()
    {
        await EnsureRolesExistAsync(Roles.Customer, Roles.Administrator, Roles.RestaurantOwner); 
    }

    [OneTimeTearDown]
    public async Task RunAfterAnyTests()
    {
        await _database.DisposeAsync();
        await _factory.DisposeAsync();
    }
}

```

### tests\Application.FunctionalTests\TodoItems\Commands\CreateTodoItemTests.cs

```csharp
﻿using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class CreateTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateTodoItemCommand();

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldCreateTodoItem()
    {
        var userId = await RunAsDefaultUserAsync();

        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var command = new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Tasks",
            Note = "Test note",
            Priority = PriorityLevel.Medium,
            Reminder = null
        };

        var itemResult = await SendAsync(command);
        
        itemResult.ShouldBeSuccessful();
        var itemId = itemResult.Value;

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));
        var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);

        item.Should().NotBeNull();
        item!.Title.Should().Be(command.Title);
        item.CreatedBy.Should().Be(userId.ToString()); 
        item.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
        item.LastModifiedBy.Should().Be(userId.ToString()); 
        item.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}

```

### tests\Application.FunctionalTests\TodoItems\Commands\DeleteTodoItemTests.cs

```csharp
﻿using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Commands.DeleteTodoItem;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class DeleteTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoItemId()
    {
        var command = new DeleteTodoItemCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await SendAsync(command);
        
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldDeleteTodoItem()
    {
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var itemResult = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "New Item"
        });
        
        itemResult.ShouldBeSuccessful();
        var itemId = itemResult.Value;

        var deleteResult = await SendAsync(new DeleteTodoItemCommand(listId, itemId));
        deleteResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));
        var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);

        item.Should().BeNull();
    }
}

```

### tests\Application.FunctionalTests\TodoItems\Commands\UpdateNonExistentItemTests.cs

```csharp
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class UpdateNonExistentItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFoundError_WhenItemDoesNotExist()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;
        
        var nonExistentItemId = Guid.NewGuid();
        
        // Act
        var command = new UpdateTodoItemCommand
        {
            Id = nonExistentItemId,
            ListId = listId,
            Title = "Updated Title",
            IsDone = true
        };
        
        var result = await SendAsync(command);
        
        // Assert
        result.ShouldBeFailure("TodoItems.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}

```

### tests\Application.FunctionalTests\TodoItems\Commands\UpdateTodoItemDetailTests.cs

```csharp
﻿using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItemDetail;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class UpdateTodoItemDetailTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoItemId()
    {
        var command = new UpdateTodoItemDetailCommand 
        { 
            ListId = Guid.NewGuid(), 
            Id = Guid.NewGuid(), 
            Note = "Test note",
            Priority = PriorityLevel.High
        };
        
        var result = await SendAsync(command);
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldUpdateTodoItemDetail()
    {
        var userId = await RunAsDefaultUserAsync();

        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var itemResult = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "New Item",
            Note = "Initial note",
            Priority = PriorityLevel.Low,
            Reminder = null
        });
        
        itemResult.ShouldBeSuccessful();
        var itemId = itemResult.Value;

        var command = new UpdateTodoItemDetailCommand
        {
            ListId = listId,
            Id = itemId,
            Note = "This is the note.",
            Priority = PriorityLevel.High,
            Reminder = DateTime.UtcNow.AddDays(1)
        };

        var updateResult = await SendAsync(command);
        updateResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));
        var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);

        item.Should().NotBeNull();
        item!.Note.Should().Be(command.Note);
        item.Priority.Should().Be(command.Priority);
        item.Reminder.Should().NotBeNull();
        command.Reminder.Should().NotBeNull();
        item.Reminder!.Value.Should().BeCloseTo(command.Reminder!.Value, TimeSpan.FromMilliseconds(10000));
        item.LastModifiedBy.Should().NotBeNull();
        item.LastModifiedBy.Should().Be(userId.ToString()); 
        item.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}

```

### tests\Application.FunctionalTests\TodoItems\Commands\UpdateTodoItemTests.cs

```csharp
﻿using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Commands;

using static Testing;

public class UpdateTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoItemId()
    {
        var command = new UpdateTodoItemCommand { ListId = Guid.NewGuid(), Id = Guid.NewGuid(), Title = "New Title" };
        var result = await SendAsync(command);
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldUpdateTodoItem()
    {
        var userId = await RunAsDefaultUserAsync();

        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var itemResult = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "New Item"
        });
        
        itemResult.ShouldBeSuccessful();
        var itemId = itemResult.Value;

        var command = new UpdateTodoItemCommand
        {
            ListId = listId,
            Id = itemId,
            Title = "Updated Item Title"
        };

        var updateResult = await SendAsync(command);
        updateResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));
        var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);

        item.Should().NotBeNull();
        item!.Title.Should().Be(command.Title);
        item.LastModifiedBy.Should().NotBeNull();
        item.LastModifiedBy.Should().Be(userId.ToString()); 
        item.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}

```

### tests\Application.FunctionalTests\TodoItems\Queries\GetTodoItemsWithPaginationTests.cs

```csharp
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate.Enums;

namespace YummyZoom.Application.FunctionalTests.TodoItems.Queries;

using static Testing;

public class GetTodoItemsWithPaginationTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyList_WhenNoItems()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "Empty List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;
        
        // Act
        var query = new GetTodoItemsWithPaginationQuery
        {
            ListId = listId,
            PageNumber = 1,
            PageSize = 10
        };
        
        var result = await SendAsync(query);
        
        // Assert
        result.ShouldBeSuccessful();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
    
    [Test]
    public async Task ShouldReturnAllItems_WithPagination()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "List with Items"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;
        
        // Create 5 items
        for (int i = 1; i <= 5; i++)
        {
            var itemResult = await SendAsync(new CreateTodoItemCommand
            {
                ListId = listId,
                Title = $"Item {i}",
                Priority = PriorityLevel.Medium
            });
            
            itemResult.ShouldBeSuccessful();
        }
        
        // Act - Get page 1 with 2 items per page
        var query1 = new GetTodoItemsWithPaginationQuery
        {
            ListId = listId,
            PageNumber = 1,
            PageSize = 2
        };
        
        var result1 = await SendAsync(query1);
        
        // Assert
        result1.ShouldBeSuccessful();
        result1.Value.Items.Should().HaveCount(2);
        result1.Value.TotalCount.Should().Be(5);
        result1.Value.TotalPages.Should().Be(3);
        result1.Value.HasNextPage.Should().BeTrue();
        result1.Value.HasPreviousPage.Should().BeFalse();
        
        // Act - Get page 2 with 2 items per page
        var query2 = new GetTodoItemsWithPaginationQuery
        {
            ListId = listId,
            PageNumber = 2,
            PageSize = 2
        };
        
        var result2 = await SendAsync(query2);
        
        // Assert
        result2.ShouldBeSuccessful();
        result2.Value.Items.Should().HaveCount(2);
        result2.Value.TotalCount.Should().Be(5);
        result2.Value.TotalPages.Should().Be(3);
        result2.Value.HasNextPage.Should().BeTrue();
        result2.Value.HasPreviousPage.Should().BeTrue();
    }
    
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoListId()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var nonExistentListId = Guid.NewGuid();
        
        // Act
        var query = new GetTodoItemsWithPaginationQuery
        {
            ListId = nonExistentListId,
            PageNumber = 1,
            PageSize = 10
        };
        
        var result = await SendAsync(query);
        
        // Assert
        result.ShouldBeFailure("TodoLists.NotFound");
    }
}

```

### tests\Application.FunctionalTests\TodoLists\Commands\CreateTodoListTests.cs

```csharp
﻿using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoLists.Commands;

using static Testing;

public class CreateTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateTodoListCommand();
        await FluentActions.Invoking(() => 
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldRequireUniqueTitle()
    {
        await SendAndUnwrapAsync(new CreateTodoListCommand
        {
            Title = "Shopping"
        });

        var command = new CreateTodoListCommand
        {
            Title = "Shopping"
        };

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ShouldCreateTodoList()
    {
        var userId = await RunAsDefaultUserAsync();

        var command = new CreateTodoListCommand
        {
            Title = "Tasks"
        };

        var result = await SendAsync(command);

        result.ShouldBeSuccessful();
        var id = result.Value;

        var list = await FindAsync<TodoList>(TodoListId.Create(id));

        list.Should().NotBeNull();
        list!.Title.Should().Be(command.Title);
        list.CreatedBy.Should().Be(userId.ToString()); 
        list.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}

```

### tests\Application.FunctionalTests\TodoLists\Commands\DeleteTodoListTests.cs

```csharp
﻿using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Application.TodoLists.Commands.DeleteTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoListId()
    {
        var command = new DeleteTodoListCommand(Guid.NewGuid());
        var result = await SendAsync(command);
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var deleteResult = await SendAsync(new DeleteTodoListCommand(listId));
        deleteResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));

        list.Should().BeNull();
    }
}

```

### tests\Application.FunctionalTests\TodoLists\Commands\PurgeTodoListsTests.cs

```csharp
﻿using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Application.TodoLists.Commands.PurgeTodoLists;
using YummyZoom.Domain.TodoListAggregate;

namespace YummyZoom.Application.FunctionalTests.TodoLists.Commands;

using static Testing;

public class PurgeTodoListsTests : BaseTestFixture
{
    [Test]
    public async Task ShouldDenyAnonymousUser()
    {
        var command = new PurgeTodoListsCommand();

        command.GetType().Should().BeDecoratedWith<AuthorizeAttribute>();

        var action = () => SendAsync(command);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task ShouldDenyNonAdministrator()
    {
        await RunAsDefaultUserAsync();

        var command = new PurgeTodoListsCommand();

        var action = () => SendAsync(command);

        await action.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task ShouldAllowAdministrator()
    {
        await RunAsAdministratorAsync();

        var command = new PurgeTodoListsCommand();

        var action = () => SendAsync(command);

        await action.Should().NotThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task ShouldDeleteAllLists()
    {
        await RunAsAdministratorAsync();

        await SendAsync(new CreateTodoListCommand
        {
            Title = "New List #1"
        });

        await SendAsync(new CreateTodoListCommand
        {
            Title = "New List #2"
        });

        await SendAsync(new CreateTodoListCommand
        {
            Title = "New List #3"
        });

        await SendAsync(new PurgeTodoListsCommand());

        var count = await CountAsync<TodoList>();

        count.Should().Be(0);
    }
}

```

### tests\Application.FunctionalTests\TodoLists\Commands\UpdateTodoListTests.cs

```csharp
﻿using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Application.TodoLists.Commands.UpdateTodoList;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoLists.Commands;

using static Testing;

public class UpdateTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnNotFound_ForInvalidTodoListId()
    {
        var command = new UpdateTodoListCommand { Id = Guid.NewGuid(), Title = "New Title" };
        var result = await SendAsync(command);
        result.ShouldBeFailure("TodoLists.NotFound");
    }

    [Test]
    public async Task ShouldRequireUniqueTitle()
    {
        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        
        await SendAsync(new CreateTodoListCommand
        {
            Title = "Other List"
        });

        var command = new UpdateTodoListCommand
        {
            Id = listResult.Value,
            Title = "Other List"
        };

        (await FluentActions.Invoking(() =>
            SendAsync(command))
                .Should().ThrowAsync<ValidationException>().Where(ex => ex.Errors.ContainsKey("Title")))
                .And.Errors["Title"].Should().Contain("'Title' must be unique.");
    }

    [Test]
    public async Task ShouldUpdateTodoList()
    {
        var userId = await RunAsDefaultUserAsync();

        var listResult = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });
        
        listResult.ShouldBeSuccessful();
        var listId = listResult.Value;

        var command = new UpdateTodoListCommand
        {
            Id = listId,
            Title = "Updated List Title"
        };

        var updateResult = await SendAsync(command);
        updateResult.ShouldBeSuccessful();

        var list = await FindAsync<TodoList>(TodoListId.Create(listId));

        list.Should().NotBeNull();
        list!.Title.Should().Be(command.Title);
        list.LastModifiedBy.Should().NotBeNull();
        list.LastModifiedBy.Should().Be(userId.ToString()); 
        list.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10000));
    }
}

```

### tests\Application.FunctionalTests\TodoLists\Queries\GetTodosTests.cs

```csharp
﻿using YummyZoom.Application.TodoLists.Queries.GetTodos;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.TodoLists.Queries;

using static Testing;

public class GetTodosTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnPriorityLevels()
    {
        await RunAsDefaultUserAsync();

        var query = new GetTodosQuery();

        var result = await SendAsync(query);

        result.ShouldBeSuccessful();
        result.Value.PriorityLevels.Should().NotBeEmpty();
    }

    [Test]
    public async Task ShouldReturnAllListsAndItems()
    {
        await RunAsDefaultUserAsync();

        var todoList = TodoList.Create("Shopping", Color.Blue);

        var apples = TodoItem.Create("Apples", null, PriorityLevel.None, null);
        apples.Complete();
        todoList.AddItem(apples);

        var milk = TodoItem.Create("Milk", null, PriorityLevel.None, null);
        milk.Complete();
        todoList.AddItem(milk);

        var bread = TodoItem.Create("Bread", null, PriorityLevel.None, null);
        bread.Complete();
        todoList.AddItem(bread);

        todoList.AddItem(TodoItem.Create("Toilet paper", null, PriorityLevel.None, null));
        todoList.AddItem(TodoItem.Create("Pasta", null, PriorityLevel.None, null));
        todoList.AddItem(TodoItem.Create("Tissues", null, PriorityLevel.None, null));
        todoList.AddItem(TodoItem.Create("Tuna", null, PriorityLevel.None, null));

        await AddAsync(todoList);

        var query = new GetTodosQuery();

        var result = await SendAsync(query);

        result.ShouldBeSuccessful();
        result.Value.Lists.Should().HaveCount(1);
        result.Value.Lists.First().Items.Should().HaveCount(7);
    }

    [Test]
    public async Task ShouldDenyAnonymousUser()
    {
        var query = new GetTodosQuery();

        var action = () => SendAsync(query);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

```

### tests\Application.FunctionalTests\Users\RegisterUserTests.cs

```csharp
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors; 
using YummyZoom.Infrastructure.Identity; 
using YummyZoom.Application.Users.Commands.RegisterUser; 
using YummyZoom.SharedKernel.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace YummyZoom.Application.FunctionalTests.Users;

using static Testing;

public class RegisterUserTests : BaseTestFixture
{
    [SetUp] 
    public async Task TestSetup()
    {
        await SetupForUserRegistrationTestsAsync();
    }
    
    [Test]
    public async Task RegisterUser_WithValidData_ShouldSucceedAndCreateBothUsers()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = "Reg User",
            Email = "register.test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var returnedUserId = result.Value;
        returnedUserId.Should().NotBeEmpty(); 

        // Verify ApplicationUser created
        var appUser = await FindAsync<ApplicationUser>(returnedUserId);
        appUser.Should().NotBeNull();
        appUser!.UserName.Should().Be(command.Email); // Username is always email
        appUser.Email.Should().Be(command.Email);
        appUser.Id.Should().Be(returnedUserId); // Verify the ID matches

        // Verify Domain UserAggregate created
        var domainUserId = UserId.Create(returnedUserId);
        var domainUser = await FindAsync<User>(domainUserId); 
        domainUser.Should().NotBeNull();
        domainUser!.Id.Should().Be(domainUserId);
        // Assert the Name property
        domainUser.Name.Should().Be(command.Name);
        domainUser.Email.Should().Be(command.Email);
        // Assuming AuditableEntityInterceptor uses IUser.Id (string representation of Guid)
        domainUser.CreatedBy.Should().Be(returnedUserId.ToString());
    }

    [Test]
    public async Task RegisterUser_ShouldAssignCustomerRole()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = "Role Test User",
            Email = "role.test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var returnedUserId = result.Value;

        // Get the created user
        var appUser = await FindAsync<ApplicationUser>(returnedUserId);
        appUser.Should().NotBeNull();

        // Verify the user is assigned to the Customer role
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var isInRole = await userManager.IsInRoleAsync(appUser!, Roles.Customer);
        isInRole.Should().BeTrue("User should be assigned to the Customer role");
    }

    [Test]
    public async Task RegisterUser_WithDuplicateEmail_ShouldReturnFailureResult()
    {
        // Arrange
        var command1 = new RegisterUserCommand 
        {
            Name = "Dupe Email",
            Email = "dupe.email@example.com", // Duplicate Email
            Password = "Password123!"
        };
        var result1 = await SendAsync(command1);
        result1.ShouldBeSuccessful();

        var command2 = new RegisterUserCommand 
        {
            Name = "Another Person",
            Email = "dupe.email@example.com", // Duplicate Email
            Password = "Password456!"
        };

        // Act
        var result2 = await SendAsync(command2);

        // Assert
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be(UserErrors.DuplicateEmail(command2.Email).Code);
    }


    [Test]
    public async Task RegisterUser_WithMissingFields_ShouldFailValidation()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = null, // Testing missing Name, a required field
            Email = "missing.fields@example.com",
            Password = "Password123!"
        };

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        // The ValidationBehaviour should catch this before the handler
        await act.Should().ThrowAsync<ValidationException>();
    }
    
    [Test]
    public async Task RegisterUser_WithInvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = "Invalid Email",
            Email = "invalid-email", // Invalid format
            Password = "Password123!"
        };

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
    
     [Test]
    public async Task RegisterUser_WithShortPassword_ShouldFailValidation()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = "Short Password",
            Email = "short.pw@example.com", 
            Password = "123" // Too short
        };

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}

```

### tests\Application.FunctionalTests\Users\RoleAssignmentTests.cs

```csharp
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.Application.Users.Commands.AssignRoleToUser;
using YummyZoom.Application.Users.Commands.RemoveRoleFromUser;
using YummyZoom.SharedKernel.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Users.Commands.RegisterUser;

namespace YummyZoom.Application.FunctionalTests.Users;

using static Testing;

public class RoleAssignmentTests : BaseTestFixture
{
    private Guid _administratorUserId;
    private Guid _testUserId;

    [SetUp]
    public async Task TestSetup()
    {
        await ResetState(); // Start with a clean database for each test
        _administratorUserId = await RunAsAdministratorAsync(); // Run tests as administrator
        
        // Create a test user to assign/remove roles from
        var registerUserCommand = new RegisterUserCommand
        {
            Name = "Test User",
            Email = "test.user@example.com",
            Password = "Password123!"
        };
        var registerResult = await SendAsync(registerUserCommand);
        registerResult.ShouldBeSuccessful();
        _testUserId = registerResult.Value;

        // Ensure the test user is initially only in the Customer role (default)
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = await userManager.GetRolesAsync(await userManager.FindByIdAsync(_testUserId.ToString()));
        roles.Should().Contain(Roles.Customer);
        roles.Count.Should().Be(1);
    }

    [Test]
    public async Task AssignRoleToUser_WithValidData_ShouldSucceedAndAssignRole()
    {
        // Arrange
        var command = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify the role is assigned in Identity
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByIdAsync(_testUserId.ToString());
        appUser.Should().NotBeNull();
        var isInRole = await userManager.IsInRoleAsync(appUser!, Roles.RestaurantOwner);
        isInRole.Should().BeTrue("User should be assigned to the RestaurantOwner role in Identity.");

        // Verify the role assignment is added to the Domain UserAggregate
        var domainUser = await FindAsync<User>(UserId.Create(_testUserId));
        domainUser.Should().NotBeNull();
        domainUser!.UserRoles.Should().ContainSingle(ra => ra.RoleName == Roles.RestaurantOwner);
    }

    [Test]
    public async Task AssignRoleToUser_WhenUserNotFound_ShouldReturnFailureResult()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var command = new AssignRoleToUserCommand(
            UserId: nonExistentUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.UserNotFound(nonExistentUserId).Code);
    }

    [Test]
    public async Task AssignRoleToUser_WithInvalidRoleName_ShouldReturnFailureResult()
    {
        // Arrange
        var invalidRoleName = "InvalidRole";
        var command = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: invalidRoleName,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Assuming RoleAssignment.Create returns a specific error for invalid role names
        // Need to check the actual error code returned by RoleAssignment.Create if different
        result.Error.Code.Should().Be(Domain.UserAggregate.Errors.RoleAssignmentErrors.InvalidRoleName(invalidRoleName).Code);
    }

    [Test]
    public async Task AssignRoleToUser_WhenRoleAlreadyAssigned_ShouldSucceedAndNotDuplicate()
    {
        // Arrange
        // User is already in Customer role from setup
        var command = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.Customer,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful(); // Domain logic treats adding existing role as success

        // Verify the role is still assigned in Identity (no change expected)
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByIdAsync(_testUserId.ToString());
        appUser.Should().NotBeNull();
        var isInRole = await userManager.IsInRoleAsync(appUser!, Roles.Customer);
        isInRole.Should().BeTrue("User should still be in the Customer role in Identity.");

        // Verify the role assignment is still only one in the Domain UserAggregate
        var domainUser = await FindAsync<User>(UserId.Create(_testUserId));
        domainUser.Should().NotBeNull();
        domainUser!.UserRoles.Should().ContainSingle(ra => ra.RoleName == Roles.Customer);
    }

    [Test]
    public async Task AssignRoleToUser_AsNonAdministrator_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        await RunAsDefaultUserAsync(); // Run as a non-administrator user
        var command = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RemoveRoleFromUser_WithValidData_ShouldSucceedAndRemoveRole()
    {
        // Arrange
        // First assign a role to remove
        var assignCommand = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);
        var assignResult = await SendAsync(assignCommand);
        assignResult.ShouldBeSuccessful();

        // Verify role is assigned before removing
        using var scopeBeforeRemove = CreateScope();
        var userManagerBeforeRemove = scopeBeforeRemove.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUserBeforeRemove = await userManagerBeforeRemove.FindByIdAsync(_testUserId.ToString());
        await userManagerBeforeRemove.IsInRoleAsync(appUserBeforeRemove!, Roles.RestaurantOwner).Should().BeTrue();

        var command = new RemoveRoleFromUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify the role is removed in Identity
        using var scopeAfterRemove = CreateScope();
        var userManagerAfterRemove = scopeAfterRemove.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUserAfterRemove = await userManagerAfterRemove.FindByIdByIdAsync(_testUserId.ToString());
        await userManagerAfterRemove.IsInRoleAsync(appUserAfterRemove!, Roles.RestaurantOwner).Should().BeFalse("User should be removed from the RestaurantOwner role in Identity.");

        // Verify the role assignment is removed from the Domain UserAggregate
        var domainUser = await FindAsync<User>(UserId.Create(_testUserId));
        domainUser.Should().NotBeNull();
        domainUser!.UserRoles.Should().NotContain(ra => ra.RoleName == Roles.RestaurantOwner);
    }

    [Test]
    public async Task RemoveRoleFromUser_WhenUserNotFound_ShouldReturnFailureResult()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var command = new RemoveRoleFromUserCommand(
            UserId: nonExistentUserId,
            RoleName: Roles.Customer,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.UserNotFound(nonExistentUserId).Code);
    }

    [Test]
    public async Task RemoveRoleFromUser_WhenRoleNotAssigned_ShouldReturnFailureResult()
    {
        // Arrange
        // User is only in Customer role from setup
        var command = new RemoveRoleFromUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner, // Role not assigned
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.RoleNotFound(Roles.RestaurantOwner).Code);
    }

    [Test]
    public async Task RemoveRoleFromUser_WhenRemovingLastRole_ShouldReturnFailureResult()
    {
        // Arrange
        // User is only in Customer role from setup
        var command = new RemoveRoleFromUserCommand(
            UserId: _testUserId,
            RoleName: Roles.Customer, // The only assigned role
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.CannotRemoveLastRole.Code);

        // Verify the role was NOT removed in Identity
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByIdAsync(_testUserId.ToString());
        appUser.Should().NotBeNull();
        var isInRole = await userManager.IsInRoleAsync(appUser!, Roles.Customer);
        isInRole.Should().BeTrue("User should still be in the Customer role in Identity.");

        // Verify the role assignment was NOT removed from the Domain UserAggregate
        var domainUser = await FindAsync<User>(UserId.Create(_testUserId));
        domainUser.Should().NotBeNull();
        domainUser!.UserRoles.Should().ContainSingle(ra => ra.RoleName == Roles.Customer);
    }

    [Test]
    public async Task RemoveRoleFromUser_AsNonAdministrator_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        // First assign a role as administrator
        var assignCommand = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);
        await SendAsync(assignCommand);

        await RunAsDefaultUserAsync(); // Run as a non-administrator user
        var command = new RemoveRoleFromUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}

```

### tests\Application.UnitTests\Application.UnitTests.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <RootNamespace>YummyZoom.Application.UnitTests</RootNamespace>
        <AssemblyName>YummyZoom.Application.UnitTests</AssemblyName>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Azure.Identity" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="nunit" />
        <PackageReference Include="NUnit.Analyzers">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="NUnit3TestAdapter" />
        <PackageReference Include="coverlet.collector">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="FluentAssertions" />
        <PackageReference Include="Moq" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Application\Application.csproj" />
        <ProjectReference Include="..\..\src\Infrastructure\Infrastructure.csproj" />
    </ItemGroup>

</Project>

```

### tests\Application.UnitTests\Common\Behaviours\RequestLoggerTests.cs

```csharp
﻿using YummyZoom.Application.Common.Behaviours;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace YummyZoom.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateTodoItemCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;
    private Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>> _next = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateTodoItemCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());
        _next = new Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>>();

        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand, MediatR.Unit>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Handle(new CreateTodoItemCommand { ListId = Guid.NewGuid(), Title = "title" }, _next.Object, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Once); 
        _next.Verify(n => n(), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        _next = new Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>>();
        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand, MediatR.Unit>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Handle(new CreateTodoItemCommand { ListId = Guid.NewGuid(), Title = "title" }, _next.Object, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Never); 
        _next.Verify(n => n(), Times.Once);
    }
}

```

### tests\Application.UnitTests\Common\Exceptions\ValidationExceptionTests.cs

```csharp
﻿using YummyZoom.Application.Common.Exceptions;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace YummyZoom.Application.UnitTests.Common.Exceptions;

public class ValidationExceptionTests
{
    [Test]
    public void DefaultConstructorCreatesAnEmptyErrorDictionary()
    {
        var actual = new ValidationException().Errors;

        actual.Keys.Should().BeEquivalentTo(Array.Empty<string>());
    }

    [Test]
    public void SingleValidationFailureCreatesASingleElementErrorDictionary()
    {
        var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Age", "must be over 18"),
            };

        var actual = new ValidationException(failures).Errors;

        actual.Keys.Should().BeEquivalentTo(["Age"]);
        actual["Age"].Should().BeEquivalentTo(["must be over 18"]);
    }

    [Test]
    public void MulitpleValidationFailureForMultiplePropertiesCreatesAMultipleElementErrorDictionaryEachWithMultipleValues()
    {
        var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Age", "must be 18 or older"),
                new ValidationFailure("Age", "must be 25 or younger"),
                new ValidationFailure("Password", "must contain at least 8 characters"),
                new ValidationFailure("Password", "must contain a digit"),
                new ValidationFailure("Password", "must contain upper case letter"),
                new ValidationFailure("Password", "must contain lower case letter"),
            };

        var actual = new ValidationException(failures).Errors;

        actual.Keys.Should().BeEquivalentTo(["Password", "Age"]);

        actual["Age"].Should().BeEquivalentTo(
        [
                "must be 25 or younger",
                "must be 18 or older",
        ]);

        actual["Password"].Should().BeEquivalentTo(
        [
                "must contain lower case letter",
                "must contain upper case letter",
                "must contain at least 8 characters",
                "must contain a digit",
        ]);
    }
}

```

### tests\Application.UnitTests\Common\Mappings\MappingTests.cs

```csharp
﻿using System.Reflection;
using System.Runtime.CompilerServices;
using AutoMapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;
using YummyZoom.Application.TodoLists.Queries.GetTodos;
using NUnit.Framework;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Application.UnitTests.Common.Mappings;

public class MappingTests
{
    private readonly IConfigurationProvider _configuration;
    private readonly IMapper _mapper;

    public MappingTests()
    {
        _configuration = new MapperConfiguration(config => 
            config.AddMaps(Assembly.GetAssembly(typeof(IApplicationDbContext))));

        _mapper = _configuration.CreateMapper();
    }

    [Test]
    public void ShouldHaveValidConfiguration()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Test]
    [TestCase(typeof(TodoList), typeof(TodoListDto))]
    [TestCase(typeof(TodoItem), typeof(TodoItemDto))]
    [TestCase(typeof(TodoList), typeof(LookupDto))]
    [TestCase(typeof(TodoItem), typeof(LookupDto))]
    [TestCase(typeof(TodoItem), typeof(TodoItemBriefDto))]
    public void ShouldSupportMappingFromSourceToDestination(Type source, Type destination)
    {
        var instance = GetInstanceOf(source);

        _mapper.Map(instance, source, destination);
    }

    private object GetInstanceOf(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) != null)
            return Activator.CreateInstance(type)!;

        // Type without parameterless constructor
        return RuntimeHelpers.GetUninitializedObject(type);
    }
}

```

### tests\Domain.UnitTests\Domain.UnitTests.csproj

```xml
﻿<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <RootNamespace>YummyZoom.Domain.UnitTests</RootNamespace>
        <AssemblyName>YummyZoom.Domain.UnitTests</AssemblyName>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="nunit" />
        <PackageReference Include="NUnit.Analyzers">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="NUnit3TestAdapter" />
        <PackageReference Include="coverlet.collector">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="FluentAssertions" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Domain\Domain.csproj" />
    </ItemGroup>

</Project>

```

### tests\Domain.UnitTests\Common\ValueObjects\AddressTests.cs

```csharp
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Common.ValueObjects;

[TestFixture]
public class AddressTests
{
    [Test]
    public void Create_WithValidInputs_ShouldReturnAddress()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Anytown";
        var state = "CA";
        var zipCode = "91234";
        var country = "USA";
        var label = "Home";
        var deliveryInstructions = "Leave at the door";

        // Act
        var address = Address.Create(street, city, state, zipCode, country, label, deliveryInstructions);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.ZipCode.Should().Be(zipCode);
        address.Country.Should().Be(country);
        address.Label.Should().Be(label);
        address.DeliveryInstructions.Should().Be(deliveryInstructions);
    }

    [Test]
    public void Create_WithOnlyRequiredInputs_ShouldReturnAddressWithNullOptionals()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Anytown";
        var state = "CA";
        var zipCode = "91234";
        var country = "USA";

        // Act
        var address = Address.Create(street, city, state, zipCode, country);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.ZipCode.Should().Be(zipCode);
        address.Country.Should().Be(country);
        address.Label.Should().BeNull();
        address.DeliveryInstructions.Should().BeNull();
    }

    [Test]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Anytown";
        var state = "CA";
        var zipCode = "91234";
        var country = "USA";
        var label = "Home";
        var deliveryInstructions = "Leave at the door";

        var address1 = Address.Create(street, city, state, zipCode, country, label, deliveryInstructions);
        var address2 = Address.Create(street, city, state, zipCode, country, label, deliveryInstructions);

        // Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Home", "Leave at the door");
        var address2 = Address.Create("456 Oak Ave", "Otherville", "NY", "56789", "USA", "Work", "Ring the bell"); // Different values

        var address3 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Home", "Leave at the door");
        var address4 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Work", "Leave at the door"); // Different label

        var address5 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Home", "Leave at the door");
        var address6 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Home", "Ring the bell"); // Different delivery instructions


        // Assert
        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();

        address3.Should().NotBe(address4);
        (address3 == address4).Should().BeFalse();

        address5.Should().NotBe(address6);
        (address5 == address6).Should().BeFalse();
    }
}

```

### tests\Domain.UnitTests\TodoListAggregate\TodoListTests.cs

```csharp
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TodoListAggregate;

[TestFixture]
public class TodoListTests
{
    private const string DefaultTodoListTitle = "Shopping List";
    private const string AnotherTodoListTitle = "Work Tasks";
    private const string DefaultTodoItemTitle = "Buy Milk";

    // Helper to create a new TodoItem with default or specified values for adding
    private TodoItem CreateNewTodoItem(string title = DefaultTodoItemTitle, PriorityLevel priority = PriorityLevel.None, string? note = null)
    {
        // Creates an item with a new ID, listId is null as it's not yet added to a list
        return TodoItem.Create(title, note, priority, null);
    }

    // Helper to create a TodoItem instance meant to represent an update to an existing item
    private TodoItem CreateUpdatedTodoItem(TodoItemId id, string title, PriorityLevel priority = PriorityLevel.None, string? note = null)
    {
        // Uses an existing ID, assumes the Create overload for existing items does not take listId
        return TodoItem.Create(id, title, note, priority, null);
    }

    [Test]
    public void Create_WithValidTitle_ShouldInitializeTodoListWithTitleAndDefaultColor()
    {
        // Act
        var todoList = TodoList.Create(DefaultTodoListTitle);

        // Assert
        todoList.Should().NotBeNull();
        todoList.Id.Should().NotBeNull(); // Assuming TodoListId is generated
        todoList.Title.Should().Be(DefaultTodoListTitle);
        todoList.Color.Should().Be(Color.White); // Default color
        todoList.Items.Should().BeEmpty();
    }

    [Test]
    public void Create_WithValidTitleAndSpecifiedColor_ShouldInitializeTodoListCorrectly()
    {
        // Arrange
        var color = Color.Blue;

        // Act
        var todoList = TodoList.Create(AnotherTodoListTitle, color);

        // Assert
        todoList.Should().NotBeNull();
        todoList.Id.Should().NotBeNull();
        todoList.Title.Should().Be(AnotherTodoListTitle);
        todoList.Color.Should().Be(color);
        todoList.Items.Should().BeEmpty();
    }

    [Test]
    public void UpdateTitle_WithValidNewTitle_ShouldUpdateTodoListTitle()
    {
        // Arrange
        var todoList = TodoList.Create("Old Title");
        const string newTitle = "New Valid Title";

        // Act
        todoList.UpdateTitle(newTitle);

        // Assert
        todoList.Title.Should().Be(newTitle);
    }

    [Test]
    public void UpdateColour_WithNewColor_ShouldUpdateTodoListColour()
    {
        // Arrange
        var todoList = TodoList.Create(DefaultTodoListTitle, Color.White);
        var newColor = Color.Green;

        // Act
        todoList.UpdateColour(newColor);

        // Assert
        todoList.Color.Should().Be(newColor);
    }

    [Test]
    public void AddItem_WithValidItem_ShouldAddTodoItemToList()
    {
        // Arrange
        var todoList = TodoList.Create(DefaultTodoListTitle);
        var todoItem = CreateNewTodoItem();

        // Act
        todoList.AddItem(todoItem);

        // Assert
        todoList.Items.Should().ContainSingle();
        todoList.Items.Should().Contain(todoItem);
    }

    [Test]
    public void RemoveItem_ExistingItem_ShouldRemoveTodoItemFromList()
    {
        // Arrange
        var todoList = TodoList.Create(DefaultTodoListTitle);
        var item1 = CreateNewTodoItem("Task 1");
        var item2 = CreateNewTodoItem("Task 2");
        todoList.AddItem(item1);
        todoList.AddItem(item2);

        // Act
        todoList.RemoveItem(item1); // Pass the TodoItem object

        // Assert
        todoList.Items.Should().ContainSingle();
        todoList.Items.Should().NotContain(i => i.Id == item1.Id);
        todoList.Items.Should().Contain(i => i.Id == item2.Id);
    }

    [Test]
    public void UpdateItem_ExistingItem_ShouldUpdateExistingTodoItemInList()
    {
        // Arrange
        var todoList = TodoList.Create(DefaultTodoListTitle);
        var originalItem = CreateNewTodoItem("Initial Task", PriorityLevel.Low);
        todoList.AddItem(originalItem);

        var updatedItemInstance = CreateUpdatedTodoItem(originalItem.Id, "Updated Task", PriorityLevel.High);

        // Act
        todoList.UpdateItem(updatedItemInstance);

        // Assert
        todoList.Items.Should().ContainSingle();
        var itemInList = todoList.Items.Single();
        itemInList.Id.Should().Be(originalItem.Id);
        itemInList.Title.Should().Be("Updated Task");
        itemInList.Priority.Should().Be(PriorityLevel.High);
    }
}

```

### tests\Domain.UnitTests\UserAggregate\UserAggregateTests.cs

```csharp
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;
using System.Collections.Generic;
using System;
using System.Linq;

namespace YummyZoom.Domain.UnitTests.UserAggregate;

[TestFixture]
public class UserAggregateTests
{
    private const string DefaultUserName = "John Doe";
    private const string DefaultUserEmail = "john.doe@example.com";
    private const string DefaultUserPhoneNumber = "123-456-7890";
    private const string DefaultRoleName = "Customer";

    // Helper method to create a valid RoleAssignment for testing
    private static RoleAssignment CreateValidRoleAssignment(string roleName = DefaultRoleName, string? targetEntityId = null, string? targetEntityType = null)
    {
        var result = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);
        result.IsSuccess.Should().BeTrue("because the role assignment inputs are valid");
        return result.Value;
    }

    // Helper method to create a valid Address for testing
    private static Address CreateValidAddress(string label = "Home")
    {
        var result = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", label);
        // Address Create method doesn't return Result, assuming valid inputs create valid Address
        return result;
    }

     // Helper method to create a valid PaymentMethod for testing
    private static PaymentMethod CreateValidPaymentMethod(string type = "Card", string tokenizedDetails = "tok_test", bool isDefault = false)
    {
        var result = PaymentMethod.Create(type, tokenizedDetails, isDefault);
        // PaymentMethod Create method doesn't return Result, assuming valid inputs create valid PaymentMethod
        return result;
    }


    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeUserCorrectly()
    {
        // Arrange
        var roles = new List<RoleAssignment> { CreateValidRoleAssignment() };

        // Act
        var result = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, roles);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var user = result.Value;

        user.Id.Value.Should().NotBe(Guid.Empty, "because a unique UserId should be generated");
        user.Name.Should().Be(DefaultUserName);
        user.Email.Should().Be(DefaultUserEmail);
        user.PhoneNumber.Should().Be(DefaultUserPhoneNumber);
        user.UserRoles.Should().ContainSingle();
        user.UserRoles.First().RoleName.Should().Be(DefaultRoleName);
        user.Addresses.Should().BeEmpty();
        user.PaymentMethods.Should().BeEmpty();
    }

    [Test]
    public void Create_WithNullRoles_ShouldFailAndReturnMustHaveAtLeastOneRoleError()
    {
        // Arrange
        List<RoleAssignment> roles = null!; // Use null-forgiving operator or cast

        // Act
        var result = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, roles); // The cast might be needed here depending on compiler

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.MustHaveAtLeastOneRole);
    }

     [Test]
    public void Create_WithEmptyRoles_ShouldFailAndReturnMustHaveAtLeastOneRoleError()
    {
        // Arrange
        var roles = new List<RoleAssignment>();

        // Act
        var result = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, roles);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.MustHaveAtLeastOneRole);
    }

    [Test]
    public void AddAddress_WithValidAddress_ShouldAddAddressToAddressesCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var address = CreateValidAddress();

        // Act
        var result = user.AddAddress(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Addresses.Should().ContainSingle();
        user.Addresses.Should().Contain(address);
    }

    [Test]
    public void RemoveAddress_ExistingAddress_ShouldRemoveAddressFromAddressesCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var address1 = CreateValidAddress("Home");
        var address2 = CreateValidAddress("Work");
        user.AddAddress(address1);
        user.AddAddress(address2);
        user.Addresses.Should().HaveCount(2);

        // Act
        var result = user.RemoveAddress(address1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Addresses.Should().ContainSingle();
        user.Addresses.Should().NotContain(address1);
        user.Addresses.Should().Contain(address2);
    }

    [Test]
    public void RemoveAddress_NonExistentAddress_ShouldReturnSuccess()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var existingAddress = CreateValidAddress("Home");
        user.AddAddress(existingAddress);
        user.Addresses.Should().ContainSingle();
        var nonExistentAddress = CreateValidAddress("Other"); // Different address

        // Act
        var result = user.RemoveAddress(nonExistentAddress);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Based on current domain implementation
        user.Addresses.Should().ContainSingle(); // Address should not have been removed
        user.Addresses.Should().Contain(existingAddress);
    }

    [Test]
    public void AddPaymentMethod_WithValidPaymentMethod_ShouldAddPaymentMethodToCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var paymentMethod = CreateValidPaymentMethod();

        // Act
        var result = user.AddPaymentMethod(paymentMethod);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PaymentMethods.Should().ContainSingle();
        user.PaymentMethods.Should().Contain(paymentMethod);
    }

    [Test]
    public void RemovePaymentMethod_ExistingPaymentMethod_ShouldRemovePaymentMethodFromCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var paymentMethod1 = CreateValidPaymentMethod("Card");
        var paymentMethod2 = CreateValidPaymentMethod("PayPal");
        user.AddPaymentMethod(paymentMethod1);
        user.AddPaymentMethod(paymentMethod2);
        user.PaymentMethods.Should().HaveCount(2);

        // Act
        var result = user.RemovePaymentMethod(paymentMethod1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PaymentMethods.Should().ContainSingle();
        user.PaymentMethods.Should().NotContain(paymentMethod1);
        user.PaymentMethods.Should().Contain(paymentMethod2);
    }

    [Test]
    public void RemovePaymentMethod_NonExistentPaymentMethod_ShouldFailAndReturnPaymentMethodNotFound()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var existingPaymentMethod = CreateValidPaymentMethod("Card");
        user.AddPaymentMethod(existingPaymentMethod);
        user.PaymentMethods.Should().ContainSingle();
        var nonExistentPaymentMethodId = PaymentMethodId.CreateUnique(); // Non-existent ID

        // Act
        var result = user.RemovePaymentMethod(nonExistentPaymentMethodId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.PaymentMethodNotFound(nonExistentPaymentMethodId.Value));
        user.PaymentMethods.Should().ContainSingle(); // Payment method should not have been removed
        user.PaymentMethods.Should().Contain(existingPaymentMethod);
    }

    [Test]
    public void UpdateProfile_WithValidInputs_ShouldUpdateNameAndPhoneNumber()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var newName = "Jane Doe";
        var newPhoneNumber = "987-654-3210";

        // Act
        var result = user.UpdateProfile(newName, newPhoneNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Name.Should().Be(newName);
        user.PhoneNumber.Should().Be(newPhoneNumber);
    }

    [Test]
    public void UpdateProfile_WithNullPhoneNumber_ShouldUpdateNameAndSetPhoneNumberToNull()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment() });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var newName = "Jane Doe";
        string? newPhoneNumber = null;

        // Act
        var result = user.UpdateProfile(newName, newPhoneNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Name.Should().Be(newName);
        user.PhoneNumber.Should().BeNull();
    }

    [Test]
    public void AddRole_WithValidRoleAssignment_ShouldAddRoleAssignmentToCollection()
    {
        // Arrange
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { CreateValidRoleAssignment("Customer") });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        var restaurantOwnerRole = CreateValidRoleAssignment("RestaurantOwner", Guid.NewGuid().ToString(), "Restaurant");

        // Act
        var result = user.AddRole(restaurantOwnerRole);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.UserRoles.Should().HaveCount(2);
        user.UserRoles.Should().Contain(restaurantOwnerRole);
    }

    [Test]
    public void AddRole_WithExistingRoleAssignment_ShouldReturnSuccessAndNotAddDuplicate()
    {
        // Arrange
        var customerRole = CreateValidRoleAssignment("Customer");
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { customerRole });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.UserRoles.Should().ContainSingle();

        // Act
        var result = user.AddRole(customerRole); // Add the same role again

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.UserRoles.Should().ContainSingle(); // Should not add a duplicate
        user.UserRoles.Should().Contain(customerRole);
    }

    [Test]
    public void RemoveRole_ExistingRoleAssignment_ShouldRemoveRoleAssignmentFromCollection()
    {
        // Arrange
        var customerRole = CreateValidRoleAssignment("Customer");
        var adminRole = CreateValidRoleAssignment("Admin");
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { customerRole, adminRole });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.UserRoles.Should().HaveCount(2);

        // Act
        var result = user.RemoveRole(adminRole.RoleName, adminRole.TargetEntityId, adminRole.TargetEntityType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.UserRoles.Should().ContainSingle();
        user.UserRoles.Should().NotContain(adminRole);
        user.UserRoles.Should().Contain(customerRole);
    }

    [Test]
    public void RemoveRole_LastRoleAssignment_ShouldFailAndReturnCannotRemoveLastRoleError()
    {
        // Arrange
        var customerRole = CreateValidRoleAssignment("Customer");
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { customerRole });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.UserRoles.Should().ContainSingle();

        // Act
        var result = user.RemoveRole(customerRole.RoleName, customerRole.TargetEntityId, customerRole.TargetEntityType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.CannotRemoveLastRole);
        user.UserRoles.Should().ContainSingle(); // Role should not have been removed
        user.UserRoles.Should().Contain(customerRole);
    }

    [Test]
    public void RemoveRole_NonExistentRoleAssignment_ShouldFailAndReturnRoleNotFound()
    {
        // Arrange
        var customerRole = CreateValidRoleAssignment("Customer");
        var userResult = User.Create(DefaultUserName, DefaultUserEmail, DefaultUserPhoneNumber, new List<RoleAssignment> { customerRole });
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.UserRoles.Should().ContainSingle();
        var nonExistentRole = CreateValidRoleAssignment("Admin"); // Non-existent role

        // Act
        var result = user.RemoveRole(nonExistentRole.RoleName, nonExistentRole.TargetEntityId, nonExistentRole.TargetEntityType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.RoleNotFound(nonExistentRole.RoleName)); // Reusing error for now
        user.UserRoles.Should().ContainSingle(); // Role should not have been removed
        user.UserRoles.Should().Contain(customerRole);
    }
}

```

### tests\Domain.UnitTests\UserAggregate\Entities\PaymentMethodTests.cs

```csharp
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using System;

namespace YummyZoom.Domain.UnitTests.UserAggregate.Entities;

[TestFixture]
public class PaymentMethodTests
{
    [Test]
    public void Create_WithValidInputs_ShouldReturnPaymentMethod()
    {
        // Arrange
        var type = "Card";
        var tokenizedDetails = "tok_test";
        var isDefault = false;

        // Act
        var paymentMethod = PaymentMethod.Create(type, tokenizedDetails, isDefault);

        // Assert
        paymentMethod.Should().NotBeNull();
        paymentMethod.Id.Should().NotBeNull();
        paymentMethod.Id.Value.Should().NotBe(Guid.Empty); 
        paymentMethod.Type.Should().Be(type);
        paymentMethod.TokenizedDetails.Should().Be(tokenizedDetails);
        paymentMethod.IsDefault.Should().Be(isDefault);
    }

    [Test]
    public void Equality_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var paymentMethodId = PaymentMethodId.CreateUnique();
        var paymentMethod1 = PaymentMethod.Create(paymentMethodId, "Card", "tok_test", false); 
        var paymentMethod2 = PaymentMethod.Create(paymentMethodId, "PayPal", "tok_other", true); 

        // Assert
        paymentMethod1.Should().Be(paymentMethod2);
        (paymentMethod1 == paymentMethod2).Should().BeTrue();
        paymentMethod1.GetHashCode().Should().Be(paymentMethod2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var paymentMethod1 = PaymentMethod.Create("Card", "tok_test", false);
        var paymentMethod2 = PaymentMethod.Create("Card", "tok_test", false); // Different IDs

        // Assert
        paymentMethod1.Should().NotBe(paymentMethod2);
        (paymentMethod1 != paymentMethod2).Should().BeTrue();
    }

    [Test]
    public void SetAsDefault_ShouldSetIsDefaultToTrue()
    {
        // Arrange
        var paymentMethod = PaymentMethod.Create("Card", "tok_test", false);
        paymentMethod.IsDefault.Should().BeFalse();

        // Act
        paymentMethod.SetAsDefault();

        // Assert
        paymentMethod.IsDefault.Should().BeTrue();
    }
}

```

### tests\Domain.UnitTests\UserAggregate\Errors\UserErrorsTests.cs

```csharp
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;
using System;

namespace YummyZoom.Domain.UnitTests.UserAggregate.Errors;

[TestFixture]
public class UserErrorsTests
{
    [Test]
    public void InvalidUserId_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var invalidValue = "some-invalid-id";

        // Act
        var error = UserErrors.InvalidUserId(invalidValue);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.InvalidUserId");
        error.Description.Should().Contain(invalidValue);
    }

    [Test]
    public void RoleNotFound_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var roleName = "NonExistentRole";

        // Act
        var error = UserErrors.RoleNotFound(roleName);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.RoleNotFound");
        error.Description.Should().Contain(roleName);
    }

    [Test]
    public void CannotRemoveLastRole_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Act
        var error = UserErrors.CannotRemoveLastRole;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.CannotRemoveLastRole");
        error.Description.Should().Be("Cannot remove the last role from the user.");
    }

    [Test]
    public void AddressNotFound_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var addressId = Guid.NewGuid();

        // Act
        var error = UserErrors.AddressNotFound(addressId);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.AddressNotFound");
        error.Description.Should().Contain(addressId.ToString());
    }

    [Test]
    public void PaymentMethodNotFound_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var paymentMethodId = Guid.NewGuid();

        // Act
        var error = UserErrors.PaymentMethodNotFound(paymentMethodId);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.PaymentMethodNotFound");
        error.Description.Should().Contain(paymentMethodId.ToString());
    }

    [Test]
    public void InvalidPaymentMethod_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Act
        var error = UserErrors.InvalidPaymentMethod;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.InvalidPaymentMethod");
        error.Description.Should().Be("Payment method is invalid.");
    }

     [Test]
    public void MustHaveAtLeastOneRole_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Act
        var error = UserErrors.MustHaveAtLeastOneRole;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.MustHaveAtLeastOneRole");
        error.Description.Should().Be("User must have at least one role.");
    }
}

```

### tests\Domain.UnitTests\UserAggregate\ValueObjects\PaymentMethodIdTests.cs

```csharp
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors; // Assuming InvalidPaymentMethod error is here
using YummyZoom.SharedKernel;
using System;

namespace YummyZoom.Domain.UnitTests.UserAggregate.ValueObjects;

[TestFixture]
public class PaymentMethodIdTests
{
    [Test]
    public void CreateUnique_ShouldReturnPaymentMethodIdWithNonEmptyGuidValue()
    {
        // Act
        var paymentMethodId = PaymentMethodId.CreateUnique();

        // Assert
        paymentMethodId.Should().NotBeNull();
        paymentMethodId.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void Create_WithValidGuid_ShouldReturnPaymentMethodIdWithCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var paymentMethodId = PaymentMethodId.Create(guid);

        // Assert
        paymentMethodId.Should().NotBeNull();
        paymentMethodId.Value.Should().Be(guid);
    }

    [Test]
    public void Create_WithValidGuidString_ShouldReturnSuccessResultWithPaymentMethodId()
    {
        // Arrange
        var guidString = Guid.NewGuid().ToString();

        // Act
        var result = PaymentMethodId.Create(guidString);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Value.ToString().Should().Be(guidString);
    }

    [Test]
    public void Create_WithInvalidGuidString_ShouldReturnFailureResultWithInvalidPaymentMethodError()
    {
        // Arrange
        var invalidGuidString = "invalid-guid-string";

        // Act
        var result = PaymentMethodId.Create(invalidGuidString);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Assuming UserErrors.InvalidPaymentMethod is used for invalid GUID string
        result.Error.Should().Be(UserErrors.InvalidPaymentMethod);
    }

    [Test]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var paymentMethodId1 = PaymentMethodId.Create(guid);
        var paymentMethodId2 = PaymentMethodId.Create(guid);

        // Assert
        paymentMethodId1.Should().Be(paymentMethodId2);
        (paymentMethodId1 == paymentMethodId2).Should().BeTrue();
        paymentMethodId1.GetHashCode().Should().Be(paymentMethodId2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var paymentMethodId1 = PaymentMethodId.CreateUnique();
        var paymentMethodId2 = PaymentMethodId.CreateUnique();

        // Assert
        paymentMethodId1.Should().NotBe(paymentMethodId2);
        (paymentMethodId1 != paymentMethodId2).Should().BeTrue();
    }
}

```

### tests\Domain.UnitTests\UserAggregate\ValueObjects\RoleAssignmentTests.cs

```csharp
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using System;

namespace YummyZoom.Domain.UnitTests.UserAggregate.ValueObjects;

[TestFixture]
public class RoleAssignmentTests
{
    [Test]
    public void Create_WithValidRoleName_ShouldSucceedAndReturnRoleAssignment()
    {
        // Arrange
        var roleName = "Customer";

        // Act
        var result = RoleAssignment.Create(roleName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var roleAssignment = result.Value;

        roleAssignment.RoleName.Should().Be(roleName);
        roleAssignment.TargetEntityId.Should().BeNull();
        roleAssignment.TargetEntityType.Should().BeNull();
    }

    [Test]
    public void Create_WithValidRoleNameAndTarget_ShouldSucceedAndReturnRoleAssignment()
    {
        // Arrange
        var roleName = "RestaurantOwner";
        var targetEntityId = Guid.NewGuid().ToString();
        var targetEntityType = "Restaurant";

        // Act
        var result = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var roleAssignment = result.Value;

        roleAssignment.RoleName.Should().Be(roleName);
        roleAssignment.TargetEntityId.Should().Be(targetEntityId);
        roleAssignment.TargetEntityType.Should().Be(targetEntityType);
    }

    [Test]
    public void Create_WithNullOrEmptyRoleName_ShouldFailAndReturnInvalidRoleNameError()
    {
        // Arrange
        string roleName = null!; // Test null
        string emptyRoleName = ""; // Test empty
        string whitespaceRoleName = "   "; // Test whitespace

        // Act & Assert for null
        var resultNull = RoleAssignment.Create(roleName);
        resultNull.IsFailure.Should().BeTrue();
        resultNull.Error.Code.Should().Be("RoleAssignment.InvalidRoleName");

        // Act & Assert for empty
        var resultEmpty = RoleAssignment.Create(emptyRoleName);
        resultEmpty.IsFailure.Should().BeTrue();
        resultEmpty.Error.Code.Should().Be("RoleAssignment.InvalidRoleName");

        // Act & Assert for whitespace
        var resultWhitespace = RoleAssignment.Create(whitespaceRoleName);
        resultWhitespace.IsFailure.Should().BeTrue();
        resultWhitespace.Error.Code.Should().Be("RoleAssignment.InvalidRoleName");
    }

    [Test]
    public void Create_WithTargetEntityIdButNoType_ShouldFailAndReturnInvalidTargetError()
    {
        // Arrange
        var roleName = "RestaurantStaff";
        var targetEntityId = Guid.NewGuid().ToString();
        string? targetEntityType = null;

        // Act
        var result = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoleAssignment.InvalidTarget");
    }

    [Test]
    public void Create_WithTargetEntityTypeButNoId_ShouldFailAndReturnInvalidTargetError()
    {
        // Arrange
        var roleName = "RestaurantStaff";
        string? targetEntityId = null;
        var targetEntityType = "Restaurant";

        // Act
        var result = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoleAssignment.InvalidTarget");
    }

    [Test]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var roleName = "Customer";
        var assignment1 = RoleAssignment.Create(roleName).Value;
        var assignment2 = RoleAssignment.Create(roleName).Value;

        var roleNameTarget = "RestaurantStaff";
        var targetId = Guid.NewGuid().ToString();
        var targetType = "Restaurant";
        var assignmentWithTarget1 = RoleAssignment.Create(roleNameTarget, targetId, targetType).Value;
        var assignmentWithTarget2 = RoleAssignment.Create(roleNameTarget, targetId, targetType).Value;


        // Assert
        assignment1.Should().Be(assignment2);
        (assignment1 == assignment2).Should().BeTrue();
        assignment1.GetHashCode().Should().Be(assignment2.GetHashCode());

        assignmentWithTarget1.Should().Be(assignmentWithTarget2);
        (assignmentWithTarget1 == assignmentWithTarget2).Should().BeTrue();
        assignmentWithTarget1.GetHashCode().Should().Be(assignmentWithTarget2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var assignment1 = RoleAssignment.Create("Customer").Value;
        var assignment2 = RoleAssignment.Create("Admin").Value; // Different role name

        var assignmentWithTarget1 = RoleAssignment.Create("RestaurantOwner", Guid.NewGuid().ToString(), "Restaurant").Value;
        var assignmentWithTarget2 = RoleAssignment.Create("RestaurantOwner", Guid.NewGuid().ToString(), "Restaurant").Value; // Different target ID

        var assignmentWithTarget3 = RoleAssignment.Create("RestaurantOwner", Guid.NewGuid().ToString(), "Restaurant").Value;
        var assignmentWithTarget4 = RoleAssignment.Create("RestaurantOwner", assignmentWithTarget3.TargetEntityId, "AnotherType").Value; // Different target type


        // Assert
        assignment1.Should().NotBe(assignment2);
        (assignment1 != assignment2).Should().BeTrue();

        assignmentWithTarget1.Should().NotBe(assignmentWithTarget2);
        (assignmentWithTarget1 != assignmentWithTarget2).Should().BeTrue();

        assignmentWithTarget3.Should().NotBe(assignmentWithTarget4);
        (assignmentWithTarget3 != assignmentWithTarget4).Should().BeTrue();
    }
}

```

### tests\Domain.UnitTests\UserAggregate\ValueObjects\UserIdTests.cs

```csharp
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;
using System;

namespace YummyZoom.Domain.UnitTests.UserAggregate.ValueObjects;

[TestFixture]
public class UserIdTests
{
    [Test]
    public void CreateUnique_ShouldReturnUserIdWithNonEmptyGuidValue()
    {
        // Act
        var userId = UserId.CreateUnique();

        // Assert
        userId.Should().NotBeNull();
        userId.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void Create_WithValidGuid_ShouldReturnUserIdWithCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var userId = UserId.Create(guid);

        // Assert
        userId.Should().NotBeNull();
        userId.Value.Should().Be(guid);
    }

    [Test]
    public void Create_WithValidGuidString_ShouldReturnSuccessResultWithUserId()
    {
        // Arrange
        var guidString = Guid.NewGuid().ToString();

        // Act
        var result = UserId.Create(guidString);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Value.ToString().Should().Be(guidString);
    }

    [Test]
    public void Create_WithInvalidGuidString_ShouldReturnFailureResultWithInvalidUserIdError()
    {
        // Arrange
        var invalidGuidString = "invalid-guid-string";

        // Act
        var result = UserId.Create(invalidGuidString);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidUserId(invalidGuidString));
    }

    [Test]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var userId1 = UserId.Create(guid);
        var userId2 = UserId.Create(guid);

        // Assert
        userId1.Should().Be(userId2);
        (userId1 == userId2).Should().BeTrue();
        userId1.GetHashCode().Should().Be(userId2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var userId1 = UserId.CreateUnique();
        var userId2 = UserId.CreateUnique();

        // Assert
        userId1.Should().NotBe(userId2);
        (userId1 != userId2).Should().BeTrue();
    }
}

```

### tests\Domain.UnitTests\ValueObjects\ColourTests.cs

```csharp
﻿using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.ValueObjects;

public class ColourTests
{
    [Test]
    public void ShouldReturnCorrectColourCode()
    {
        var code = "#FFFFFF";

        var colour = Color.Create(code);

        colour.Code.Should().Be(code);
    }

    [Test]
    public void ToStringReturnsCode()
    {
        var colour = Color.White;

        colour.ToString().Should().Be(colour.Code);
    }

    [Test]
    public void ShouldPerformImplicitConversionToColourCodeString()
    {
        string code = Color.White;

        code.Should().Be("#FFFFFF");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedColourCode()
    {
        var colour = (Color)"#FFFFFF";

        colour.Should().Be(Color.White);
    }
}

```

### tests\Infrastructure.IntegrationTests\GlobalUsings.cs

```csharp
global using NUnit.Framework;
```

### tests\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <RootNamespace>YummyZoom.Infrastructure.IntegrationTests</RootNamespace>
        <AssemblyName>YummyZoom.Infrastructure.IntegrationTests</AssemblyName>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="NUnit" />
        <PackageReference Include="NUnit3TestAdapter" />
        <PackageReference Include="NUnit.Analyzers">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="coverlet.collector">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

</Project>

```

