
---

```
printenv POSTGRES_DB POSTGRES_USER
YummyZoomDb
postgres

PGPASSWORD='Ze1X}pRT~hurUxAXzWT{Hm' pg_dump -U postgres -d YummyZoomDb --schema-only --no-owner --no-privileges -f /tmp/schema.sql

PGPASSWORD='nG0gg5P(7mH88)nyqQb6RV' pg_dump -U postgres -d YummyZoomDb --data-only --no-owner --no-privileges -f /tmp/data.sql

docker cp postgres-jcrqsgth:/tmp/. E:/source/repos/CA/YummyZoom/db_scripts/
```

Note: PGPASSWORD might be different between runs.

```
dotnet user-secrets list --project .\src\Web\Web.csproj

dotnet user-secrets set "Stripe:WebhookSecret" "whsec_880350f8ed42a0728afac33ddb8242c5e4ab5e4b634fb5ed954430f8dd52998" --project .\src\Web\Web.csproj
```

