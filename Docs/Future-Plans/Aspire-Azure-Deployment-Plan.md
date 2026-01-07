# Aspire + azd + Bicep plan for YummyZoom

## Goal
Initialize azd + Aspire infrastructure (Bicep + Container Apps) using the proven reference in `ref/aspire-azure-deployment.md` and `ref/infra/`, then adapt it to YummyZoom’s current app graph and config needs.

## Current app requirements (src/)
- AppHost (`src/AppHost`) defines:
  - PostgreSQL resource named `postgres` with database `YummyZoomDb` (PostGIS image locally).
  - Redis resource named `redis`.
  - Single app project `web`.
- App expects connection strings:
  - `ConnectionStrings:YummyZoomDb` (required by `Infrastructure` layer).
  - `ConnectionStrings:redis` (for caching, SignalR features, TeamCart real-time store).
- `Web` uses Key Vault provider only if `AZURE_KEY_VAULT_ENDPOINT` is set.

## Key differences vs reference project
- Reference uses two services (`apiservice`, `webfrontend`); YummyZoom uses one service (`web`).
- Secret mappings should target `YummyZoomDb` and `redis` connection string keys.
- YummyZoom relies on PostGIS extensions (Postgres 16 + `postgis`, `pg_trgm`, `unaccent`).

## Plan and steps

### 1) Generate the azd template from AppHost
Run azd from Windows (WSL note) so `azd` and `dotnet` are available:

```bat
cmd.exe /c "cd /d E:\source\repos\CA\YummyZoom\src\AppHost && azd init"
```

Suggested choices when prompted:
- Use existing app.
- Aspire template (or select “Aspire” when asked).
- Service name: `web` (or keep `app` if you prefer the default; just keep it consistent with the infra template tags).

This should generate `azure.yaml`, `infra/`, and `.azure/` under `src/AppHost`.

### 2) Replace/align the generated infra with the reference
Copy the structure from `ref/infra/` into `src/AppHost/infra/`, then update names and secrets to match YummyZoom:

- `main.bicep`
  - Keep subscription-scope deployment and RG creation pattern from the reference.
  - Keep `postgresUser` and `postgresPassword` secure parameters.
- `postgres.module.bicep`
  - Ensure Postgres version is 16 and that extensions `postgis`, `pg_trgm`, `unaccent` are allowed.
  - Keep firewall rule `AllowAllAzureIps` if you want Azure-hosted access.
- `cache.module.bicep`
  - Redis Basic (C1) is OK for dev; adjust SKU if needed.
- `keyvault-secrets.module.bicep`
  - Write secrets named `YummyZoomDb-Conn` and `Redis-Conn`.

### 3) Update Container App template for the single web service
Create a single template (for example `web.tmpl.yaml`) based on `ref/infra/webfrontend.tmpl.yaml` and update env vars:

- Key Vault-backed secrets:
  - `YummyZoomDb-Conn` → `ConnectionStrings__YummyZoomDb`
  - `Redis-Conn` → `ConnectionStrings__redis`
- Add `AZURE_KEY_VAULT_ENDPOINT={{ .Env.KEYVAULT_URI }}` so `AddKeyVaultIfConfigured` can load additional secrets.
- Keep `AZURE_CLIENT_ID` and ACR integration as in the reference.
- Remove `services__apiservice__*` env vars since there is no API service.

Make sure the template tag matches the `azure.yaml` service name:
- `azd-service-name: web`
- `aspire-resource-name: web`

### 4) Update `azure.yaml`
Replace the existing root `azure.yaml` (appservice) with an Aspire-based one under `src/AppHost`:

- `services.web` (or `services.app`) should point to `./AppHost.csproj`.
- `host: containerapp`.
- Secure parameters: `postgresUser`, `postgresPassword`.

Keep the service name aligned with the template tags and file name (e.g., `web.tmpl.yaml`).

### 5) Wire up GitHub Actions (optional now, required for CI/CD)
Generate the workflow from Windows:

```bat
cmd.exe /c "cd /d E:\source\repos\CA\YummyZoom\src\AppHost && azd pipeline config"
```

Set GitHub repo variables and secrets:
- Vars: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_ENV_NAME`, `AZURE_LOCATION`
- Secrets: `AZURE_POSTGRES_USER`, `AZURE_POSTGRES_PASSWORD`

### 6) Provision and deploy
```bat
cmd.exe /c "cd /d E:\source\repos\CA\YummyZoom\src\AppHost && azd auth login"
cmd.exe /c "cd /d E:\source\repos\CA\YummyZoom\src\AppHost && azd up"
```

Use `azd provision` for infra-only and `azd deploy` for app updates.

## Additional notes
- Ensure the Postgres Flexible Server SKU supports required extensions. EF migrations will attempt to create `postgis`, `pg_trgm`, and `unaccent`.
- If you want the app to read secrets directly from Key Vault (beyond the connection strings), store them in the same vault and keep `AZURE_KEY_VAULT_ENDPOINT` set.
- Subscription-scope deployments require permissions to create resource groups.
- The root-level `azure.yaml` should be removed or replaced to avoid conflicting azd configs; keep only the AppHost-based template.

## References
- Aspire + azd deployment: https://learn.microsoft.com/dotnet/aspire/deployment/azure/azure-dev-cli
- Azure Developer CLI overview: https://learn.microsoft.com/azure/developer/azure-developer-cli/overview
- azd pipeline config: https://learn.microsoft.com/azure/developer/azure-developer-cli/azd-pipeline-config
- Bicep overview: https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview
- Azure Container Apps: https://learn.microsoft.com/azure/container-apps/overview
- Azure Database for PostgreSQL Flexible Server: https://learn.microsoft.com/azure/postgresql/flexible-server/overview
