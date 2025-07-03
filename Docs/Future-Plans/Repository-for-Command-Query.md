### On the Command Side: **Use the Repository Pattern**

For your command handlers, abstracting data access behind a repository interface is still a **very good practice**.

**1. The Interface (in `Application` or `Domain` Layer):**

The interface defines the contract in terms of the **Aggregate Root**. It speaks the language of the domain, not the database.

**File Location:** `src/Application/Common/Interfaces/IRestaurantRepository.cs` (or `src/Domain/Restaurants/IRestaurantRepository.cs`)

```csharp
namespace YummyZoom.Application.Common.Interfaces;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(RestaurantId id, CancellationToken cancellationToken = default);
    Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken = default);
    // Note: EF Core's Unit of Work often handles updates, so a specific Update method isn't always needed.
    // The command handler gets the restaurant, modifies it, and the UoW handles the save.
}
```

**2. The Implementation (in `Infrastructure` Layer):**

The implementation uses EF Core to fulfill the contract. This is where the "how" is defined.

**File Location:** `src/Infrastructure/Persistence/Repositories/RestaurantRepository.cs`

```csharp
namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RestaurantRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Restaurant?> GetByIdAsync(RestaurantId id, CancellationToken cancellationToken)
    {
        // Use FindAsync to get the full aggregate
        return await _dbContext.Restaurants.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken)
    {
        await _dbContext.Restaurants.AddAsync(restaurant, cancellationToken);
    }
}
```

**Why is this abstraction valuable for Commands?**

* **Decouples Application from EF Core:** Your command handlers depend on `IRestaurantRepository`, not `ApplicationDbContext`. This makes them easier to unit test with a mock repository. You're testing the application logic, not the database.
* **Enforces Aggregate-Oriented Access:** The repository interface ensures you can only fetch the *entire aggregate*. It prevents developers from writing leaky queries inside a command handler that only fetch parts of an aggregate, which could lead to broken invariants.
* **Clarity of Intent:** `_restaurantRepository.GetByIdAsync()` is clearer and more domain-focused than `_dbContext.Restaurants.FindAsync()`. It reinforces the idea that you are working with a business concept, not a database table.

---

### On the Query Side: **DO NOT Use a Repository Pattern**

For your query handlers, creating a repository abstraction is an **anti-pattern**. It adds complexity for zero benefit and actively works against your goal of performance and flexibility.

**Why is a Repository a bad idea for Queries?**

Imagine you tried to create a `IReadOnlyRestaurantRepository` interface:

```csharp
// DON'T DO THIS
public interface IReadOnlyRestaurantRepository
{
    Task<RestaurantDetailsDto> GetRestaurantDetailsAsync(Guid id);
    Task<IEnumerable<RestaurantSearchResultDto>> SearchRestaurantsAsync(string cuisine, int page, int pageSize);
    Task<IEnumerable<TopRatedRestaurantDto>> GetTopRatedRestaurantsAsync(int count);
    // ... and so on for every single query
}
```

This quickly becomes a disaster:

* **Interface Bloat:** Your repository interface explodes with a method for every possible query your application needs. It becomes a massive, unmaintainable "god interface."
* **Leaky Abstraction:** The DTOs (`RestaurantDetailsDto`, `RestaurantSearchResultDto`) are specific to a single use case. Forcing them into a generic repository interface makes the repository aware of specific UI needs, which violates its purpose.
* **It Adds No Value:** The query handler is already an abstraction. It has one job: handle one specific query. Adding another layer of abstraction (`IReadOnlyRepository`) between the handler and the data access code just adds boilerplate and indirection without providing any real decoupling benefit.

#### The Correct Approach for Queries: **Direct Implementation**

Your query handlers should directly contain the data access logic using Dapper or raw SQL.

**File Location:** `src/Application/Restaurants/Queries/GetRestaurantDetailsQuery.cs`

```csharp
// The Query Handler IS the implementation
public sealed class GetRestaurantDetailsQueryHandler
    : IQueryHandler<GetRestaurantDetailsQuery, RestaurantDetailsDto>
{
    // Depend directly on the connection factory or a read-only DbContext
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetRestaurantDetailsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<RestaurantDetailsDto>> Handle(
        GetRestaurantDetailsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = @"SELECT ... FROM Restaurants ... WHERE Id = @RestaurantId";

        var dto = await connection.QuerySingleOrDefaultAsync<RestaurantDetailsDto>(
            sql, new { RestaurantId = request.RestaurantId });

        // ... return result
    }
}
```

*(`IDbConnectionFactory` would be a simple interface defined in `Application` and implemented in `Infrastructure` to provide a `DbConnection`)*

This approach is:

* **Lean and Mean:** No unnecessary layers.
* **Self-Contained:** Everything needed to understand and execute the query is in one file.
* **Optimized:** The handler can craft the most efficient SQL query possible for its specific DTO, without compromise.

---

### Final Overview and Recommendation

| Path | Abstraction Strategy | Rationale |
| :--- | :--- | :--- |
| **Command** | **Repository Pattern** <br> `CommandHandler` -> `IRepository` -> EF Core `DbContext` | • Enforces aggregate boundaries. <br> • Decouples application from EF Core. <br> • Enables clean unit testing. <br> • Focuses on domain concepts. |
| **Query** | **No Repository** <br> `QueryHandler` -> Dapper / Raw SQL `DbConnection` | • Avoids interface bloat and leaky abstractions. <br> • Maximizes performance and flexibility. <br> • Keeps query logic self-contained and easy to find. <br> • A repository adds zero value here. |

In summary: **Yes, absolutely abstract your write-side data access with repositories. And no, absolutely do not abstract your read-side data access with repositories.** This two-pronged strategy fully embraces the spirit of CQRS and gives you a clean, maintainable, and highly performant architecture.
