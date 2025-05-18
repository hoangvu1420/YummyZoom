# YummyZoom Domain Layer Guidelines

The Domain Layer is the core of our application, containing the business logic based on Domain-Driven Design (DDD). It models **what** the system does and the rules governing it, operating independently of infrastructure (like databases) or UI concerns.

---

## Core Concepts & Building Blocks

Familiarize yourself with these fundamental DDD patterns used in the codebase:

1. **Aggregates & Aggregate Roots (`AggregateRoot<TId, TIdType>`):**
    * **What:** A cluster of domain objects (Entities, Value Objects) treated as a single unit for data changes. The Aggregate Root (AR) is the main Entity, the *only* one referenced by objects *outside* the aggregate.
    * **Purpose:** Enforces consistency boundaries. All operations modifying objects within the Aggregate *must* go through the Root to ensure business rules (invariants) are upheld.
    * **In Code:** Classes inheriting from `AggregateRoot`. E.g., `Dinner`, `Menu`, `Host`, `Guest`.
    * **Rules:** Load and save entire Aggregates as a single unit. Reference *other* Aggregates *only by their ID* (a Value Object), never a direct object reference.

2. **Entities (`Entity<TId>`):**
    * **What:** Objects with a distinct identity that persists over time. Equality is based on their unique ID, not their attributes.
    * **Purpose:** Represent domain concepts that have a lifecycle and state changes needing tracking.
    * **In Code:** Classes inheriting from `Entity`. E.g., `Reservation` (within `Dinner` aggregate), `MenuItem` (within `Menu` aggregate), `GuestRating` (within `Guest` aggregate).
    * **Rules:** Entities *within the same Aggregate* can hold direct references to each other. They are owned by their Aggregate Root. Their IDs are typically Value Objects.

3. **Value Objects (`ValueObject`):**
    * **What:** Objects describing a characteristic or attribute. They have no conceptual identity; equality is based *only* on the values of their properties. They should be immutable.
    * **Purpose:** Measure, quantify, or describe things in the domain. Use them instead of simple primitive types for richer meaning (e.g., `Price` instead of `decimal`).
    * **In Code:** Classes inheriting from `ValueObject`. E.g., `Price`, `Location`, `AverageRating`. Also used for non-aggregate IDs like `ReservationId`, `MenuItemId`.
    * **Rules:** Implement `GetEqualityComponents()` to define equality. Once created, their state should not change.

4. **Aggregate Root IDs (`AggregateRootId<TIdType>`):**
    * **What:** A specialized Value Object specifically representing the unique identifier for an Aggregate Root.
    * **Purpose:** Provides strong type safety and clarity when referencing Aggregate Roots.
    * **In Code:** Classes inheriting from `AggregateRootId`. E.g., `DinnerId`, `MenuId`, `HostId`, `GuestId`.

5. **Domain Events (`IDomainEvent`):**
    * **What:** Objects representing a significant occurrence or state change that happened *within* the domain.
    * **Purpose:** To capture events that other parts of the application (or external systems) might react to, decoupling side effects from core logic.
    * **In Code:** Records or classes implementing `IDomainEvent`. E.g., `DinnerCreated`, `MenuCreated`.
    * **Usage:** Raised by domain objects (`AddDomainEvent`) when an action completes. These events are collected and dispatched (usually by the Application layer using MediatR) *after* the Aggregate's state changes are successfully persisted.

6. **Factory Methods (Static `Create` methods):**
    * **What:** Static methods (commonly named `Create` or `CreateNew`) on Aggregates, Entities, or Value Objects.
    * **Purpose:** Encapsulate the creation logic and ensure that objects are *always* created in a valid, consistent state, enforcing initial invariants. Constructors are typically made protected or private to force usage of factories.
    * **In Code:** Static methods like `Dinner.Create(...)`, `Price.Create(...)`, `MenuId.CreateUnique()`.
    * **Rule:** Always use these factory methods to instantiate domain objects.

7. **Error Handling (`Result` and `Result<TValue>`):**
    * **Purpose:** Explicitly signal operation outcomes (success or failure with errors) without exceptions.
    * **What:** `Result` for operations without a success value (like `void`), `Result<TValue>` for operations returning a `TValue` on success. Both carry `Error` objects on failure.
    * **Rule:** Domain methods (especially on Aggregate Roots) should return `Result` or `Result<TValue>` for operations that can fail due to business rules.
    * **In Code:** Use `Result.Success()`, `Result.Success<TValue>(value)`, `Result.Failure(error)`, `Result.Failure<TValue>(error)`. Factory methods may return `Result<TValue>` for validation.
    * **Usage:** Always check `IsSuccess` or `IsFailure`. Access `Result<TValue>.Value` only if `IsSuccess` is true. Propagate errors as needed.
    * **Placement:** Errors for each Aggregate belong in the aggregate's `Errors` folder (e.g., `src\Domain\UserAggregate\Errors`).

---

## Working with the Domain Layer

1. **Interacting with Persistence:** The Domain Layer itself is unaware of how data is stored. Repositories, defined as interfaces (often in the Application Layer or Domain) and implemented in the Infrastructure Layer, are used to Load and Save **entire Aggregates**. You load an Aggregate, call methods on its Root to change its state, and then pass the modified Aggregate back to the Repository to be saved.

2. **Modifying Domain Objects:**
    * Modify the state of an Aggregate **only by calling public methods** on its Aggregate Root. Avoid directly changing properties or collections from outside the AR.
    * These methods encapsulate the specific business operations, ensuring that all necessary logic and invariant checks are performed correctly during state transitions.
    * Internal collections (like a list of reservations) are typically exposed as `IReadOnlyList<T>` to prevent external code from adding or removing items arbitrarily.
    * As noted above, these modification methods often return `Result` or `Result<TValue>` to signal the outcome, including any business rule violations or success.

3. **Creating New Domain Objects:**
    * Always use the static `Create` or `CreateUnique` **Factory Methods** provided on the respective Aggregate Roots, Entities, and Value Objects. These methods contain the logic needed to create valid instances.

4. **Handling Validation:**
    * Understand the different types of validation and where they belong:
        * **Structural/Format Validation:** Checks for basic correctness like "is not null", "is not empty string", "is a positive number", "is a correctly formatted email", "is a valid GUID structure". These checks often occur *early* in the request pipeline. The **Application Layer** (eally, within the Command/Query handlers), potentially using libraries like FluentValidation, is a common place to validate incoming DTOs/Commands *before* attempting to create or load domain objects.
        * **Value Object Structural Validation:** If a structural or format check is fundamental to the definition of a Value Object (e.g., an `EmailAddress` must be a valid format), this validation belongs in the **Value Object's static factory method** (`Create`). This method should return `Result<TValue>` (using `Result.Success<TValue>(value)` or `Result.Failure<TValue>(error)`) to indicate the creation outcome.
        * **Business Rule Validation (Invariants):** Checks based on the *current state* of an Aggregate or relationships *within its boundary* belong in **Domain methods** (primarily on the Aggregate Root). These methods enforce invariants (e.g., "Can a guest reserve a spot? Only if the Dinner is 'Upcoming' and not at `MaxGuests` capacity"). They return `Result` or `Result<TValue>` (using `Result.Success()`, `Result.Success<TValue>(value)`, `Result.Failure(error)`, or `Result.Failure<TValue>(error)`) upon violation or success.
    * **In summary:** Application Layer validates input data structure. Value Objects validate their own inherent structural validity upon creation. Aggregate Root methods validate state transitions against complex business rules.

5. **Raising Domain Events:** Use `AddDomainEvent(IDomainEvent domainEvent)` from within an Aggregate Root or Entity method when a significant business action successfully completes within that object's context. The Aggregate Root base class handles collecting these events for later dispatch.

---

## Key Principles

* **Rich Domain Model:** Domain objects are not just data holders; they contain the behavior (methods) that operates on that data. Logic belongs with the data it affects.
* **Encapsulation:** Protect the internal state of your Aggregates and Entities. Use private/protected setters and expose collections as `IReadOnlyList<T>`. Changes happen via methods.
* **Immutability for Value Objects:** Once a Value Object is created, its internal values should not change. If a different value is needed, create a new instance.
* **Single Responsibility Principle (SRP) for Aggregates:** An Aggregate Root is responsible for maintaining the consistency and enforcing the invariants *only within its own boundary*.
* **Tell, Don't Ask:** Call methods on domain objects to perform actions ("tell the dinner to add a reservation") rather than querying their state externally and making decisions outside the object ("ask the dinner if it's full, then add a reservation if it's not").
* **Ubiquitous Language:** Use the specific terminology agreed upon with domain experts consistently throughout the code (class names, method names, property names, event names).
* **Keep Domain Pure:** The Domain Layer should have **NO direct dependencies** on external layers or frameworks (Databases, APIs, UI, specific logging implementations, ASP.NET Core types). Any necessary external information is passed *into* the domain methods as parameters (e.g., passing a `UserId` to a factory method).
* **Domain Methods Signal Outcomes:** Use `Result` or `Result<TValue>` in domain methods (especially ARs) to signal both success and expected failures (business rule violations).
* **Parameterless Constructors:** These `protected` constructors (often with `#pragma warning disable`) are typically required by ORMs (like Entity Framework) for object materialization. **Never use them directly in your application code** to create instances; always use the factory methods.

---

## What NOT to Do (Common Pitfalls)

* **Don't create public setters for properties.** State changes should happen via methods.
* **Don't modify Entities or Value Objects *directly* from outside their containing Aggregate Root.** Go through the AR.
* **Don't hold direct object references between different Aggregates.** Use their Aggregate Root IDs.
* **Don't put Application or Infrastructure logic** (like saving to database, sending emails, calling external APIs, logging, framework-specific code) **inside Domain objects.**
* **Don't ignore the result of a `Result` or `Result<TValue>` method call.** Always check the `IsSuccess` or `IsFailure` property.
* **Don't create overly large or "god" Aggregates** responsible for unrelated concepts. Keep Aggregates small and focused on a specific consistency boundary (refer to `Notes/Part 11.md` for design process).

---

## Example of a Domain Aggregate

src\Domain\MenuAggregate\Menu.cs:

```csharp
public sealed class Menu : AggregateRoot<MenuId, Guid>
{
    private readonly List<MenuSection> _sections = []; // Owned collection of MenuSection Entities
    private readonly List<DinnerId> _dinnerIds = []; // References to Dinner Aggregate
    private readonly List<MenuReviewId> _menuReviewIds = []; // References to MenuReview Aggregate

    public string Name { get; private set; }
    public string Description { get; private set; }
    public AverageRating AverageRating { get; private set; }
    public IReadOnlyList<MenuSection> Sections => _sections.AsReadOnly();
    public HostId HostId { get; private set; }

    public IReadOnlyList<DinnerId> DinnerIds => _dinnerIds.AsReadOnly();
    public IReadOnlyList<MenuReviewId> MenuReviewIds => _menuReviewIds.AsReadOnly(); 

    private Menu(
        MenuId menuId,
        HostId hostId,
        string name,
        string description,
        AverageRating averageRating,
        List<MenuSection> sections)
        : base(menuId)
    {
        HostId = hostId;
        Name = name;
        Description = description;
        AverageRating = averageRating;
        _sections = sections;
    }

    public static Menu Create(
        HostId hostId,
        string name,
        string description,
        List<MenuSection>? sections = null)
    {
        // TODO: enforce invariants
        var menu = new Menu(
            MenuId.CreateUnique(),
            hostId,
            name,
            description,
            AverageRating.CreateNew(),
            sections ?? []);
        
        menu.AddDomainEvent(new MenuCreated(menu));
        
        return menu;
    }
    
    public void AddDinnerId(DinnerId dinnerId)
    {
        _dinnerIds.Add(dinnerId);
    }

#pragma warning disable CS8618
    private Menu()
    {
    }
#pragma warning restore CS8618
}
```

src\Domain\MenuAggregate\Entities\MenuSection.cs:

```csharp
public sealed class MenuSection : Entity<MenuSectionId>
{
    private readonly List<MenuItem> _items;
    public string Name { get; private set; }
    public string Description { get; private set; }

    public IReadOnlyList<MenuItem> Items => _items.AsReadOnly();

    private MenuSection(
        string name, 
        string description, 
        List<MenuItem> items, 
        MenuSectionId? id = null)
        : base(id ?? MenuSectionId.CreateUnique())
    {
        Name = name;
        Description = description;
        _items = items;
    }

    public static MenuSection Create(
        string name,
        string description,
        List<MenuItem>? items = null)
    {
        // TODO: enforce invariants
        return new MenuSection(name, description, items ?? new());
    }
}
```

src\Domain\MenuAggregate\ValueObjects\MenuId.cs:

```csharp
public sealed class MenuId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private MenuId(Guid value)
    {
        Value = value;
    }

    public static MenuId CreateUnique()
    {
        // TODO: enforce invariants
        return new MenuId(Guid.NewGuid());
    }

    public static MenuId Create(Guid value)
    {
        // TODO: enforce invariants
        return new MenuId(value);
    }

    public static Result<MenuId> Create(string value)
    {
        return !Guid.TryParse(value, out var guid) ? (Result<MenuId>)Errors.Menu.InvalidMenuId : (Result<MenuId>)new MenuId(guid);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

src\Domain\MenuAggregate\ValueObjects\MenuSectionId.cs:

```csharp
public sealed class MenuSectionId : ValueObject
{
    public MenuSectionId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; private set; }

    public static MenuSectionId CreateUnique()
    {
        // TODO: enforce invariants
        return new MenuSectionId(Guid.NewGuid());
    }

    public static MenuSectionId Create(Guid value)
    {
        // TODO: enforce invariants
        return new MenuSectionId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

src\Domain\MenuAggregate\Events\MenuCreated.cs:

```csharp
public record MenuCreated(Menu Menu) : IDomainEvent;
```
