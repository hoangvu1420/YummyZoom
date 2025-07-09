Of course. The `User` aggregate is central to the entire platform, so its application layer design requires careful consideration of security, user experience, and system-wide data integrity.

Here is the Feature Discovery and Application Layer Design for the `User` aggregate.

---

## Feature Discovery & Application Layer Design

### Aggregate Under Design: `User`

### 1. Core Use Cases & Actors

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Guest / Unauthenticated User` | Register for a new account. | Creates a new `User` aggregate, typically involving email verification and password creation. |
| `Customer (User)` | Manage their profile. | Updates their name and phone number. |
| `Customer (User)` | Manage their delivery addresses. | Adds, edits, or removes delivery addresses for faster checkout. |
| `Customer (User)` | Manage their payment methods. | Adds, removes, or sets a default tokenized payment method. |
| `Customer (User)` | Change their account email. | Initiates and completes a secure process to update their primary login email. |
| `Admin` | Manage platform users. | Views a list of all users, views their details, and can activate or deactivate their accounts for moderation purposes. |
| `Admin` | Fulfill a data deletion request (GDPR). | Initiates a process to permanently delete or anonymize a user's data across the entire system. |
| `System (Auth Process)` | Authenticate a user. | Looks up a user by email to verify their identity during login. |
| `System (Event Handler)` | Anonymize user data upon deletion. | A background process that scrubs Personally Identifiable Information (PII) from related aggregates like `Order` and `Review` after a user is deleted. |

---

### 2. Commands (Write Operations)

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization Policy |
| :--- | :--- | :--- | :--- | :--- |
| **`RegisterUserCommand`** | `Guest` | `Name`, `Email`, `Password` | `RegisterUserResponse(UserId)` | `Public` (No auth required). |
| **`UpdateUserProfileCommand`** | `Customer (Self)` | `UserId`, `Name`, `PhoneNumber` | `Result.Success()` | Must be the owner of the `UserId`. |
| **`AddAddressCommand`** | `Customer (Self)` | `UserId`, `AddressDto` | `AddAddressResponse(AddressId)` | Must be the owner of the `UserId`. |
| **`RemoveAddressCommand`** | `Customer (Self)` | `UserId`, `AddressId` | `Result.Success()` | Must be the owner of the `UserId` and the `AddressId`. |
| **`AddPaymentMethodCommand`** | `Customer (Self)` | `UserId`, `PaymentGatewayToken`, `DisplayName` | `AddPaymentMethodResponse(PaymentMethodId)` | Must be the owner of the `UserId`. |
| **`RemovePaymentMethodCommand`** | `Customer (Self)` | `UserId`, `PaymentMethodId` | `Result.Success()` | Must be the owner of the `UserId` and the `PaymentMethodId`. |
| **`SetDefaultPaymentMethodCommand`** | `Customer (Self)` | `UserId`, `PaymentMethodId` | `Result.Success()` | Must be the owner of the `UserId` and the `PaymentMethodId`. |
| **`RequestEmailChangeCommand`** | `Customer (Self)` | `UserId`, `NewEmail` | `Result.Success()` | Must be the owner of the `UserId`. |
| **`ConfirmEmailChangeCommand`** | `Customer (Self)` | `UserId`, `ConfirmationToken` | `Result.Success()` | Must be the owner of the `UserId`. |
| **`DeactivateUserCommand`** | `Admin` | `UserId` | `Result.Success()` | `Admin` role required. |
| **`DeleteUserAccountCommand`** | `Admin`, `User (Self)` | `UserId`, `ForceDelete` (Admin only) | `Result.Success()` | `Admin` role or owner of the `UserId`. |

---

### 3. Queries (Read Operations)

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`GetUserProfileQuery`** | `Customer (Self)` | `UserId` | `UserProfileDto` (with addresses, payments) | `SELECT u.* FROM "Users" u WHERE u.Id = @Id;` followed by separate queries for `Addresses` and `PaymentMethods`. |
| **`GetUsersForAdminQuery`** | `Admin` | `PaginationParams`, `FilterCriteria` (name, email, status) | `PaginatedList<UserSummaryDto>` | `SELECT Id, Name, Email, IsActive, CreatedAt FROM "Users" WHERE ... ORDER BY ...` |
| **`GetUserDetailsForAdminQuery`** | `Admin` | `UserId` | `AdminUserDetailsDto` (profile, addresses, payments, recent activity) | `JOIN` across `Users`, `Addresses`, `PaymentMethods`. May also query `Orders` table for recent activity summary. |
| **`GetUserByEmailQuery`** | `System (Internal)` | `Email` | `InternalUserDto (Id, IsActive)` | `SELECT Id, IsActive FROM "Users" WHERE Email = @Email` (heavily indexed). |

---

### 4. Domain Event Handling

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| **`UserCreated`** | `RegisterUserCommand` | `SendWelcomeEmailHandler` | Sends a "Welcome to YummyZoom!" email to the user. |
| **`UserCreated`** | `RegisterUserCommand` | `CreateDefaultCustomerProfileHandler` | Could create entries in other systems (e.g., marketing, analytics) for the new customer. |
| **`UserEmailChanged`** | `ConfirmEmailChangeCommand`| `InvalidateSessionsAndNotifyOldEmailHandler` | Forces logout on all devices by invalidating refresh tokens. Sends a security alert to the user's *old* email address. |
| **`UserDeleted`** | `DeleteUserAccountCommand` | **`AnonymizeUserDataSaga` (Process Manager)** | **CRITICAL:** This is not a simple handler. It kicks off a long-running process to: 1. Delete associated `RoleAssignment`s. 2. Find all `Order`s by this user and replace their `CustomerID` and `DeliveryAddress` with anonymized values. 3. Find all `Review`s and anonymize the `CustomerID`. 4. Remove data from any external systems (e.g., payment gateway customer record). |
| **`UserDeactivated`**| `DeactivateUserCommand` | `LogUserOutOfAllDevicesHandler` | Immediately invalidates all authentication tokens associated with the `UserId`. |

---

### 5. Key Business Logic & Application Service Orchestration

#### **`RegisterUserCommandCommandHandler` Orchestration:**

1.  **Validate** command input using FluentValidation (e.g., password complexity, valid email format).
2.  **Authorize**: This is a public endpoint, so no authorization is needed.
3.  **Start a transaction** (or rely on the `UnitOfWork`'s implicit transaction).
4.  **Perform pre-invocation business checks:**
    *   **Email Uniqueness:** This is the most critical check. `var emailExists = await _userRepository.IsEmailUniqueAsync(command.Email);` If `false`, return a `UserErrors.EmailAlreadyExists` error. This check *must* happen in the application service, not the aggregate.
5.  **Use an Authentication Service:**
    *   The application service, **not the domain model**, is responsible for passwords.
    *   `var authResult = await _authService.CreateUserAsync(command.Email, command.Password);` (e.g., using `UserManager` from ASP.NET Core Identity).
    *   If `authResult.IsFailure`, return the authentication error. The `_authService` handles all password hashing and storage.
6.  **Invoke the Aggregate's Method:**
    *   `var userCreationResult = User.Create(command.Name, command.Email, ...);`
    *   If `userCreationResult.IsFailure`, return the domain error.
7.  **Persist the new aggregate:**
    *   `await _userRepository.AddAsync(userCreationResult.Value);`
8.  **Complete the transaction.** The `UnitOfWork` will commit both the new `User` record and the auth system's changes (if in the same DB transaction) and dispatch the `UserCreated` event.
9.  **Map and return** the `RegisterUserResponse` DTO.

---

### Design Notes & Suggestions

1.  **Password Management:** The current design is **excellent** in that the `User` aggregate does not contain any password information. This is the correct approach. The orchestration outlined above, where the Application Service uses a dedicated authentication library (`_authService`), is the recommended implementation pattern. It perfectly separates domain concerns from security infrastructure.

2.  **Email Change Security:** The `UpdateEmail` method is dangerously simple. An email address is a primary identifier and security credential.
    *   **Recommendation:** Implement a two-step email change process as reflected in the commands:
        1.  `RequestEmailChangeCommand`: The user provides the new email. The system generates a unique, time-sensitive token, stores it (e.g., in a cache or a dedicated table with the `UserId` and `NewEmail`), and sends a confirmation link to the *new* email address.
        2.  `ConfirmEmailChangeCommand`: The user clicks the link, which calls this command with the token. The handler validates the token and only then calls the `User.UpdateEmail()` method on the aggregate.

3.  **GDPR & Data Deletion:** The `MarkAsDeleted` method is a good start for a "soft delete," but it doesn't address the "right to be forgotten."
    *   **Recommendation:** The `UserDeleted` event must trigger a sophisticated, robust **Saga or Process Manager**. A simple event handler is insufficient because anonymizing data across many `Order` and `Review` aggregates can be a long-running, fallible process. A saga ensures that if one part fails, it can be retried or compensated, guaranteeing eventual consistency for the deletion request.

4.  **Child Entity Management:** The `AddAddress` and `AddPaymentMethod` methods take a pre-constructed child entity. This is acceptable, but a more encapsulated approach is often preferred.
    *   **Suggestion:** Consider changing the aggregate methods to accept primitive values, e.g., `Result AddAddress(string street, string city, ...)` and `Result AddPaymentMethod(string token, string displayName, ...)` . The aggregate root would then be responsible for creating the `Address` or `PaymentMethod` child entity itself, ensuring all its own invariants are met internally. This strengthens the aggregate boundary.