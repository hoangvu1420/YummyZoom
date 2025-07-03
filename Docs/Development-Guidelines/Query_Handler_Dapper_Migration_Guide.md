# CQRS Pattern Implementation Guidelines

## Overview

This document establishes the conventions for implementing Command Query Responsibility Segregation (CQRS) in the YummyZoom application. The goal is to optimize read and write operations by using different approaches for commands and queries.

## Pattern Conventions

### Commands (Write Operations)

**Use Repository Pattern with Full Aggregates**

Commands should use repositories to work with full domain aggregates to ensure:

- Business rules and invariants are enforced
- Domain events are properly raised
- Aggregate consistency is maintained
- Complex business logic is encapsulated

**Example:**

```csharp
public class CreateRoleAssignmentCommandHandler : IRequestHandler<CreateRoleAssignmentCommand, Result<CreateRoleAssignmentResponse>>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Use repository for aggregate operations
    public async Task<Result<CreateRoleAssignmentResponse>> Handle(CreateRoleAssignmentCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Work with full aggregate
            var roleAssignmentResult = RoleAssignment.Create(userId, restaurantId, request.Role);
            await _roleAssignmentRepository.AddAsync(roleAssignmentResult.Value, cancellationToken);
            return Result.Success(new CreateRoleAssignmentResponse(roleAssignmentResult.Value.Id.Value));
        }, cancellationToken);
    }
}
```

### Queries (Read Operations)

**Use IDbConnectionFactory with Dapper for Direct SQL**

Queries should use `IDbConnectionFactory` and Dapper to fetch DTOs directly for:

- Optimal performance (no aggregate loading)
- Flexible data shaping
- Direct database-to-DTO mapping
- Support for complex joins across aggregates

**Example:**

```csharp
public class GetRestaurantRoleAssignmentsQueryHandler : IRequestHandler<GetRestaurantRoleAssignmentsQuery, Result<GetRestaurantRoleAssignmentsResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public async Task<Result<GetRestaurantRoleAssignmentsResponse>> Handle(GetRestaurantRoleAssignmentsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                ra.""Id"",
                ra.""UserId"",
                ra.""RestaurantId"",
                ra.""Role"",
                ra.""CreatedAt"",
                ra.""UpdatedAt""
            FROM ""RoleAssignments"" AS ra
            WHERE ra.""RestaurantId"" = @RestaurantId";

        var roleAssignments = await connection.QueryAsync<RoleAssignmentDto>(
            new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        return Result.Success(new GetRestaurantRoleAssignmentsResponse(roleAssignments.AsList()));
    }
}
```

## Refactoring Guide

### Step-by-Step Process to Migrate a Query Handler

#### 1. Add Required Dependencies

**In the Application project:**

```xml
<PackageReference Include="Dapper" />
```

**In the Infrastructure project:**

```xml
<PackageReference Include="Dapper" />
```

#### 2. Update Using Statements

Replace repository-related imports with Dapper and connection factory imports:

**Before:**

```csharp
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
```

**After:**

```csharp
using Dapper;
using YummyZoom.Application.Common.Interfaces;
```

#### 3. Change Constructor Dependencies

**Before:**

```csharp
private readonly IRoleAssignmentRepository _roleAssignmentRepository;

public GetRestaurantRoleAssignmentsQueryHandler(IRoleAssignmentRepository roleAssignmentRepository)
{
    _roleAssignmentRepository = roleAssignmentRepository;
}
```

**After:**

```csharp
private readonly IDbConnectionFactory _dbConnectionFactory;

public GetRestaurantRoleAssignmentsQueryHandler(IDbConnectionFactory dbConnectionFactory)
{
    _dbConnectionFactory = dbConnectionFactory;
}
```

#### 4. Rewrite the Handle Method

**Before (Repository + LINQ):**

```csharp
public async Task<Result<GetRestaurantRoleAssignmentsResponse>> Handle(GetRestaurantRoleAssignmentsQuery request, CancellationToken cancellationToken)
{
    var restaurantId = RestaurantId.Create(request.RestaurantId);
    var roleAssignments = await _roleAssignmentRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);

    var roleAssignmentDtos = roleAssignments.Select(ra => new RoleAssignmentDto(
        ra.Id.Value,
        ra.UserId.Value,
        ra.RestaurantId.Value,
        ra.Role,
        ra.CreatedAt,
        ra.UpdatedAt)).ToList();

    return Result.Success(new GetRestaurantRoleAssignmentsResponse(roleAssignmentDtos));
}
```

**After (Dapper + SQL):**

```csharp
public async Task<Result<GetRestaurantRoleAssignmentsResponse>> Handle(GetRestaurantRoleAssignmentsQuery request, CancellationToken cancellationToken)
{
    using var connection = _dbConnectionFactory.CreateConnection();

    const string sql = @"
        SELECT
            ra.""Id"",
            ra.""UserId"",
            ra.""RestaurantId"",
            ra.""Role"",
            ra.""CreatedAt"",
            ra.""UpdatedAt""
        FROM ""RoleAssignments"" AS ra
        WHERE ra.""RestaurantId"" = @RestaurantId";

    var roleAssignments = await connection.QueryAsync<RoleAssignmentDto>(
        new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

    return Result.Success(new GetRestaurantRoleAssignmentsResponse(roleAssignments.AsList()));
}
```

## SQL Guidelines for PostgreSQL

### Column Naming

- Use double quotes for column identifiers: `"Id"`, `"UserId"`, `"CreatedAt"`
- Match the exact casing used in your EF Core configuration

### Parameter Binding

- Use anonymous objects for parameters: `new { request.RestaurantId }`
- Use `CommandDefinition` for cancellation token support

### Complex Queries

For queries that need data from multiple aggregates, use JOINs:

```sql
SELECT 
    ra."Id",
    ra."UserId",
    u."Email" as UserEmail,
    u."FirstName" as UserFirstName, 
    u."LastName" as UserLastName,
    ra."RestaurantId",
    ra."Role",
    ra."CreatedAt",
    ra."UpdatedAt"
FROM "RoleAssignments" AS ra
INNER JOIN "AspNetUsers" AS u ON ra."UserId"::text = u."Id"
WHERE ra."RestaurantId" = @RestaurantId
ORDER BY ra."CreatedAt" DESC
```

**Example Implementation:**
See `GetRestaurantRoleAssignmentsWithUserInfoQueryHandler` for a complete example that demonstrates joining RoleAssignments with User data to create rich DTOs that would be difficult to achieve efficiently with repository patterns.

## Benefits of This Approach

### Performance Benefits

- **No Aggregate Loading**: Only fetch exactly the data needed
- **Direct Mapping**: Database to DTO without intermediate objects
- **Optimized Queries**: Hand-crafted SQL for specific use cases
- **Reduced Memory Usage**: No tracking, no change detection

### Maintainability Benefits

- **Clear Separation**: Commands handle business logic, queries handle data retrieval
- **Explicit Queries**: SQL is visible and can be optimized
- **Flexible Data Shaping**: Easy to create DTOs with data from multiple sources
- **Independent Evolution**: Read and write sides can evolve separately

### CQRS Compliance

- **True Separation**: Different models for reads and writes
- **Scalability**: Read and write sides can be scaled independently
- **Event Sourcing Ready**: Commands work with aggregates and events
- **Reporting Friendly**: Queries can be optimized for reporting needs

## Testing Considerations

### Functional Tests

- Ensure existing functional tests continue to pass
- Query tests should verify the correct data is returned
- Command tests should verify business rules and side effects

### Performance Testing

- Measure query performance before and after migration
- Verify memory usage improvements
- Test with larger datasets

## Migration Checklist

For each query handler to be migrated:

- [ ] Add Dapper package references
- [ ] Update using statements
- [ ] Change constructor dependency from repository to `IDbConnectionFactory`
- [ ] Rewrite Handle method to use SQL + Dapper
- [ ] Update column names for PostgreSQL compatibility
- [ ] Add `CommandDefinition` with cancellation token
- [ ] Run existing functional tests
- [ ] Verify performance improvements
- [ ] Update any related documentation

## Commands That Should NOT Change

The following command handlers should continue using repositories:

- `CreateRoleAssignmentCommandHandler`
- `UpdateRoleAssignmentCommandHandler`
- `DeleteRoleAssignmentCommandHandler`
- All other command handlers that modify aggregates

These handlers work with business logic and need the full aggregate for proper domain rule enforcement.
