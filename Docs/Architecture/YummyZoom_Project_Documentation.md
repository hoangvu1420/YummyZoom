# YummyZoom Project Documentation

## Project Structure

The YummyZoom project follows a Clean Architecture with Domain-Driven Design (DDD) principles, organized into several layers to promote separation of concerns, testability, and maintainability.

```txt
YummyZoom/
├── src/
│   ├── AppHost/             - Service hosting and configuration
│   ├── Application/         - Application-specific business logic, commands, queries, DTOs, validators
│   │   ├── Abstractions/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── DTOs/
│   │   ├── Validators/
│   │   └── Behaviors/       - Pipeline behaviors (logging, validation, transactions)
│   ├── Domain/              - Core business logic, entities, value objects, aggregates, domain events, errors
│   │   ├── Aggregates/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Events/
│   │   ├── Errors/
│   │   ├── Services/
│   │   └── Specifications/
│   ├── Infrastructure/      - Implementation details for infrastructure concerns (data access, identity, etc.)
│   │   ├── Persistence/
│   │   │   ├── EfCore/      - Entity Framework Core implementation
│   │   │   │   ├── Configurations/
│   │   │   │   ├── Migrations/
│   │   │   │   └── Interceptors/
│   │   │   └── Dapper/
│   │   ├── Identity/
│   │   ├── ReadModels/
│   │   ├── Repositories/
│   │   └── Services/
│   ├── ServiceDefaults/     - Default service configurations using .NET Aspire
│   ├── SharedKernel/        - Common components used across layers (Result pattern, etc.)
│   └── Web/                 - API endpoints and presentation logic
│       ├── Endpoints/
│       ├── Contracts/
│       ├── Filters/
│       ├── Middlewares/
│       └── Configuration/
└── tests/
    ├── Application.FunctionalTests/
    ├── Application.UnitTests/
    ├── Domain.UnitTests/
    ├── Web.ApiContractTests/
    └── Infrastructure.IntegrationTests/
```

## Layer Responsibilities

- **Domain:** This is the heart of the application. It contains the core business logic, including entities (objects with identity), value objects (objects that represent descriptive aspects of the domain with no conceptual identity), aggregate roots (entities that are the root of an aggregate, ensuring consistency within the aggregate), domain events (objects that represent something that happened in the domain), and domain errors. The Domain layer is independent of all other layers.

- **Application:** This layer contains the application services that orchestrate the domain layer to perform specific use cases. It includes command handlers (for executing actions that change the state of the application), query handlers (for retrieving data), Data Transfer Objects (DTOs) for data exchange, and validators for input validation. The Application layer depends on the Domain layer and defines interfaces for infrastructure concerns.

- **Infrastructure:** This layer provides the concrete implementations for the interfaces defined in the Application layer. This includes data access implementations (e.g., using Entity Framework Core), external service integrations, identity management, and other technical concerns. The Infrastructure layer depends on the Application and Domain layers.

- **Web:** This layer is the presentation layer, responsible for handling user interactions (e.g., HTTP requests). It is a thin layer that translates user requests into calls to the Application layer and formats the results for the user. It depends on the Application layer.

- **SharedKernel:** This layer contains common utilities and components that are shared across multiple layers, such as the `Result` pattern for handling the outcome of operations and a consistent way to represent errors.

- **AppHost and ServiceDefaults:** These projects are responsible for hosting the application services and providing default configurations using .NET Aspire, similar to a docker-compose file.

## Test Projects

- **Application.FunctionalTests**: End-to-end tests of application use cases via handlers, using real infrastructure where needed and test doubles where appropriate. See `Docs/Development-Guidelines/Application-Functional-Tests-Guidelines.md`.
- **Domain.UnitTests**: Pure unit tests for aggregates, entities, value objects, and domain services. Validate behaviors, invariants, `Result` outcomes, and domain events without external dependencies. See `Docs/Development-Guidelines/Domain_Layer_Test_Guidelines.md`.
- **Web.ApiContractTests**: Contract tests for web endpoints to ensure request/response schemas, status codes, and error shapes remain stable. Treat the API as a contract with strict assertions. See `Docs/Development-Guidelines/WebApi_Contract_Tests_Guidelines.md`.

## Rules for Working with a New Feature

When implementing a new feature in the YummyZoom project, follow these guidelines to maintain the Clean Architecture and DDD principles:

1. **Domain First:** Begin by modeling the core concepts of the new feature in the `Domain` layer. Define the necessary entities, value objects, aggregates, and domain events that represent the business domain of the feature. Ensure that the domain model is rich and encapsulates the business rules.
2. **Define Application Use Cases:** In the `Application` layer, define the commands and queries that represent the use cases for the new feature. Create handlers for these commands and queries. These handlers should orchestrate the domain model to perform the required actions or retrieve the necessary data. Define any required DTOs for data transfer between layers and validators for input.
3. **Implement Infrastructure Details:** In the `Infrastructure` layer, provide the concrete implementations for any interfaces defined in the Application layer that are required by the new feature (e.g., data repositories, external service clients). Configure persistence for the new domain entities if using an ORM like Entity Framework Core.
4. **Create Presentation Layer Entry Points:** In the `Web` layer (or other presentation layers), create the necessary endpoints (e.g., API controllers or minimal APIs) to expose the functionality of the new feature to the outside world. These endpoints should be thin and delegate the business logic to the Application layer by sending commands and queries.
5. **Leverage SharedKernel:** Utilize the common components in the `SharedKernel` for consistent error handling and result management.
6. **Write Comprehensive Tests:** Develop unit tests for the Domain and Application layers, and integration/functional tests for the Infrastructure and Web layers to ensure the new feature works correctly and to prevent regressions.
7. **Adhere to Style and Structure:** Follow the existing coding style, naming conventions, and project structure to maintain consistency across the codebase. Place new files and folders in the appropriate layers and directories based on their responsibility.

> **Note:**  
> The `TodoList` and `TodoItem` classes and related features found across the layers are template placeholders. They serve as implementation examples for understanding the project structure and patterns, but are not part of the actual YummyZoom project scope.
