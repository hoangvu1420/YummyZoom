The diagram illustrates the tables, columns, primary keys (PK), foreign keys (FK), unique constraints (UK), and relationships. Comments within the diagram explain specific design choices like owned types, which are a key feature of Domain-Driven Design (DDD) implemented with EF Core.

**This diagram serves as a blueprint for the database schema. This diagram should be kept up-to-date as the model evolves.**

### Database Schema (Mermaid ER Diagram)

```mermaid
erDiagram
    %% --- ASP.NET Core Identity Aggregate ---
    AspNetUsers {
        UUID Id PK
        string UserName UK
        string NormalizedUserName
        string Email
        string NormalizedEmail
        boolean EmailConfirmed
        string PasswordHash
        string SecurityStamp
        string ConcurrencyStamp
        string PhoneNumber
        boolean PhoneNumberConfirmed
        boolean TwoFactorEnabled
        timestamp LockoutEnd
        boolean LockoutEnabled
        int AccessFailedCount
    }

    AspNetRoles {
        UUID Id PK
        string Name
        string NormalizedName
        string ConcurrencyStamp
    }

    AspNetUserRoles {
        UUID UserId PK, FK
        UUID RoleId PK, FK
    }

    AspNetUserClaims {
        int Id PK
        UUID UserId FK
        string ClaimType
        string ClaimValue
    }

    AspNetUserLogins {
        string LoginProvider PK
        string ProviderKey PK
        UUID UserId FK
        string ProviderDisplayName
    }

    AspNetUserTokens {
        UUID UserId PK, FK
        string LoginProvider PK
        string Name PK
        string Value
    }

    AspNetRoleClaims {
        int Id PK
        UUID RoleId FK
        string ClaimType
        string ClaimValue
    }

    %% --- DomainUsers Aggregate ---
    DomainUsers {
        UUID Id PK
        string Name
        string Email UK
        string PhoneNumber
        boolean IsActive
    }

    UserAddresses {
        UUID AddressId PK
        UUID UserId FK
        string Label
        string Street
        string City
        string Country
        string ZipCode
    }

    UserPaymentMethods {
        UUID PaymentMethodId PK
        UUID UserId FK
        string Type
        string TokenizedDetails
        boolean IsDefault
    }

    %% --- RoleAssignments Aggregate (references DomainUsers) ---
    RoleAssignments {
        UUID Id PK
        UUID UserId
        UUID RestaurantId
        string Role
        %% Unique Key: (UserId, RestaurantId, Role)
    }

    %% --- UserDeviceSessions Store (references DomainUsers, Devices) ---
    UserDeviceSessions {
        UUID Id PK
        UUID UserId
        UUID DeviceId
        string FcmToken
        boolean IsActive
    }

    Devices {
        UUID Id PK
        string DeviceId UK
        string Platform
        string ModelName
    }

    %% --- TodoLists Aggregate ---
    TodoLists {
        UUID Id PK
        string Title
        string Colour "Owned VO"
    }

    TodoItems {
        UUID TodoListId PK, FK
        UUID TodoItemId PK
        string Title
        string Note
        boolean IsDone
    }

    %% --- Relationships (Grouped by Aggregate) ---

    %% ASP.NET Core Identity Aggregate
    AspNetUsers ||--o{ AspNetUserRoles : "has"
    AspNetRoles ||--o{ AspNetUserRoles : "has"
    AspNetUsers ||--o{ AspNetUserClaims : "has"
    AspNetUsers ||--o{ AspNetUserLogins : "has"
    AspNetUsers ||--o{ AspNetUserTokens : "has"
    AspNetRoles ||--o{ AspNetRoleClaims : "has"

    %% Shared PK: Identity <-> DomainUsers
    AspNetUsers ||--|| DomainUsers : "is"

    %% DomainUsers Aggregate
    DomainUsers ||--o{ UserAddresses : "owns"
    DomainUsers ||--o{ UserPaymentMethods : "owns"

    %% RoleAssignments references DomainUsers (no FK)
    DomainUsers ||..|| RoleAssignments : "is referenced by"

    %% UserDeviceSessions Store
    DomainUsers ||..|| UserDeviceSessions : "is referenced by"
    Devices ||..|| UserDeviceSessions : "is referenced by"

    %% TodoLists Aggregate
    TodoLists ||--o{ TodoItems : "owns"
```

### Key Changes and Rationale

1. **Dotted Lines for Aggregate References**: The relationships from `RoleAssignments` to `DomainUsers`, and from `UserDeviceSessions` to both `DomainUsers` and `Devices`, are now shown with **dotted lines (`||..||`)**. This visually represents that they are references by ID only, without a database-level foreign key constraint, correctly adhering to the DDD principle.
2. **Solid Lines for Enforced Constraints**:
    * Relationships *within* the ASP.NET Identity schema (e.g., `AspNetUserClaims` to `AspNetUsers`) remain solid as they have enforced FKs.
    * The link between an aggregate root and its owned entities (e.g., `DomainUsers` to `UserAddresses`) also remains solid, as this is a strong, composition-based relationship enforced by an FK.
3. **Shared Primary Key**: The `AspNetUsers` and `DomainUsers` one-to-one relationship (`||--||`) is kept as a solid line because they are linked by a shared primary key, representing two facets of the same core entity, rather than one aggregate referencing another distinct aggregate.
