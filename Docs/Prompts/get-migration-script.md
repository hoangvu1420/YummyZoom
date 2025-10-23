
---

```
printenv POSTGRES_DB POSTGRES_USER
YummyZoomDb
postgres

PGPASSWORD='+7yMQ}RY)r69REx9{eN1_q' pg_dump -U postgres -d YummyZoomDb --schema-only --no-owner --no-privileges -f /tmp/schema.sql

PGPASSWORD='nG0gg5P(7mH88)nyqQb6RV' pg_dump -U postgres -d YummyZoomDb --data-only --no-owner --no-privileges -f /tmp/data.sql

docker cp postgres-jcrqsgth:/tmp/. E:/source/repos/CA/YummyZoom/db_scripts/
```

Note: PGPASSWORD might be different between runs.