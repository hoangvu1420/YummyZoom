The diagram illustrates the tables, columns, primary keys (PK), foreign keys (FK), unique constraints (UK), and relationships. Comments within the diagram explain specific design choices like owned types and value objects (VO), which are key features of Domain-Driven Design (DDD) implemented with EF Core.

**This diagram serves as a blueprint for the database schema. This diagram should be kept up-to-date as the model evolves.**

### Database Schema (Mermaid ER Diagram)

```mermaid
---
config:
  theme: default
---

erDiagram
    %% =============================================================
    %% BOUNDED CONTEXTS (See Domain_Design.md)
    %%   Identity & Access
    %%   Restaurant Catalog
    %%   Order & Fulfillment (includes TeamCart)
    %%   Payouts & Monetization
    %%   Support & Governance
    %%   Misc / Shared
    %% Legend: solid line = enforced FK / ownership inside boundary; dotted = logical cross-context reference (no FK)
    %% =============================================================

    %% ---------------- Identity & Access Context ----------------
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

    %% --- User Aggregate (DomainUsers) ---
    DomainUsers {
        UUID Id PK
        string Name
        string Email UK
        string PhoneNumber
        boolean IsActive
    }

    UserAddresses {
        UUID UserId PK, FK
        UUID AddressId PK
        string Label
        string Street
        string City
        string State
        string Country
        string ZipCode
        string DeliveryInstructions
    }

    UserPaymentMethods {
        UUID UserId PK, FK
        UUID PaymentMethodId PK
        string Type
        string TokenizedDetails
        boolean IsDefault
    }

    %% ---------------- Restaurant Catalog Context ----------------
    %% --- Restaurant Aggregate ---
    Restaurants {
        UUID Id PK
        string Name
        string Description
        string CuisineType
        string LogoUrl
        string BackgroundImageUrl
        boolean IsVerified
        boolean IsAcceptingOrders
        string Location_Street "Location VO"
        string Location_City "Location VO"
        string Location_State "Location VO"
        string Location_Country "Location VO"
        string Location_ZipCode "Location VO"
        decimal Geo_Latitude "GeoCoordinates VO"
        decimal Geo_Longitude "GeoCoordinates VO"
        string ContactInfo_PhoneNumber "Contact VO"
        string ContactInfo_Email "Contact VO"
        string BusinessHours "BusinessHours VO"
    }

    %% ---------------- Cross-Context Bridge (Identity ↔ Catalog) ----------------
    %% --- RoleAssignments Aggregate ---
    RoleAssignments {
        UUID Id PK
        UUID UserId "Ref to DomainUsers"
        UUID RestaurantId "Ref to Restaurants"
        string Role
        %% UK: (UserId, RestaurantId, Role)
    }

    %% --- Menu & Catalog Entities ---
    Menus {
        UUID Id PK
        UUID RestaurantId FK
        string Name
        string Description
        boolean IsEnabled
    }

    MenuCategories {
        UUID Id PK
        UUID MenuId FK
        string Name
        int DisplayOrder
    }

    MenuItems {
        UUID Id PK
        UUID RestaurantId "Ref to Restaurants"
        UUID MenuCategoryId "Ref to MenuCategories"
        string Name
        string Description
        string ImageUrl
        decimal BasePrice_Amount "Money VO"
        string BasePrice_Currency "Money VO"
        boolean IsAvailable
        jsonb AppliedCustomizations "List<CustomizationGroupID>"
        jsonb DietaryTagIds "List<TagID>"
    }

    CustomizationGroups {
        UUID Id PK
        UUID RestaurantId "Ref to Restaurants"
        string GroupName
        int MinSelections
        int MaxSelections
    }

    CustomizationChoices {
        UUID CustomizationGroupId PK, FK
        UUID ChoiceId PK
        string Name
        decimal PriceAdjustment_Amount "Money VO"
        string PriceAdjustment_Currency "Money VO"
        boolean IsDefault
        int DisplayOrder
    }
    
    Tags {
        UUID Id PK
        string TagName UK
        string TagDescription
        string TagCategory
    }

    %% ---------------- Order & Fulfillment Context ----------------
    %% --- Order Aggregate ---
    Orders {
        UUID Id PK
        string OrderNumber UK
        UUID CustomerId "Ref to DomainUsers"
        UUID RestaurantId "Ref to Restaurants"
        UUID AppliedCouponId "Ref to Coupons"
        UUID SourceTeamCartId "Ref to TeamCarts"
        string Status
        timestamp PlacementTimestamp
        timestamp LastUpdateTimestamp
        timestamp EstimatedDeliveryTime
        timestamp ActualDeliveryTime
        string SpecialInstructions
        string DeliveryAddress_Street "Address VO"
        string DeliveryAddress_City "Address VO"
        string DeliveryAddress_State "Address VO"
        string DeliveryAddress_Country "Address VO"
        string DeliveryAddress_ZipCode "Address VO"
        decimal Subtotal_Amount "Money VO"
        string Subtotal_Currency "Money VO"
        decimal DiscountAmount_Amount "Money VO"
        string DiscountAmount_Currency "Money VO"
        decimal DeliveryFee_Amount "Money VO"
        string DeliveryFee_Currency "Money VO"
        decimal TipAmount_Amount "Money VO"
        string TipAmount_Currency "Money VO"
        decimal TaxAmount_Amount "Money VO"
        string TaxAmount_Currency "Money VO"
        decimal TotalAmount_Amount "Money VO"
        string TotalAmount_Currency "Money VO"
    }

    OrderItems {
        UUID OrderId PK, FK
        UUID OrderItemId PK
        UUID Snapshot_MenuItemId
        string Snapshot_ItemName
        decimal Snapshot_BasePriceAtOrder_Amount "Money VO"
        string Snapshot_BasePriceAtOrder_Currency "Money VO"
        int Quantity
        jsonb SelectedCustomizations
        decimal LineItemTotal_Amount "Money VO"
        string LineItemTotal_Currency "Money VO"
    }
    
    PaymentTransactions {
        UUID OrderId PK, FK
        UUID PaymentTransactionId PK
        string Type
        string Status
        decimal Transaction_Amount "Money VO"
        string Transaction_Currency "Money VO"
        timestamp Timestamp
        string PaymentGatewayReferenceId
        string PaymentMethodType
        string PaymentMethodDisplay
        UUID PaidByUserId
    }

    %% --- Coupon Aggregate ---
    Coupons {
        UUID Id PK
        UUID RestaurantId "Ref to Restaurants"
        string Code
        string Description
        string Value_Type "CouponValue VO"
        decimal Value_PercentageValue "CouponValue VO"
        decimal Value_FixedAmount_Amount "CouponValue VO"
        string Value_FixedAmount_Currency "CouponValue VO"
        UUID Value_FreeItemValue "CouponValue VO"
        string AppliesTo_Scope "AppliesTo VO"
        jsonb AppliesTo_ItemIds "AppliesTo VO"
        jsonb AppliesTo_CategoryIds "AppliesTo VO"
        decimal MinOrderAmount_Amount "Money VO"
        string MinOrderAmount_Currency "Money VO"
        timestamp ValidityStartDate
        timestamp ValidityEndDate
        int TotalUsageLimit
        int CurrentTotalUsageCount
        int UsageLimitPerUser
        boolean IsEnabled
        %% UK: (Code, RestaurantId)
    }
    
    %% --- Review Aggregate ---
    Reviews {
        UUID Id PK
        UUID OrderId "Ref to Orders"
        UUID CustomerId "Ref to DomainUsers"
        UUID RestaurantId "Ref to Restaurants"
        int Rating "Rating VO"
        string Comment
        timestamp SubmissionTimestamp
        boolean IsModerated
        boolean IsHidden
        string Reply
    }

    %% ---------------- Payouts & Monetization Context ----------------
    %% --- Payouts & Monetization ---
    RestaurantAccounts {
        UUID Id PK
        UUID RestaurantId UK
        decimal CurrentBalance_Amount "Money VO"
        string CurrentBalance_Currency "Money VO"
        string PayoutMethod_Details "PayoutMethod VO"
    }

    AccountTransactions {
        UUID Id PK
        UUID RestaurantAccountId FK
        string Type
        decimal Amount "Money VO"
        string Currency "Money VO"
        timestamp Timestamp
        UUID RelatedOrderId "Ref to Orders"
        string Notes
    }

    %% ---------------- Support & Governance Context ----------------
    %% --- Support & Governance ---
    SupportTickets {
        UUID Id PK
        string TicketNumber UK
        string Subject
        string Status
        string Priority
        string Type
        timestamp SubmissionTimestamp
        timestamp LastUpdateTimestamp
        UUID AssignedToAdminId
    }

    SupportTicketMessages {
        UUID SupportTicketId PK, FK
        UUID MessageId PK
        UUID AuthorId
        string AuthorType
        string MessageText
        timestamp Timestamp
        boolean IsInternalNote
    }
    
    SupportTicketContextLinks {
        UUID SupportTicketId PK, FK
        UUID EntityID PK
        string EntityType PK
    }
    
    %% ---------------- TeamCart (Order & Fulfillment Context) ----------------
    %% --- TeamCart Aggregate ---
    TeamCarts {
        UUID Id PK
        UUID RestaurantId "Ref to Restaurants"
        UUID HostUserId "Ref to DomainUsers"
        string Status
        string ShareToken_Value "ShareToken VO"
        timestamp ShareToken_ExpiresAt "ShareToken VO"
        timestamp Deadline
        timestamp CreatedAt
        timestamp ExpiresAt
        decimal TipAmount_Amount "Money VO"
        string TipAmount_Currency "Money VO"
        UUID AppliedCouponId "Ref to Coupons"
    }

    TeamCartMembers {
        UUID TeamCartId PK, FK
        UUID TeamCartMemberId PK
        UUID UserId "Ref to DomainUsers"
        string Name
        string Role
    }

    TeamCartItems {
        UUID TeamCartId PK, FK
        UUID TeamCartItemId PK
        UUID AddedByUserId "Ref to DomainUsers"
        UUID Snapshot_MenuItemId
        UUID Snapshot_MenuCategoryId
        string Snapshot_ItemName
        decimal BasePrice_Amount "Money VO"
        string BasePrice_Currency "Money VO"
        int Quantity
        decimal LineItemTotal_Amount "Money VO"
        string LineItemTotal_Currency "Money VO"
        jsonb SelectedCustomizations
    }

    TeamCartMemberPayments {
        UUID TeamCartId PK, FK
        UUID MemberPaymentId PK
        UUID UserId "Ref to DomainUsers"
        decimal Payment_Amount "Money VO"
        string Payment_Currency "Money VO"
        string Method
        string Status
        string OnlineTransactionId
        timestamp CreatedAt
        timestamp UpdatedAt
    }
    
    %% ---------------- Misc / Shared Tables ----------------
    %% --- Misc / Application Service Tables ---
    Devices {
        UUID Id PK
        string DeviceId UK
        string Platform
        string ModelName
    }
    
    UserDeviceSessions {
        UUID Id PK
        UUID UserId "Ref to DomainUsers"
        UUID DeviceId "Ref to Devices"
        string FcmToken
        boolean IsActive
    }
    
    %% --- Relationships ---
    %% =============================================================
    %% RELATIONSHIPS GROUPED BY BOUNDED CONTEXT
    %% =============================================================

    %% Identity & Access
    AspNetUsers ||--o{ AspNetUserRoles : "has"
    AspNetRoles ||--o{ AspNetUserRoles : "has"
    AspNetUsers ||--o{ AspNetUserClaims : "has"
    AspNetUsers ||--o{ AspNetUserLogins : "has"
    AspNetUsers ||--o{ AspNetUserTokens : "has"
    AspNetRoles ||--o{ AspNetRoleClaims : "has"
    AspNetUsers ||--|| DomainUsers : "is"
    DomainUsers ||--o{ UserAddresses : "owns"
    DomainUsers ||--o{ UserPaymentMethods : "owns"

    %% Restaurant Catalog
    Restaurants ||..o| Menus : "has"
    Restaurants ||..o| CustomizationGroups : "defines"
    Menus }o--|| MenuCategories : "contains"
    MenuCategories ||..o| MenuItems : "groups"
    CustomizationGroups ||--o{ CustomizationChoices : "owns"

    %% MenuItems tagged with Tags 
    MenuItems }o..o{ Tags : "tagged with"

    %% Additional Catalog Relationships
    Restaurants ||..o| MenuItems : "offers"

    %% --- Added logical reference relationships for Id/Ids fields ---
    %% Coupons and their scopes
    Restaurants      ||..o| Coupons : "issues"
    Coupons          ||..o| TeamCarts : "applied to"
    Coupons          }o..o{ MenuItems : "applies to items"
    Coupons          }o..o{ MenuCategories : "applies to categories"

    %% Snapshot references (historical copies, not enforced FKs)
    MenuItems        ||..o| OrderItems : "snapshotted by"
    MenuItems        ||..o| TeamCartItems : "snapshotted by"
    MenuCategories   ||..o| TeamCartItems : "snapshotted by"

    %% Payment & Accounting cross references
    DomainUsers      ||..o| PaymentTransactions : "pays"
    Orders           ||..o| AccountTransactions : "referenced by"

    %% Support system references
    DomainUsers      ||..o| SupportTickets : "assigned"
    DomainUsers      ||..o| SupportTicketMessages : "authors"

    %% Team cart user participation
    DomainUsers      ||..o| TeamCartMembers : "joins"
    DomainUsers      ||..o| TeamCartItems : "adds"
    DomainUsers      ||..o| TeamCartMemberPayments : "pays"

    %% Cross-Context Bridge
    %% Role Assignments (connects Users and Restaurants)
    DomainUsers      ||..|| RoleAssignments : "is assigned"
    Restaurants      ||..|| RoleAssignments : "has roles for"

    %% Order & Fulfillment
    Orders ||--o{ OrderItems : "owns"
    Orders ||--o{ PaymentTransactions : "owns"
    DomainUsers      ||..o| Orders : "places"
    Restaurants      ||..o| Orders : "fulfills"
    Coupons          ||..o| Orders : "is applied to"
    TeamCarts        ||..o| Orders : "is converted to"

    %% Reviews
    DomainUsers      ||..o| Reviews : "writes"
    Restaurants      ||..o| Reviews : "is reviewed for"
    Orders           |o..o| Reviews : "is basis of"

    %% Payouts & Monetization
    Restaurants      ||..o| RestaurantAccounts : "has"
    RestaurantAccounts ||..o{ AccountTransactions : "has ledger of"
    
    %% Support & Governance
    SupportTickets ||--o{ SupportTicketMessages : "owns"
    SupportTickets ||--o{ SupportTicketContextLinks : "owns"

    %% Team Carts (Order & Fulfillment)
    TeamCarts ||--o{ TeamCartMembers : "owns"
    TeamCarts ||--o{ TeamCartItems : "owns"
    TeamCarts ||--o{ TeamCartMemberPayments : "owns"
    DomainUsers ||..o| TeamCarts : "hosts"
    Restaurants ||..o| TeamCarts : "is for"
    
    %% Device Tracking (Misc / Shared)
    DomainUsers ||..|| UserDeviceSessions : "is referenced by"
    Devices     ||..|| UserDeviceSessions : "is referenced by"
    
    %% ---------------- Styling (Bounded Context Colors) ----------------
    %% Identity & Access (Blue)
    style AspNetUsers fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style AspNetRoles fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style AspNetUserRoles fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style AspNetUserClaims fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style AspNetUserLogins fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style AspNetUserTokens fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style AspNetRoleClaims fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style DomainUsers fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style UserAddresses fill:#E3F2FD,stroke:#1565C0,stroke-width:1px
    style UserPaymentMethods fill:#E3F2FD,stroke:#1565C0,stroke-width:1px

    %% Cross-Context Bridge (Teal)
    style RoleAssignments fill:#E0F7FA,stroke:#00838F,stroke-width:1px

    %% Restaurant Catalog (Green)
    style Restaurants fill:#E8F5E9,stroke:#2E7D32,stroke-width:1px
    style Menus fill:#E8F5E9,stroke:#2E7D32,stroke-width:1px
    style MenuCategories fill:#E8F5E9,stroke:#2E7D32,stroke-width:1px
    style MenuItems fill:#E8F5E9,stroke:#2E7D32,stroke-width:1px
    style CustomizationGroups fill:#E8F5E9,stroke:#2E7D32,stroke-width:1px
    style CustomizationChoices fill:#E8F5E9,stroke:#2E7D32,stroke-width:1px
    style Tags fill:#E8F5E9,stroke:#2E7D32,stroke-width:1px

    %% Order & Fulfillment (Orange)
    style Orders fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px
    style OrderItems fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px
    style PaymentTransactions fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px
    style Coupons fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px
    style Reviews fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px
    style TeamCarts fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px
    style TeamCartMembers fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px
    style TeamCartItems fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px
    style TeamCartMemberPayments fill:#FFF3E0,stroke:#EF6C00,stroke-width:1px

    %% Payouts & Monetization (Purple)
    style RestaurantAccounts fill:#F3E5F5,stroke:#6A1B9A,stroke-width:1px
    style AccountTransactions fill:#F3E5F5,stroke:#6A1B9A,stroke-width:1px

    %% Support & Governance (Red / Pink)
    style SupportTickets fill:#FFEBEE,stroke:#C62828,stroke-width:1px
    style SupportTicketMessages fill:#FFEBEE,stroke:#C62828,stroke-width:1px
    style SupportTicketContextLinks fill:#FFEBEE,stroke:#C62828,stroke-width:1px

    %% Misc / Shared (Gray)
    style Devices fill:#ECEFF1,stroke:#455A64,stroke-width:1px
    style UserDeviceSessions fill:#ECEFF1,stroke:#455A64,stroke-width:1px

    %% ------------------------------------------------------------------
    %% NOTE: Mermaid's erDiagram styling support may vary by renderer.
    %% If styles do not apply in your environment, consider switching
    %% to a flowchart-based diagram for richer theming.
    %% ------------------------------------------------------------------
```

### Key Changes and Rationale

#### Bounded Context Color Legend

| Bounded Context | Color |
|-----------------|-------|
| Identity & Access | Blue |
| Cross-Context Bridge | Teal |
| Restaurant Catalog | Green |
| Order & Fulfillment (incl. TeamCart) | Orange |
| Payouts & Monetization | Purple |
| Support & Governance | Red/Pink |
| Misc / Shared | Gray |

1.  **Full Schema Representation**: The diagram has been expanded to include all aggregates and entities defined in `Domain_Design.md` and implemented in the `ApplicationDbContextModelSnapshot.cs`. This includes new tables for `SupportTickets`, `TeamCarts`, `Reviews`, `Coupons`, `RestaurantAccounts`, and their child entities.
2.  **Owned Entities and VOs**:
    *   **Child Tables**: Owned entities that get their own table via `OwnsMany` (e.g., `OrderItems`, `CustomizationChoices`, `TeamCartMembers`) are shown as separate tables with a solid composition relationship (`||--o{`) from their owner.
    *   **Value Objects (VOs)**: Properties of owned entities configured with `OwnsOne` (e.g., `Restaurant.Location`, `Order.TotalAmount`, `Coupon.Value`) are flattened into the parent table's columns. These columns are now listed directly in the parent entity in the diagram with a `VO` comment for clarity (e.g., `Location_Street "Location VO"`).
3.  **Accurate Column Naming**: All column names, especially for flattened Value Objects (like `BasePrice_Amount`, `AppliesTo_Scope`), now precisely match the physical schema in the database snapshot.
4.  **Relationship Clarity**:
    *   **Dotted Lines (`||..||`)**: Used for logical references between different aggregates where no database-level foreign key constraint exists, adhering to DDD principles (e.g., `RoleAssignments` to `DomainUsers`).
    *   **Solid Lines (`||--o{` or `}o--||`)**: Used for relationships with enforced foreign key constraints, such as the parent-child relationship within an aggregate (e.g., `Orders` to `OrderItems`) or a direct FK link between entities (e.g., `MenuCategories` to `Menus`).
5.  **Constraints and Keys**: Primary Keys (PK), Foreign Keys (FK), and Unique Keys (UK) have been added based on the snapshot configuration (`HasKey`, `HasForeignKey`, `IsUnique`). Unique key compositions are noted in comments.
6.  **Comprehensive Relationships**: All logical connections between aggregates are now drawn, providing a complete picture of how domain objects interact (e.g., how an `Order` relates to a `User`, `Restaurant`, and `Coupon`).
