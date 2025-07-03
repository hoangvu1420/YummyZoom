# Application Layer Guidelines

## CQRS Pattern

The Application layer uses **Command Query Responsibility Segregation (CQRS)** to optimize for different data access patterns:

### 📝 Commands (Write Operations)

**Use Repositories + Aggregates**

- Enforce business rules and domain invariants
- Work with full aggregates for consistency
- Wrap in transactions using `IUnitOfWork`

```csharp
public class CreateSomethingCommandHandler : IRequestHandler<CreateSomethingCommand, Result<CreateSomethingResponse>>
{
    private readonly ISomethingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<CreateSomethingResponse>> Handle(CreateSomethingCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var result = SomethingAggregate.Create(request.Data);
            await _repository.AddAsync(result.Value, cancellationToken);
            return Result.Success(new CreateSomethingResponse(result.Value.Id.Value));
        }, cancellationToken);
    }
}
```

### 📖 Queries (Read Operations)

**Use Dapper + Direct SQL**

- Optimize performance with targeted data fetching
- Support complex joins across aggregates
- Map directly to DTOs

```csharp
public class GetSomethingQueryHandler : IRequestHandler<GetSomethingQuery, Result<GetSomethingResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public async Task<Result<GetSomethingResponse>> Handle(GetSomethingQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        
        const string sql = """SELECT * FROM "TableName" WHERE "Id" = @Id""";
        var data = await connection.QueryAsync<SomethingDto>(
            new CommandDefinition(sql, new { request.Id }, cancellationToken: cancellationToken));
            
        return Result.Success(new GetSomethingResponse(data.AsList()));
    }
}
```

## Application Layer Components

- **Commands & Handlers:** State-changing operations using repositories and aggregates
- **Queries & Handlers:** Data retrieval operations using direct SQL and Dapper  
- **DTOs:** Data transfer objects for communication between layers
- **Validators:** Input validation using FluentValidation
- **Event Handlers:** React to domain events for decoupled side effects
- **Common Directory:**
  - **Interfaces:** Abstractions for infrastructure dependencies
  - **Behaviours:** Cross-cutting concerns (validation, logging, authorization)
  - **Models:** Shared data structures (`PaginatedList`, `Result`)

## How to Implement a New Feature

### 1. Model the Domain

Define or update entities, value objects, and aggregates in the Domain layer.

### 2. Implement Use Cases

**For Commands (Write Operations):**

```csharp
// Command
[Authorize(Roles = Roles.Administrator)]
[Authorize(Policy = Policies.MustBeSomething)]
public record CreateSomethingCommand(string Name) : IRequest<Result<CreateSomethingResponse>>;

// Handler - Use Repository Pattern
public class CreateSomethingCommandHandler : IRequestHandler<CreateSomethingCommand, Result<CreateSomethingResponse>>
{
    private readonly ISomethingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    // Use repositories and work with full aggregates
}

// Validator
public class CreateSomethingCommandValidator : AbstractValidator<CreateSomethingCommand>
{
    public CreateSomethingCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
    }
}
```

**For Queries (Read Operations):**

```csharp
// Query
public record GetSomethingQuery(Guid Id) : IRequest<Result<GetSomethingResponse>>;

// Handler - Use Dapper Pattern
public class GetSomethingQueryHandler : IRequestHandler<GetSomethingQuery, Result<GetSomethingResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    // Use direct SQL with Dapper for optimal performance
}
```

### 3. Define Interfaces

Add any new infrastructure interfaces in `Common/Interfaces`.

### 4. Register Services

Services are automatically registered via assembly scanning in `DependencyInjection.cs`.

### 5. Testing

Write tests in `tests/Application.FunctionalTests`.

## File Organization

### Directory Structure

```
src/Application/<FeatureName>/
├── Commands/
│   └── <ActionName>/
│       ├── <Action>Command.cs
│       ├── <Action>CommandHandler.cs
│       └── <Action>CommandValidator.cs
└── Queries/
    └── <ActionName>/
        ├── <Action>Query.cs
        └── <Action>QueryHandler.cs
```

### Naming Conventions

- **Commands:** `<Action><Entity>Command.cs` (e.g., `CreateRoleAssignmentCommand.cs`)
- **Queries:** `<Action><Entity>Query.cs` (e.g., `GetUserRoleAssignmentsQuery.cs`)
- **Handlers:** `<Action><Entity>CommandHandler.cs` or `<Action><Entity>QueryHandler.cs`
- **Validators:** `<Action><Entity>CommandValidator.cs`
- **Responses:** `<Action><Entity>Response.cs` or include in main file if small

## Best Practices

### General

- **Keep Handlers Thin:** Orchestrate, don't implement business logic
- **Use DTOs:** Never expose domain entities to outer layers
- **Leverage Behaviours:** Use pipeline behaviors for cross-cutting concerns
- **Consistent Error Handling:** Use `Result` pattern and custom exceptions

### Commands

- Always use repositories and work with full aggregates
- Wrap in transactions using `IUnitOfWork.ExecuteInTransactionAsync`
- Validate business rules in domain entities, not handlers
- Use `[Authorize]` attributes for access control with roles or policies

### Queries

- Use `IDbConnectionFactory` and Dapper for direct SQL
- Create optimized queries for specific read scenarios
- Use JOINs for complex data from multiple aggregates
- Include cancellation token support with `CommandDefinition`

### Validation

- Use FluentValidation for input validation
- Validate format, length, required fields in validators
- Handle business logic validation in domain layer

## Common Pitfalls

- **Wrong Pattern Choice:** Using repositories for queries or Dapper for commands
- **Leaking Domain Logic:** Putting business rules in handlers instead of domain
- **Direct Infrastructure Access:** Depending on implementations instead of interfaces
- **Skipping Validation:** Not using validators for input validation
- **Ignoring Authorization:** Forgetting `[Authorize]` attributes on sensitive operations

## Migration Guide

For detailed steps to migrate existing query handlers to the new Dapper pattern, see:
📖 **[Query Handler Dapper Migration Guide](Query_Handler_Dapper_Migration_Guide.md)**

This guide includes:

- Step-by-step refactoring process
- SQL guidelines for PostgreSQL
- Performance benefits and examples
- Migration checklist

## Layer Interactions

- **Domain Layer:** Application invokes domain logic via entities and aggregates
- **Infrastructure Layer:** Application defines interfaces, Infrastructure provides implementations  
- **Web Layer:** Sends commands/queries to Application, receives DTOs back

---

## Example: Complete Feature Implementation

```csharp
// Command (Write Operation)
[Authorize(Roles = Roles.Administrator)]
public record CreateRoleAssignmentCommand(Guid UserId, Guid RestaurantId, RestaurantRole Role) : IRequest<Result<CreateRoleAssignmentResponse>>;

public record CreateRoleAssignmentResponse(Guid RoleAssignmentId);

public class CreateRoleAssignmentCommandHandler : IRequestHandler<CreateRoleAssignmentCommand, Result<CreateRoleAssignmentResponse>>
{
    private readonly IRoleAssignmentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    
    // Implementation uses repository pattern with full aggregates
}

// Query (Read Operation)  
public record GetRestaurantRoleAssignmentsQuery(Guid RestaurantId) : IRequest<Result<GetRestaurantRoleAssignmentsResponse>>;

public record GetRestaurantRoleAssignmentsResponse(List<RoleAssignmentDto> RoleAssignments);

public class GetRestaurantRoleAssignmentsQueryHandler : IRequestHandler<GetRestaurantRoleAssignmentsQuery, Result<GetRestaurantRoleAssignmentsResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    
    // Implementation uses Dapper with direct SQL for optimal performance
}
```

This approach ensures optimal performance, maintainability, and true CQRS separation while preserving domain integrity.

## References

- **[Query Handler Dapper Migration Guide](Query_Handler_Dapper_Migration_Guide.md)** - Detailed migration steps for existing query handlers
- **[YummyZoom Project Documentation](../YummyZoom_Project_Documentation.md)** - Overall architecture overview
- **Existing Features** - Reference `TodoLists`, `Users`, `RoleAssignments` for implementation patterns

---

*This document serves as the primary guide for Application layer development. Always consult existing code for patterns and the migration guide for query handler updates.*
