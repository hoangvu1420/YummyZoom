
---

```
printenv POSTGRES_DB POSTGRES_USER
YummyZoomDb
postgres

PGPASSWORD='38YqkyTdkARnKA3hk~eg!s' pg_dump -U postgres -d YummyZoomDb --schema-only --no-owner --no-privileges -f /tmp/schema.sql
```

Note: PGPASSWORD might be different between runs.