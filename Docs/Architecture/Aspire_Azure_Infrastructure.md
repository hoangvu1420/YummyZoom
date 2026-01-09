# Aspire + Azure Infrastructure (Current Implementation)

This document describes how YummyZoom’s current infrastructure is defined and deployed using **.NET Aspire (AppHost)** + **Azure Developer CLI (azd)** + **Bicep** + **Azure Container Apps**.

## Scope and source of truth

Primary files that define the current infra setup:

- Aspire app graph (local + publish-time shape): `src/AppHost/Program.cs`
- azd project definition: `src/AppHost/azure.yaml`
- Bicep (provisioning): `src/AppHost/infra/main.bicep` (+ its modules)
- Container App template for the `web` service: `src/AppHost/infra/web.tmpl.yaml`
- CI/CD pipeline: `.github/workflows/azure-dev.yml`

Reference material (kept in-repo) that matches the same approach:

- `ref/infra/` and `ref/aspire-azure-deployment.md`
- Planning doc: `Docs/Future-Plans/Aspire-Azure-Deployment-Plan.md`

## Aspire host (AppHost) and runtime wiring

### App graph

AppHost is located at `src/AppHost` and is the orchestration entrypoint for both local dev and Azure deployment.

In `src/AppHost/Program.cs`, the graph is:

- `redis` (resource name: `redis`)
  - **Publish mode**: `AddConnectionString("redis")` (external Redis provided by infra)
  - **Local/dev mode**: `AddRedis("redis")` (local container)
- `postgres` (resource name: `postgres`) and database `YummyZoomDb`
  - **Publish mode**: `AddAzurePostgresFlexibleServer("postgres")` + `.AddDatabase("YummyZoomDb")` (managed Azure Database for PostgreSQL Flexible Server)
  - **Local/dev mode**: `AddPostgres("postgres")` as a container using `postgis/postgis:16-3.4`, plus:
    - `.WithPgAdmin()` (local PgAdmin)
    - `.WithEnvironment("POSTGRES_DB", "YummyZoomDb")` (default database auto-created)
- `web` project (resource name: `web`)
  - Added via `builder.AddProject<Projects.Web>("web")`
  - Marked external: `.WithExternalHttpEndpoints()`

Resource dependencies are explicit:

- `web` references and waits for the database.
- `web` references and waits for Redis in local/dev mode.

### Service defaults (health + OpenTelemetry)

The `src/ServiceDefaults` project provides cross-cutting “Aspire defaults” applied by `src/Web/Program.cs` via `builder.AddServiceDefaults()`:

- Health endpoints (development only): `/health` and `/alive`
- Service discovery + default HTTP resilience handlers
- OpenTelemetry metrics/traces with optional OTLP exporting when `OTEL_EXPORTER_OTLP_ENDPOINT` is set

### Application configuration expectations

The `web` service expects (at minimum):

- `ConnectionStrings:YummyZoomDb` (PostgreSQL connection string)
- `ConnectionStrings:redis` (Redis connection string)

Key Vault configuration is optional:

- If `AZURE_KEY_VAULT_ENDPOINT` is set, `src/Web/DependencyInjection.cs` attempts to connect to Key Vault and then adds it as a configuration source via `AddAzureKeyVault(...)`.

## Azure deployment model (azd)

### azd project

The azd project lives under `src/AppHost` (not repo root). The azd config is `src/AppHost/azure.yaml`:

- `services.web`
  - `language: dotnet`
  - `project: ./AppHost.csproj` (deploy driven by the Aspire host)
  - `host: containerapp` (Azure Container Apps)
  - `parameters`: `postgresUser`, `postgresPassword` marked `secure`

### azd environment state

azd writes local environment state under `src/AppHost/.azure/`.

Important:

- `src/AppHost/.azure/` is intended to be **uncommitted** (see `src/AppHost/.azure/.gitignore`).
- `.azure/<env>/.env` typically contains *both* computed outputs (resource IDs, endpoints) and *sensitive values* (e.g., DB passwords / connection strings). Treat it as a secret file.

### Bicep parameter binding

`src/AppHost/infra/main.parameters.json` binds Bicep parameters from azd environment variables:

- `environmentName` ← `AZURE_ENV_NAME`
- `location` ← `AZURE_LOCATION`
- `postgresUser` ← `AZURE_POSTGRES_USER` (secure)
- `postgresPassword` ← `AZURE_POSTGRES_PASSWORD` (secure)
- `principalId` ← `AZURE_PRINCIPAL_ID`

`AZURE_PRINCIPAL_ID` is used to grant role(s) to the deploying identity at provision time.

## Bicep infrastructure (provisioning)

All Bicep is under `src/AppHost/infra/`. The entrypoint is subscription-scoped: `src/AppHost/infra/main.bicep`.

### `main.bicep` (subscription scope)

`src/AppHost/infra/main.bicep`:

- Creates a resource group: `rg-${environmentName}` with tag `azd-env-name=${environmentName}`
- Deploys the following modules into that RG:
  - `resources.bicep` (shared platform resources)
  - `redis/redis-containerapp.module.bicep` (Redis container app in Azure Container Apps)
  - `postgres/postgres.module.bicep` + `postgres-roles/postgres-roles.module.bicep`
  - `keyvault/keyvault.module.bicep` + `keyvault/keyvault-secrets.module.bicep`

It also re-exports key outputs used by azd/templates:

- Managed identity IDs
- Container Apps environment IDs
- ACR endpoint/name
- Redis/Postgres connection strings
- Key Vault URI/name

### `resources.bicep` (shared resources)

`src/AppHost/infra/resources.bicep` provisions the shared “platform” for Container Apps:

1) **User-assigned managed identity**

- Type: `Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31`
- Name: `mi-${uniqueString(resourceGroup().id)}`

This identity is used for:

- Pulling images from ACR
- Accessing Key Vault secrets
- Being granted data-plane roles for Postgres (via the `postgres-roles` module)

2) **Azure Container Registry (ACR)**

- Type: `Microsoft.ContainerRegistry/registries@2023-07-01`
- SKU: `Basic`
- Name pattern: `acr${uniqueString(resourceGroup().id)}` (dashes removed to satisfy ACR naming)

Role assignment to allow the managed identity to pull:

- Type: `Microsoft.Authorization/roleAssignments@2022-04-01`
- Role: `AcrPull` (`7f951dda-4ed3-4680-a7ca-43fe172d538d`)
- Scope: the ACR registry

3) **Log Analytics workspace**

- Type: `Microsoft.OperationalInsights/workspaces@2022-10-01`
- SKU: `PerGB2018`
- Name: `law-${uniqueString(resourceGroup().id)}`

4) **Azure Container Apps managed environment**

- Type: `Microsoft.App/managedEnvironments@2025-02-02-preview`
- Name: `cae-${uniqueString(resourceGroup().id)}`
- Workload profile: `Consumption`
- Logs: configured to send app logs to the Log Analytics workspace

Aspire dashboard component inside the managed environment:

- Nested type: `dotNetComponents@2025-02-02-preview`
- `componentType: AspireDashboard`
- Name: `aspire-dashboard`

5) **Contributor role assignment for the deploying principal**

The template assigns Contributor on the Container Apps environment to `principalId` (so `AZURE_PRINCIPAL_ID` must be available during provisioning):

- Type: `Microsoft.Authorization/roleAssignments@2022-04-01`
- Role: `Contributor` (`b24988ac-6180-42a0-ab88-20f7382dd24c`)
- Scope: the Container Apps managed environment

### Redis (Container App)

#### `redis/redis-containerapp.module.bicep`

`src/AppHost/infra/redis/redis-containerapp.module.bicep` provisions Redis as a dedicated Container App in the same ACA environment:

- Type: `Microsoft.App/containerApps@2024-02-02-preview`
- Image: `docker.io/redis:7-alpine`
- Ingress: internal-only, TCP, port `6379`
- Scale: `minReplicas: 1`, `maxReplicas: 1`
- Tag: `aspire-resource-name=redis` (matches AppHost resource name)

Output:

- `connectionString`: `<containerAppName>:6379,abortConnect=false`

### PostgreSQL (Flexible Server) + extensions

#### `postgres/postgres.module.bicep`

`src/AppHost/infra/postgres/postgres.module.bicep` provisions PostgreSQL Flexible Server:

- Type: `Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01`
- Version: `16`
- SKU: `Standard_B1ms` (Burstable)
- Storage: `32 GB`
- Backup retention: `7 days`, geo-redundant backup disabled
- Availability: zone pinned to `1`, HA disabled
- Auth: AAD **enabled** and password auth **enabled**
- Firewall rule `AllowAllAzureIps` (`0.0.0.0` → `0.0.0.0`)
- Extensions enabled via `azure.extensions`:
  - `postgis`
  - `pg_trgm`
  - `unaccent`
- Database created: `YummyZoomDb`
- Tag: `aspire-resource-name=postgres` (matches AppHost resource name)

Output:

- `connectionString`: `Host=<fqdn>;Username=<postgresUser>;Password=<postgresPassword>;Database=YummyZoomDb;Ssl Mode=Require`

#### `postgres-roles/postgres-roles.module.bicep`

`src/AppHost/infra/postgres-roles/postgres-roles.module.bicep` configures an AAD administrator on the server:

- Type: `Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01`
- Uses:
  - `principalId` (managed identity principal id)
  - `principalName` (managed identity name)
  - `principalType` = `ServicePrincipal`

### Key Vault (secrets storage)

#### `keyvault/keyvault.module.bicep`

`src/AppHost/infra/keyvault/keyvault.module.bicep` provisions a Key Vault with RBAC:

- Type: `Microsoft.KeyVault/vaults@2022-07-01`
- `enableRbacAuthorization: true`
- `accessPolicies: []` (no legacy access policies)
- `enabledForTemplateDeployment: true`

Role assignment:

- Role: `Key Vault Secrets User` (`4633458b-17de-408a-b874-0445c86b69e6`)
- Assigned to: the managed identity principal ID

Outputs:

- `keyVaultUri` (vault URI, ends with `/`)
- `keyVaultName`

#### `keyvault/keyvault-secrets.module.bicep`

`src/AppHost/infra/keyvault/keyvault-secrets.module.bicep` writes two secrets:

- `YummyZoomDb-Conn` (Postgres connection string)
- `Redis-Conn` (Redis connection string)

These values are sourced from the module outputs of Postgres and the Redis container app.

## Container App definition for `web`

`src/AppHost/infra/web.tmpl.yaml` is the Container App template used by azd for the `web` service.

Key points:

- Identity:
  - `type: UserAssigned`
  - Uses the user-assigned managed identity created in `resources.bicep`
- Image pull:
  - Registry: `AZURE_CONTAINER_REGISTRY_ENDPOINT`
  - Uses the same managed identity for ACR authentication
- Ingress:
  - `external: true`
  - `activeRevisionsMode: single`
  - `allowInsecure: false`
- Key Vault-backed secrets (mounted as Container Apps secrets):
  - `yummyzoomdb-connection` → `.../secrets/YummyZoomDb-Conn`
  - `redis-connection` → `.../secrets/Redis-Conn`
- Runtime environment variables injected into the container:
  - `AZURE_CLIENT_ID` = managed identity client id (for `DefaultAzureCredential`)
  - `AZURE_KEY_VAULT_ENDPOINT` = Key Vault URI (enables Key Vault config provider)
  - `ConnectionStrings__YummyZoomDb` from secret `yummyzoomdb-connection`
  - `ConnectionStrings__redis` from secret `redis-connection`
  - Forwarded headers enabled (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`)
  - OTEL “experimental” flags enabled (no exporter endpoint is defined here)

Template tags for azd/Aspire mapping:

- `azd-service-name: web`
- `aspire-resource-name: web`

## GitHub Actions workflow (CI/CD)

The deployment workflow is `.github/workflows/azure-dev.yml`.

### Trigger

- Runs on `workflow_dispatch`
- Runs on `push` to `main`

### Auth model

Uses GitHub OIDC (federated credentials) via:

- `permissions: id-token: write`
- `azd auth login --federated-credential-provider github`

### What the workflow does

In order:

1) Checkout repository
2) Install `azd`
3) Install .NET SDK (`9.x`)
4) Authenticate to Azure with federated credentials
5) Provision infrastructure: `azd provision --no-prompt` (from `./src/AppHost`)
6) Deploy application: `azd deploy --no-prompt` (from `./src/AppHost`)

### Required GitHub repo configuration

Variables (`Settings → Secrets and variables → Actions → Variables`):

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_LOCATION`
- `AZURE_ENV_NAME`

Secrets (`Settings → Secrets and variables → Actions → Secrets`):

- `AZURE_POSTGRES_USER`
- `AZURE_POSTGRES_PASSWORD`

## Fresh provision checklist (manual, before first deploy)

When spinning up a **fresh** Azure environment (old resources deleted/clean) you typically run `azd provision` first, then complete this one-time checklist, then run `azd deploy`. These steps are intentionally **not automated** in the pipeline.

- [ ] **Grant yourself Key Vault admin access (so you can manage secrets)**
  - In Azure Portal → Key Vault → **Access control (IAM)** → add role assignment:
    - Role: **Key Vault Administrator**
    - Assignee: your owner account (human user)
  - Reason: the Bicep grants the app identity “Key Vault Secrets User” for runtime reads, but your user still needs admin rights to view/update secrets during bootstrap.
- [ ] **Apply the EF Core migrations to the new Postgres**
  - Apply `artifacts/migrations.sql` to the newly created `YummyZoomDb` database on the provisioned PostgreSQL Flexible Server.
  - Goal: bring the schema in Azure in sync with the required EF model before the service starts handling traffic.
- [ ] **Upload required app secrets into Key Vault**
  - Run `scripts/keyvault-add/import-usersecrets.ps1` against the new vault:
    - Example: `pwsh ./scripts/keyvault-add/import-usersecrets.ps1 -VaultName "<KEYVAULT_NAME>"`
  - This uses `az keyvault secret set` and reads `scripts/keyvault-add/usersecrets.json` by default (ensure the file contains the right values for the environment).
- [ ] **Update Stripe webhook destination URL to the new service URL**
  - In Stripe test dashboard, edit the existing webhook endpoint destination and update its **Endpoint URL** to the new Container App public URL:
    - `https://<your-web-containerapp-fqdn>/api/stripe-webhooks`
  - No need to recreate the destination; just update the URL so Stripe sends events to the new deployment.
- [ ] **Update backend URL in frontend configuration (if applicable)**
  - If you have a separate frontend deployment (e.g., static web app, SPA), ensure its configuration points to the new backend URL.
  - This may involve updating environment variables, config files, or redeploying the frontend with the new API base URL.

## Notable points and operational notes

1) **Secrets hygiene**

- Ensure `src/AppHost/.azure/` is not committed. It may contain sensitive `.env` values.
- The repo currently contains sensitive example material under `scripts/keyvault-add/`. Treat it as secret-bearing content and prefer:
  - GitHub Actions secrets for CI/CD
  - Key Vault secrets for runtime configuration
  - `dotnet user-secrets` for local development

2) **Network exposure**

- PostgreSQL is configured with `AllowAllAzureIps`. This is convenient for dev but broad for production; tighten it if needed (private networking, restricted rules, etc.).

3) **Preview API usage**

- The Container Apps environment and Aspire dashboard component use `@2025-02-02-preview` resource types in `src/AppHost/infra/resources.bicep`. This may require preview feature registration and could change over time.

4) **Extension requirements**

- Postgres enables `postgis`, `pg_trgm`, and `unaccent` explicitly. This is a core dependency of the current app (search/spatial use cases).

5) **App startup behavior**

- `src/Web/Program.cs` runs DB initialization in Development and seeds in non-Development environments. Ensure production seeding is intentional and idempotent for your deployment strategy.
