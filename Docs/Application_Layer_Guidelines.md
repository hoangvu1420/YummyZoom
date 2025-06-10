# Application Layer Guidelines

## Overview

The **Application layer** in the YummyZoom project is responsible for orchestrating business use cases, handling commands and queries, and acting as a mediator between the Domain layer (core business logic) and the outer layers (Infrastructure, Web). It follows Clean Architecture and Domain-Driven Design (DDD) principles, ensuring separation of concerns, testability, and maintainability.

---

## Components of the Application Layer

- **Commands & Command Handlers:**  
  Represent actions that change the state of the system (e.g., `RegisterUserCommand`, `CreateTodoList`). Each command has a handler that contains the use case logic and interacts with the Domain layer.

- **Queries & Query Handlers:**  
  Used for retrieving data (e.g., `GetTodos`). Queries do not modify state and have dedicated handlers.

- **DTOs (Data Transfer Objects):**  
  Simple objects for transferring data between layers (e.g., `RegisterUserResponse`, `TodoItemDto`).

- **Validators:**  
  Ensure that incoming requests meet business and data requirements before processing (e.g., `RegisterUserCommandValidator`).

- **Event Handlers:**  
  React to domain or application events, enabling decoupled side effects (e.g., `RoleAssignmentAddedToUserEventHandler`).

- **Common Directory:**  
  - **Interfaces:** Abstractions for infrastructure dependencies (e.g., `IUnitOfWork`, `IIdentityService`).
  - **Behaviours:** Pipeline behaviors for cross-cutting concerns (e.g., validation, logging, authorization, performance).
  - **Models:** Shared data structures (e.g., `PaginatedList`, `Result`).
  - **Mappings:** Mapping profiles/extensions for DTOs and domain objects.
  - **Exceptions:** Custom exceptions for application-specific errors.
  - **Security:** Security-related attributes and helpers.

---

## How to Work on a New Feature

1. **Model the Domain:**  
   Start by defining or updating entities, value objects, and aggregates in the Domain layer.

2. **Define Use Cases:**  
   - Create commands/queries in the Application layer to represent the feature's use cases.
   - Implement handlers for these commands/queries.
   - Add DTOs and validators as needed.

3. **Interface with Infrastructure:**  
   - Define interfaces in `Common/Interfaces` for any external dependencies.
   - Infrastructure implementations will be provided in the Infrastructure layer.

4. **Add Authorization Decorators (if needed):**  
   - If the feature requires authorization, decorate the command or query class with `[Authorize]` attributes to specify required roles or policies.  
   - For example, see `PurgeTodoListsCommand` in `src/Application/TodoLists/Commands/PurgeTodoLists/PurgeTodoLists.cs`:
     ```csharp
     [Authorize(Roles = Roles.Administrator)]
     [Authorize(Policy = Policies.CanPurge)]
     public record PurgeTodoListsCommand : IRequest<Result<Unit>>;
     ```
   - This ensures that only authorized users can execute the command or query.

5. **Register Services:**  
   - Ensure new handlers, validators, and mappings are registered in `DependencyInjection.cs` (usually via assembly scanning).

6. **Testing:**  
   - Write unit tests for handlers and validators.
   - Use the `tests/Application.UnitTests` and `tests/Application.FunctionalTests` directories.

---

## Interaction with Other Layers

- **Domain Layer:**  
  The Application layer invokes domain logic via entities, aggregates, and domain services. It should not contain business rules itself.

- **Infrastructure Layer:**  
  The Application layer defines interfaces for infrastructure concerns (e.g., repositories, services, etc.). The Infrastructure layer provides implementations.

- **Web Layer:**  
  The Web layer (API/controllers) sends commands and queries to the Application layer, which processes them and returns results.

---

## Best Practices

- **Keep Handlers Thin:**  
  Orchestrate domain logic and delegate to domain entities/services. Avoid business logic in handlers.

- **Use DTOs for Data Exchange:**  
  Never expose domain entities directly to outer layers.

- **Leverage Behaviours:**  
  Use pipeline behaviors for cross-cutting concerns (validation, logging, authorization, performance).

- **Validation First:**  
  Always validate requests before processing. Validation of simple format like required fields, length, etc. should be defined in validators and handled by FluentValidation. Business logic related validation should be handled by the domain layer.

- **Consistent Error Handling:**  
  Use custom exceptions and the `Result` pattern for predictable error management.

- **Follow Naming and Structure Conventions:**  
  Place files in appropriate directories (`Commands`, `Queries`, `EventHandlers`, etc.).

---

## Common Pitfalls

- **Leaking Domain Logic:**  
  Do not put business rules in handlers or DTOs—keep them in the Domain layer.

- **Direct Infrastructure Access:**  
  Only depend on abstractions (interfaces) in the Application layer, not concrete implementations.

- **Skipping Validation:**  
  Always use validators to enforce input correctness.

- **Ignoring Behaviours:**  
  Register and use pipeline behaviors for maintainability and consistency.

---

## Adding a New Feature: Example Steps

1. **Domain:**  
   Add or update entities/aggregates in `Domain/`.

2. **Application:**  
   - Add a new command/query in `Application/Feature/Commands` or `Application/Feature/Queries`.
   - Implement handler, validator, and DTOs.
   - Register any new interfaces in `Common/Interfaces`.

3. **Infrastructure:**  
   Implement required interfaces in the Infrastructure layer.

4. **Web:**  
   Expose the feature via an API endpoint.

5. **Testing:**  
   Write and run tests.

---

## Naming Conventions and Code Placement

When creating a new feature in the Application layer, follow these conventions for naming and organizing your code:

- **Directory Structure:**
  - Place each feature in its own directory under `src/Application/<FeatureName>/`.
  - Use subdirectories such as `Commands`, `Queries`, and `EventHandlers` to organize different types of operations.

- **File Naming:**
  - For commands: `<Action><Entity>Command.cs` (e.g., `CreateTodoListCommand.cs`).
  - For queries: `<Action><Entity>Query.cs` (e.g., `GetTodosQuery.cs`).
  - For handlers: `<Action><Entity>CommandHandler.cs` or `<Action><Entity>QueryHandler.cs`.
  - For validators: `<Action><Entity>CommandValidator.cs` or `<Action><Entity>QueryValidator.cs`.
  - For responses: `<Action><Entity>Response.cs` if a separate response object is needed.

- **Combining vs. Separating Files:**
  - If the request and response objects are small and closely related, you may place them in the same file as the handler (e.g., `CreateTodoList.cs` containing the command, response, and handler).
  - If the request or response objects are large or complex, create separate files for each (e.g., `CreateTodoListCommand.cs`, `CreateTodoListResponse.cs`, `CreateTodoListCommandHandler.cs`).
  - Validators should always be in their own file (e.g., `CreateTodoListCommandValidator.cs`).

- **DTOs for Web API:**
  - The request and response objects defined in the Application layer serve as Data Transfer Objects (DTOs) for the Web API layer. Do not expose domain entities directly to the Web layer.

- **Example Structure:**

  ```txt
  src/Application/TodoLists/Commands/CreateTodoList/
    ├── CreateTodoList.cs                # Command, response, and handler (if small)
    ├── CreateTodoListCommand.cs         # Command (if large)
    ├── CreateTodoListResponse.cs        # Response (if large)
    ├── CreateTodoListCommandHandler.cs  # Handler (if large)
    ├── CreateTodoListCommandValidator.cs# Validator
  ```

- **General Tips:**
  - Keep file and class names descriptive and consistent.
  - Group related files together for easier navigation and maintainability.

---

## Example: Register User Command Feature

Below is a typical structure for a feature in the Application layer, using the RegisterUser command as an example. This demonstrates how to organize the command, response, handler, and validator:

```csharp
// src/Application/Users/Commands/RegisterUser/RegisterUserCommand.cs
public record RegisterUserCommand(string Email, string Password, string Name) : IRequest<RegisterUserResponse>;

// src/Application/Users/Commands/RegisterUser/RegisterUserResponse.cs
public record RegisterUserResponse(Guid UserId);

// src/Application/Users/Commands/RegisterUser/RegisterUserCommandHandler.cs
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAggregateRepository _userRepository;

    public RegisterUserCommandHandler(
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IUserAggregateRepository userRepository)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // New implementation using the functional UoW pattern
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1) Create identity user + assign Customer role
            var idResult = await _identityService.CreateIdentityUserAsync(
                request.Email!, 
                request.Password!, 
                Roles.Customer);
            
            if (idResult.IsFailure) 
                return Result.Failure<Guid>(idResult.Error);

            // 2) Build domain User aggregate
            var roleResult = RoleAssignment.Create(Roles.Customer);
            if (roleResult.IsFailure) 
                return Result.Failure<Guid>(roleResult.Error);

            var userResult = User.Create(
                UserId.Create(idResult.Value),
                request.Name!, 
                request.Email!, 
                null,
                new List<RoleAssignment> { roleResult.Value });
                
            if (userResult.IsFailure) 
                return Result.Failure<Guid>(userResult.Error);

            // 3) Persist the domain user
            await _userRepository.AddAsync(userResult.Value, cancellationToken);

            return Result.Success(idResult.Value);
        }, cancellationToken);
    }
}

// src/Application/Users/Commands/RegisterUser/RegisterUserCommandValidator.cs
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
    }
}
```

- Place each file in the appropriate directory as shown above.
- The command and response serve as DTOs for the Web API layer.
- The handler contains orchestration logic and delegates business rules to the domain.
- The validator enforces input requirements using FluentValidation.

---

## References

- See `Docs/YummyZoom_Project_Documentation.md` for overall architecture.
- Use the `Common` directory for shared abstractions and behaviors.
- Refer to existing features (e.g., `TodoLists`, `Users`) for implementation patterns.

---

This document should serve as a starting point and reference for anyone working on the Application layer of YummyZoom. For further details, always consult the main project documentation and review existing code for patterns and conventions. 