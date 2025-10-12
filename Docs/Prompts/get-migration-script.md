
---

```
printenv POSTGRES_DB POSTGRES_USER
YummyZoomDb
postgres

PGPASSWORD='sjzFTM9hxgSfWF9}K2_0*-' pg_dump -U postgres -d YummyZoomDb --schema-only --no-owner --no-privileges -f /tmp/schema.sql

PGPASSWORD='sjzFTM9hxgSfWF9}K2_0*-' pg_dump -U postgres -d YummyZoomDb --data-only --no-owner --no-privileges -f /tmp/data.sql

docker cp postgres-jtbqrdkj:/tmp/. E:/source/repos/CA/YummyZoom/db_scripts/
```

Note: PGPASSWORD might be different between runs.