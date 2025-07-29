fail: Microsoft.EntityFrameworkCore.Database.Command[20102]
      Failed executing DbCommand (27ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT "MigrationId", "ProductVersion"
      FROM "__EFMigrationsHistory"
      ORDER BY "MigrationId";
fail: Microsoft.EntityFrameworkCore.Database.Command[20102]
      Failed executing DbCommand (4ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE "MenuItems" (
          "Id" uuid NOT NULL,
          "RestaurantId" uuid NOT NULL,
          "MenuCategoryId" uuid NOT NULL,
          "Name" character varying(200) NOT NULL,
          "Description" character varying(1000) NOT NULL,
          "BasePrice_Amount" numeric(18,2) NOT NULL,
          "BasePrice_Currency" character varying(3) NOT NULL,
          "ImageUrl" character varying(500),
          "IsAvailable" boolean NOT NULL DEFAULT TRUE,
          "Created" timestamp with time zone NOT NULL,
          "CreatedBy" character varying(255),
          "LastModified" timestamp with time zone NOT NULL,
          "LastModifiedBy" character varying(255),
          "IsDeleted" boolean NOT NULL DEFAULT FALSE,
          "DeletedOn" timestamp with time zone,
          "DeletedBy" character varying(255),
          "DietaryTagIds" nvarchar(max) NOT NULL,
          "AppliedCustomizations" jsonb NOT NULL,
          CONSTRAINT "PK_MenuItems" PRIMARY KEY ("Id")
      );
      COMMENT ON COLUMN "MenuItems"."Created" IS 'Timestamp when the entity was created';
      COMMENT ON COLUMN "MenuItems"."CreatedBy" IS 'Identifier of who created the entity';
      COMMENT ON COLUMN "MenuItems"."LastModified" IS 'Timestamp when the entity was last modified';
      COMMENT ON COLUMN "MenuItems"."LastModifiedBy" IS 'Identifier of who last modified the entity';
      COMMENT ON COLUMN "MenuItems"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
      COMMENT ON COLUMN "MenuItems"."DeletedOn" IS 'Timestamp when the entity was deleted';
      COMMENT ON COLUMN "MenuItems"."DeletedBy" IS 'Identifier of who deleted the entity';
fail: YummyZoom.Infrastructure.Data.ApplicationDbContextInitialiser[0]
      An error occurred while initialising the database.
      Npgsql.PostgresException (0x80004005): 42704: type "nvarchar" does not exist
      
      POSITION: 764
         at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
         at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteNonQuery(Boolean async, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteNonQueryAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteNonQueryAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteNonQueryAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Migrations.Internal.MigrationCommandExecutor.ExecuteAsync(IReadOnlyList`1 migrationCommands, IRelationalConnection connection, MigrationExecutionState executionState, Boolean beginTransaction, Boolean commitTransaction, Nullable`1 isolationLevel, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Migrations.Internal.MigrationCommandExecutor.ExecuteAsync(IReadOnlyList`1 migrationCommands, IRelationalConnection connection, MigrationExecutionState executionState, Boolean beginTransaction, Boolean commitTransaction, Nullable`1 isolationLevel, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Migrations.Internal.MigrationCommandExecutor.ExecuteNonQueryAsync(IReadOnlyList`1 migrationCommands, IRelationalConnection connection, MigrationExecutionState executionState, Boolean commitTransaction, Nullable`1 isolationLevel, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Migrations.Internal.Migrator.MigrateImplementationAsync(DbContext context, String targetMigration, MigrationExecutionState state, Boolean useTransaction, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Migrations.Internal.Migrator.MigrateImplementationAsync(DbContext context, String targetMigration, MigrationExecutionState state, Boolean useTransaction, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Migrations.Internal.Migrator.<>c.<<MigrateAsync>b__22_1>d.MoveNext()
      --- End of stack trace from previous location ---
         at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.<>c__DisplayClass30_0`2.<<ExecuteAsync>b__0>d.MoveNext()
      --- End of stack trace from previous location ---
         at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteImplementationAsync[TState,TResult](Func`4 operation, Func`4 verifySucceeded, TState state, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Migrations.Internal.Migrator.MigrateAsync(String targetMigration, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.Migrations.Internal.Migrator.MigrateAsync(String targetMigration, CancellationToken cancellationToken)
         at Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal.NpgsqlMigrator.MigrateAsync(String targetMigration, CancellationToken cancellationToken)
         at YummyZoom.Infrastructure.Data.ApplicationDbContextInitialiser.InitialiseAsync() in E:\source\repos\CA\YummyZoom\src\Infrastructure\Data\ApplicationDbContextInitialiser.cs:line 69
        Exception data:
          Severity: ERROR
          SqlState: 42704
          MessageText: type "nvarchar" does not exist
          Position: 764
          File: parse_type.c
          Line: 270
          Routine: typenameType

We have error while running the migration to the database.
Check the configuration of the models. Analyze the issue and propose a solution.

## Analysis

The database migration is failing because of a PostgreSQL compatibility issue. The error occurs at line 17 in the CREATE TABLE statement:

```sql
"DietaryTagIds" nvarchar(max) NOT NULL,
```

The problem is that `nvarchar(max)` is a SQL Server-specific data type that doesn't exist in PostgreSQL. This type mapping is being generated by Entity Framework Core based on the configuration in `MenuItemConfiguration.cs`.

### Root Cause

In the file `src/Infrastructure/Data/Configurations/MenuItemConfiguration.cs` at line 77, the `DietaryTagIds` property is configured with:

```csharp
builder.Property(m => m.DietaryTagIds)
    .HasConversion(
        tagIds => JsonSerializer.Serialize(tagIds.Select(id => id.Value).ToList(), (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!
            .Select(TagId.Create).ToList(),
        new ValueComparer<IReadOnlyList<TagId>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()))
    .HasColumnType("nvarchar(max)");
```

The `.HasColumnType("nvarchar(max)")` is the culprit - this is SQL Server syntax, not PostgreSQL.

### Inconsistency in Configuration

Interestingly, the same file shows that `AppliedCustomizations` is correctly configured for PostgreSQL using `jsonb`:

```csharp
builder.Property(m => m.AppliedCustomizations)
    .HasColumnType("jsonb")
```

This suggests the developer was aware of PostgreSQL requirements but missed updating the `DietaryTagIds` configuration.

## Solution

The fix requires changing the column type from `nvarchar(max)` to `jsonb` to match PostgreSQL syntax and maintain consistency with the `AppliedCustomizations` configuration.

### Required Changes

1. **Update MenuItemConfiguration.cs**: Change line 77 from:
   ```csharp
   .HasColumnType("nvarchar(max)");
   ```
   to:
   ```csharp
   .HasColumnType("jsonb");
   ```

2. **Remove existing migration**: Delete the current migration file that contains the invalid SQL.

3. **Generate new migration**: Create a fresh migration with the corrected configuration.

4. **Update model snapshot**: The ApplicationDbContextModelSnapshot.cs will also need to be regenerated to reflect the correct column type.

### PostgreSQL Data Type Mapping

- SQL Server: `nvarchar(max)` → PostgreSQL: `jsonb` (for JSON data)
- SQL Server: `varchar(max)` → PostgreSQL: `text`
- SQL Server: `nvarchar(n)` → PostgreSQL: `character varying(n)`

Since `DietaryTagIds` stores serialized JSON data (list of GUIDs), `jsonb` is the appropriate PostgreSQL type, which also provides better performance for JSON operations compared to `json`.

### Steps to Implement

1. Fix the configuration in `MenuItemConfiguration.cs`
2. Delete the problematic migration file
3. Run `dotnet ef migrations add InitialMigration` to generate a new migration
4. Run `dotnet ef database update` to apply the migration

This will resolve the PostgreSQL compatibility issue and allow the application to start successfully.