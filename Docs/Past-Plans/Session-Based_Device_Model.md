You are absolutely right:

* A **User** can have many active devices.
* A **Device** can be used by many users over its lifetime.
* The link between a User and a Device needs to be managed, especially the **active FCM token** for that specific combination.

The simple `UserDevice` model we discussed earlier was a good starting point, but it implicitly assumes a one-to-many relationship (one user -> many devices). To handle the many-to-many reality, we need to introduce a more sophisticated model centered around a **Session** or **Link** entity.

Let's call this our **"Session-Based Device Model"**.

---

### The Upgraded Data Model

Instead of one table, we'll use two primary tables and a junction (or link) table to manage these relationships correctly.

1. **`Devices` Table:** This table stores information about the physical hardware. It is user-agnostic.
    * `Id` (Primary Key, e.g., `Guid`)
    * `DeviceId` (The stable, unique identifier from the OS, e.g., `string`)
    * `Platform` (e.g., "Android", "iOS")
    * `ModelName` (Optional, for analytics)
    * `CreatedAt`, `UpdatedAt`

2. **`UserDeviceSessions` Table (The Junction Table):** This is the heart of the solution. It links a `User` to a `Device` for a specific period of activity and holds the FCM token for that session.
    * `Id` (Primary Key, e.g., `Guid`)
    * `UserId` (Foreign Key to the `User` aggregate)
    * `DeviceId` (Foreign Key to our new `Devices` table)
    * `FcmToken` (The active FCM token for this specific session)
    * `IsActive` (Crucial flag: `true` if this is a current, valid session for push notifications)
    * `LastLoginAt` (Timestamp for when this session became active)
    * `LoggedOutAt` (Timestamp for when the user explicitly logged out)

Now, let's see how this new model handles all the cases you brought up.

---

### How We Handle the Scenarios

#### Scenario 1: User A logs into a new device (Phone 1)

1. **Flutter App:** On successful login, the app gets the stable `DeviceId` from the OS and a new `FcmToken` from Firebase.
2. **API Call:** The app sends this information to the backend: `POST /api/sessions/login` with `{ "deviceId": "...", "fcmToken": "..." }`. The `UserId` is taken from the authentication token.
3. **Backend Logic:**
    * Find or create a record in the `Devices` table for the given `DeviceId`. Let's say its ID is `Device_P1_Id`.
    * Create a **new record** in the `UserDeviceSessions` table:
        * `UserId`: `User_A_Id`
        * `DeviceId`: `Device_P1_Id`
        * `FcmToken`: The new token from the app.
        * `IsActive`: `true`
        * `LastLoginAt`: `DateTime.UtcNow`
        * `LoggedOutAt`: `null`

**Result:** User A is now associated with Phone 1 and will receive notifications sent to them on that device.

#### Scenario 2: User A logs into a second device (Tablet 1)

The exact same logic as Scenario 1 applies. A new `UserDeviceSessions` record is created linking `User_A_Id` to the new `Device_T1_Id`.

**Result:** User A is now active on two devices. When you send a notification to User A, your query will find **two** active sessions and send the notification to both FCM tokens.

#### Scenario 3: User B logs into the *same* device (Phone 1)

This is the critical "shared device" case that the previous model handled poorly.

1. **Flutter App:** User A logs out, User B logs in. The app gets the *same* stable `DeviceId` but potentially a new (or the same) `FcmToken`.
2. **API Call:** `POST /api/sessions/login` with the device info. The `UserId` is now `User_B_Id`.
3. **Backend Logic:** This is the most important part.
    * **Step 1: Deactivate Previous Sessions.** Before creating a new session, you must enforce a business rule: **"Only one user can be active on a single device at any given time."**
        * Execute a query: `UPDATE UserDeviceSessions SET IsActive = false, LoggedOutAt = UtcNow WHERE DeviceId = 'Device_P1_Id' AND IsActive = true`.
        * This finds User A's active session on Phone 1 and deactivates it.
    * **Step 2: Create the New Session.** Now, proceed as in Scenario 1 to create a new, active `UserDeviceSessions` record for User B.
        * `UserId`: `User_B_Id`
        * `DeviceId`: `Device_P1_Id`
        * `FcmToken`: The new token.
        * `IsActive`: `true`

**Result:** User A's session on Phone 1 is now inactive. They will no longer receive their personal notifications on that device. User B's session is active, and they will receive theirs. The problem is solved correctly.

#### Scenario 4: Handling Invalid FCM Tokens

Your `IsActive` flag still serves its purpose for handling tokens that are reported as invalid by FCM.

1. **Backend Logic:** When `FcmService` sends a notification and gets an `Unregistered` error for a specific `FcmToken`:
2. **Action:** It finds the corresponding record in `UserDeviceSessions` and sets `IsActive = false`.
    * `UPDATE UserDeviceSessions SET IsActive = false WHERE FcmToken = 'the_dead_token'`.

This ensures you don't waste resources sending to dead tokens, without losing the historical record of that session. The next time the user opens the app on that device, a new login event will create a new active session with a fresh token.

### How Notification Sending Logic Changes

Your query to get tokens for a user becomes much more precise and correct:

```csharp
// In your Application layer handler (e.g., SendNotificationToUserCommandHandler)

// This query correctly gets all active tokens for a user across all their devices,
// and ignores any inactive sessions on shared devices.
var activeFcmTokens = await _dbContext.UserDeviceSessions
    .Where(s => s.UserId == command.UserId && s.IsActive)
    .Select(s => s.FcmToken)
    .ToListAsync();

// ... then send to this list of tokens.
```

### Summary of the Session-Based Model

| Feature | How it's Handled |
| :--- | :--- |
| **User on Multiple Devices** | Multiple `UserDeviceSessions` records exist with the same `UserId` but different `DeviceId`s. |
| **Device with Multiple Users** | Multiple `UserDeviceSessions` records exist with the same `DeviceId` but different `UserId`s. The `IsActive` flag ensures only one is active at a time. |
| **FCM Token Refresh/Invalidation** | The `IsActive` flag is set to `false`. A new active session is created on the next login. |
| **User Logout** | The corresponding `UserDeviceSessions` record has its `IsActive` flag set to `false` and `LoggedOutAt` timestamped. |
| **Security & Auditing** | The `UserDeviceSessions` table provides a complete history of which user logged into which device and when. This is great for features like "Manage your logged-in devices." |

This model is more complex to set up initially, but it is the **correct and robust way** to handle the real-world dynamics of users and devices, ensuring notifications are always sent to the right person on the right device.

---

**Phase 1: Data Model and Persistence**

1. **Create `Device` Entity:**
    * Create a new class `Device.cs` in `src/Infrastructure/Data/`.
    * This class will represent the `Devices` table and include properties for `Id` (Guid), `DeviceId` (string, unique OS identifier), `Platform` (string), `ModelName` (string?), `CreatedAt`, and `UpdatedAt`.
    * This entity will *not* be a Domain entity as it represents an infrastructure concern (physical device details).
2. **Create `UserDeviceSession` Entity:**
    * Create a new class `UserDeviceSession.cs` in `src/Infrastructure/Data/`.
    * This class will represent the `UserDeviceSessions` junction table.
    * Include properties for `Id` (Guid), `UserId` (Guid, Foreign Key to Identity User), `DeviceId` (Guid, Foreign Key to `Device`), `FcmToken` (string), `IsActive` (bool), `LastLoginAt` (DateTime), and `LoggedOutAt` (DateTime?).
    * Define the relationships: `UserDeviceSession` will have navigation properties to `ApplicationUser` (or the appropriate Identity user entity) and `Device`.
    * This entity will also be in the Infrastructure layer.
3. **Update `ApplicationDbContext`:**
    * Modify `src/Infrastructure/Data/ApplicationDbContext.cs` to add `DbSet<Device>` and `DbSet<UserDeviceSession>` properties.
    * Configure the relationships and table mappings in the `OnModelCreating` method, including the foreign keys and the index on `DeviceId` and `IsActive` for efficient querying during login (as described in the user's scenario 3).
4. **Create Repositories:**
    * Define new interfaces in `src/Application/Common/Interfaces/` for interacting with the new entities, e.g., `IDeviceRepository` and `IUserDeviceSessionRepository`.
    * Implement these interfaces in `src/Infrastructure/Data/`, e.g., `DeviceRepository.cs` and `UserDeviceSessionRepository.cs`, using Entity Framework Core.
    * The `IUserDeviceSessionRepository` will need methods like:
        * `AddSessionAsync(UserDeviceSession session)`
        * `DeactivateSessionsForDeviceAsync(Guid deviceId)`
        * `GetActiveSessionsByUserIdAsync(Guid userId)`
        * `MarkTokenAsInvalidAsync(string fcmToken)` (This might be better handled by querying the session by token and setting `IsActive = false`).
    * The existing `IUserDeviceRepository` and its implementation will be removed or refactored if any of its remaining functionality is still needed (though it seems the new model replaces its core purpose).
5. **Database Migrations:**
    * Generate a new database migration to create the `Devices` and `UserDeviceSessions` tables and remove the old `UserDevices` table.

**Phase 2: Application Logic Updates**

6. **Update Login/Session Creation Logic:**
    * Identify or create the appropriate command/handler in the Application layer that handles user login and device registration. Based on the user's description, this would be a new endpoint/handler, e.g., `LoginCommandHandler` or `RegisterDeviceCommandHandler`.
    * This handler will receive `DeviceId` and `FcmToken` from the client, along with the `UserId` from the authenticated context.
    * Implement the logic described in the user's scenarios 1, 2, and 3:
        * Find or create the `Device` record using `IDeviceRepository`.
        * **Crucially:** Deactivate any existing active sessions for the given `DeviceId` using `IUserDeviceSessionRepository.DeactivateSessionsForDeviceAsync`.
        * Create a new `UserDeviceSession` record using `IUserDeviceSessionRepository.AddSessionAsync`.
7. **Update Notification Sending Logic:**
    * Modify `src/Application/Notifications/Commands/SendNotificationToUser/SendNotificationToUserCommandHandler.cs`.
    * Replace the call to `_userDeviceRepository.GetActiveFcmTokensByUserIdAsync` with a call to the new `IUserDeviceSessionRepository.GetActiveSessionsByUserIdAsync`.
    * The query should select the `FcmToken` from the active sessions for the given `UserId`.
8. **Update FCM Service:**
    * Modify `src/Infrastructure/Notifications/FcmService.cs`.
    * Update the `MarkTokenAsInvalidAsync` logic (and potentially the method signature) to interact with the `UserDeviceSessions` table via the new `IUserDeviceSessionRepository`. Instead of just marking a token, it should find the specific session with that token and set its `IsActive` flag to `false`.

**Phase 3: Cleanup and Testing**

9. **Remove Old Code:**
    * Remove the `UserDevice.cs` entity.
    * Remove the `IUserDeviceRepository` interface and its implementation.
    * Remove any references to the old `UserDevice` model or repository in other parts of the codebase (e.g., `ApplicationDbContextInitialiser`, Dependency Injection configurations).
10. **Update Database Seeding:**
    * Modify `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs` to seed the new `Devices` and `UserDeviceSessions` tables instead of the old `UserDevices` table. This will involve creating `Device` records and then `UserDeviceSession` records linking users to those devices.
11. **Write Tests:**
    * Write unit tests for the new repository methods in `tests/Infrastructure.IntegrationTests/`.
    * Write unit tests for the updated command handlers in `tests/Application.UnitTests/`.
    * Write functional tests in `tests/Application.FunctionalTests/` to verify the login/session creation flow and the notification sending flow with the new model, covering the scenarios described by the user (multiple devices for one user, multiple users on one device).

This plan addresses the data model changes, the logic updates for session management and notification sending, and includes necessary cleanup and testing steps.
