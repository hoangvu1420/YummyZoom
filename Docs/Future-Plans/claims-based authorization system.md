
### The Implementation Strategy: Using Claims as Permissions

We will translate a user's `RoleAssignment`s into specific, granular claims at login. These claims will then be used by ASP.NET Core's policy-based authorization system to protect our MediatR commands.

Here is the step-by-step guide:

#### Step 1: Generate Custom Claims at Login

We need to hook into the login process to add claims based on the user's `RoleAssignment`s. The perfect place for this is a custom `IUserClaimsPrincipalFactory`.

1. **Create a Custom Claims Factory:**

```csharp
// In your Infrastructure or Application layer
public class YummyZoomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;

    public YummyZoomClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,  
        IRoleAssignmentRepository roleAssignmentRepository) 
        : base(userManager, roleManager, optionsAccessor)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        
        // Convert the Identity User's string ID to our domain's UserId
        var domainUserId = UserId.Create(Guid.Parse(user.Id)); // Assuming GUIDs

        // Fetch all business roles for this user from our domain
        var roleAssignments = await _roleAssignmentRepository.GetByUserIdAsync(domainUserId);

        foreach (var assignment in roleAssignments)
        {
            // Create a specific, contextual claim for each role
            // Format: "role:restaurant_id" -> e.g., "Owner:a1b2c3d4..."
            var permissionClaim = new Claim("permission", $"{assignment.Role}:{assignment.RestaurantId.Value}");
            identity.AddClaim(permissionClaim);
        }

        return identity;
    }
}
```

2. **Register the Factory in `Program.cs` / `Startup.cs`:**

```csharp
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, YummyZoomClaimsPrincipalFactory>();
```

Now, when a user logs in, their authentication cookie (or JWT) will contain claims like:
`permission: Owner:a1b2c3d4-e5f6...`
`permission: Staff:f7e6d5c4-b3a2...`

#### Step 2: Define a Custom Authorization Requirement and Handler

Next, we create a policy that can understand these new claims.

1. **Create the Requirement:** This class is just a marker that defines a specific authorization need.

```csharp
// In your Application layer (Authorization folder)
public class HasPermissionRequirement : IAuthorizationRequirement
{
    public string Role { get; }
    
    public HasPermissionRequirement(string role)
    {
        Role = role;
    }
}
```

2. **Create the Handler:** This is where the magic happens. The handler gets the user's claims and the resource they are trying to access (our MediatR command).

```csharp
// In your Infrastructure or Application layer
public class PermissionAuthorizationHandler : AuthorizationHandler<HasPermissionRequirement, IRestaurantCommand>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement,
        IRestaurantCommand resource) // The command itself is the resource
    {
        // Construct the claim we are looking for.
        // e.g., "Owner:a1b2c3d4..."
        var requiredPermission = $"{requirement.Role}:{resource.RestaurantId.Value}";

        if (context.User.HasClaim("permission", requiredPermission))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(); // Implicitly fails if Succeed is not called
        }
        
        return Task.CompletedTask;
    }
}

// A marker interface for our commands to easily grab the RestaurantId
public interface IRestaurantCommand
{
    public RestaurantId RestaurantId { get; }
}
```

3. **Register the Handler and Define Policies in `Program.cs` / `Startup.cs`:**

```csharp
// Register the handler
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Define policies that use the requirement
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeRestaurantOwner", policy =>
        policy.AddRequirements(new HasPermissionRequirement("Owner")));
        
    options.AddPolicy("MustBeRestaurantStaff", policy =>
        policy.AddRequirements(new HasPermissionRequirement("Staff")));
});
```

#### Step 3: Apply the Policy to MediatR Commands

Now, you can protect your commands declaratively using your existing `AuthorizeAttribute`.

```csharp
// Example: A command to update a menu
[Authorize(Policy = "MustBeRestaurantOwner")]
public record UpdateMenuCommand(
    MenuId MenuId, 
    RestaurantId RestaurantId, // Implementing IRestaurantCommand
    string Name, 
    string Description) : IRequest<Result<Menu>>, IRestaurantCommand;
```

### The Complete Workflow

1. **Login:** A user logs in. `YummyZoomClaimsPrincipalFactory` is called. It fetches the user's `RoleAssignment`s and adds `permission` claims to their identity.
2. **Request:** The user's browser sends a request to an endpoint that dispatches the `UpdateMenuCommand`. The request includes the authentication cookie/JWT.
3. **MediatR Pipeline:** Your `AuthorizationBehavior` pipeline step intercepts the `UpdateMenuCommand`.
4. **Authorization Check:**
    * The behavior sees the `[Authorize(Policy = "MustBeRestaurantOwner")]` attribute.
    * It calls ASP.NET Core's `IAuthorizationService`, asking: "Can the current user satisfy the 'MustBeRestaurantOwner' policy for this `UpdateMenuCommand` resource?"
    * The authorization service finds our `PermissionAuthorizationHandler`.
    * The handler executes its logic:
        * It gets the `RestaurantId` from the command object.
        * It constructs the required claim string: `"Owner:a1b2c3d4..."`.
        * It checks if the logged-in user's claims list contains this exact string.
        * If yes, the user is authorized, and the pipeline continues to the command handler.
        * If no, the pipeline is short-circuited, and an `Forbidden` (403) response is returned.

This approach is powerful, secure, and perfectly aligned with both DDD and modern ASP.NET Core practices. It keeps your domain pure while fully leveraging the robust security infrastructure of the framework.
