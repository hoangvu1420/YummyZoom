using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserAggregateRepository _userAggregateRepository;
    private readonly ApplicationDbContext _dbContext;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        IUserAggregateRepository userAggregateRepository,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _userAggregateRepository = userAggregateRepository;
        _dbContext = dbContext;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<Result<Guid>> CreateUserAsync(string email, string password, string name)
    {
        // Create the identity user with the provided email
        var identityUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
        };

        // Use a resilient execution strategy for database operations
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        
        try
        {
            // Execute the user creation within a transaction
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                
                try
                {
                    // Step 1: Create the identity user
                    var identityResult = await _userManager.CreateAsync(identityUser, password);
                    
                    if (!identityResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return HandleIdentityErrors(identityResult, email);
                    }

                    // Step 2: Create the domain user
                    Result<UserId> domainUserIdResult = UserId.Create(identityUser.Id);
                    if (domainUserIdResult.IsFailure) 
                    {
                        await transaction.RollbackAsync();
                        // Handle error if Identity user ID is not a valid GUID string
                        return Result.Failure<Guid>(domainUserIdResult.Error); 
                    }
                    var domainUserId = domainUserIdResult.Value;

                    // Create initial role assignment (e.g., Customer)
                    var initialRoleAssignmentResult = RoleAssignment.Create(Roles.Customer);
                    if (initialRoleAssignmentResult.IsFailure)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure<Guid>(initialRoleAssignmentResult.Error);
                    }
                    var initialRoleAssignment = initialRoleAssignmentResult.Value;

                    // Add the user to the Customer role in Identity system
                    var roleAssignResult = await _userManager.AddToRoleAsync(identityUser, initialRoleAssignment.RoleName);
                    if (!roleAssignResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                        return Result.Failure<Guid>(UserErrors.RegistrationFailed($"Failed to assign role: {errors}"));
                    }

                    // Create the domain user aggregate
                    var userAggregateResult = User.Create(
                        domainUserId,
                        name, 
                        email,
                        null, 
                        new List<RoleAssignment> { initialRoleAssignment }); 

                    if (userAggregateResult.IsFailure)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure<Guid>(userAggregateResult.Error);
                    }
                    var userAggregate = userAggregateResult.Value;

                    await _userAggregateRepository.AddAsync(userAggregate);
                    await _dbContext.SaveChangesAsync();

                    // Step 3: Commit transaction if everything succeeded
                    await transaction.CommitAsync();
                    return Result.Success(userAggregate.Id.Value); 
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Use the updated RegistrationFailed error
                    return Result.Failure<Guid>(UserErrors.RegistrationFailed(ex.Message));
                }
            });
        }
        catch (Exception ex)
        {
            // Use the updated RegistrationFailed error
            return Result.Failure<Guid>(UserErrors.RegistrationFailed(ex.Message));
        }
    }

    public async Task<Result<Guid>> CreateIdentityUserAsync(string email, string password, string roleName)
    {
        // Create the identity user with the provided email
        var identityUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
        };

        try
        {
            // Step 1: Create the identity user
            var identityResult = await _userManager.CreateAsync(identityUser, password);
            
            if (!identityResult.Succeeded)
            {
                return HandleIdentityErrors(identityResult, email);
            }

            // Step 2: Add user to the specified role in Identity system
            var roleAssignResult = await _userManager.AddToRoleAsync(identityUser, roleName);
            if (!roleAssignResult.Succeeded)
            {
                // If role assignment fails, delete the user to maintain consistency
                await _userManager.DeleteAsync(identityUser);
                
                var errors = string.Join(", ", roleAssignResult.Errors.Select(e => e.Description));
                return Result.Failure<Guid>(UserErrors.RegistrationFailed($"Failed to assign role: {errors}"));
            }

            // Return the user ID for creating the domain user
            return Result.Success(identityUser.Id);
        }
        catch (Exception ex)
        {
            // Clean up if an exception occurs
            if (identityUser.Id != Guid.Empty)
            {
                await _userManager.DeleteAsync(identityUser);
            }
            
            return Result.Failure<Guid>(UserErrors.RegistrationFailed(ex.Message));
        }
    }

    // Helper method to handle identity errors
    private static Result<Guid> HandleIdentityErrors(IdentityResult identityResult, string email)
    {
        // Check for duplicate email error
        if (identityResult.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            // Use the updated DuplicateEmail error
            return Result.Failure<Guid>(UserErrors.DuplicateEmail(email));
        }

        // General registration failure
        var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
        // Use the updated RegistrationFailed error
        return Result.Failure<Guid>(UserErrors.RegistrationFailed(errors));
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> AddUserToRoleAsync(Guid userId, string role)
    {
        var identityUser = await _userManager.FindByIdAsync(userId.ToString());
        if (identityUser == null)
        {
            return Result.Failure(UserErrors.UserNotFound(userId));
        }

        var result = await _userManager.AddToRoleAsync(identityUser, role);

        if (result.Succeeded)
        {
            return Result.Success();
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure(UserErrors.RoleAssignmentFailed($"Failed to add role '{role}': {errors}"));
    }

    public async Task<Result> RemoveUserFromRoleAsync(Guid userId, string role)
    {
        var identityUser = await _userManager.FindByIdAsync(userId.ToString());
        if (identityUser == null)
        {
            return Result.Failure(UserErrors.UserNotFound(userId));
        }

        var result = await _userManager.RemoveFromRoleAsync(identityUser, role);

        if (result.Succeeded)
        {
            return Result.Success();
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure(UserErrors.RoleRemovalFailed($"Failed to remove role '{role}': {errors}"));
    }

    public async Task<Result> UpdateEmailAsync(string userId, string newEmail)
    {
        // Use a resilient execution strategy for database operations
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        
        try
        {
            // Execute the email update within a transaction
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                
                try
                {
                    // Step 1: Find and update the Identity user
                    var identityUser = await _userManager.FindByIdAsync(userId);
                    if (identityUser == null)
                    {
                        return Result.Failure(UserErrors.InvalidUserId(userId));
                    }

                    // Check if the email is already in use
                    var existingUserWithEmail = await _userManager.FindByEmailAsync(newEmail);
                    if (existingUserWithEmail != null && existingUserWithEmail.Id.ToString() != userId)
                    {
                        return Result.Failure(UserErrors.DuplicateEmail(newEmail));
                    }

                    // Update email in Identity (note: this also updates normalized email)
                    var emailUpdateResult = await _userManager.SetEmailAsync(identityUser, newEmail);
                    if (!emailUpdateResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", emailUpdateResult.Errors.Select(e => e.Description));
                        return Result.Failure(UserErrors.EmailUpdateFailed(errors));
                    }

                    // Also update UserName since we're using email as the username
                    var usernameUpdateResult = await _userManager.SetUserNameAsync(identityUser, newEmail);
                    if (!usernameUpdateResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", usernameUpdateResult.Errors.Select(e => e.Description));
                        return Result.Failure(UserErrors.EmailUpdateFailed(errors));
                    }

                    // Step 2: Find and update the domain user
                    var domainUserId = UserId.Create(Guid.Parse(userId));
                    var domainUser = await _userAggregateRepository.GetByIdAsync(domainUserId);
                    
                    if (domainUser is null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(UserErrors.InvalidUserId(userId));
                    }

                    // Update email in domain model
                    domainUser.UpdateEmail(newEmail);
                    
                    // Save changes to domain user
                    await _userAggregateRepository.UpdateAsync(domainUser);
                    await _dbContext.SaveChangesAsync();

                    // Step 3: Commit transaction if everything succeeded
                    await transaction.CommitAsync();
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure(UserErrors.EmailUpdateFailed(ex.Message));
                }
            });
        }
        catch (Exception ex)
        {
            return Result.Failure(UserErrors.EmailUpdateFailed(ex.Message));
        }
    }

    public async Task<Result> UpdateProfileAsync(string userId, string name, string? phoneNumber)
    {
        // Use a resilient execution strategy for database operations
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        
        try
        {
            // Execute the profile update within a transaction
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                
                try
                {
                    // Step 1: Find and update the Identity user
                    var identityUser = await _userManager.FindByIdAsync(userId);
                    if (identityUser == null)
                    {
                        return Result.Failure(UserErrors.InvalidUserId(userId));
                    }

                    // Update phone number in Identity if provided (used for 2FA)
                    if (identityUser.PhoneNumber != phoneNumber)
                    {
                        var phoneUpdateResult = await _userManager.SetPhoneNumberAsync(identityUser, phoneNumber);
                        if (!phoneUpdateResult.Succeeded)
                        {
                            await transaction.RollbackAsync();
                            var errors = string.Join(", ", phoneUpdateResult.Errors.Select(e => e.Description));
                            return Result.Failure(UserErrors.ProfileUpdateFailed(errors));
                        }
                    }

                    // Step 2: Find and update the domain user
                    var domainUserId = UserId.Create(Guid.Parse(userId));
                    var domainUser = await _userAggregateRepository.GetByIdAsync(domainUserId);
                    
                    if (domainUser is null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(UserErrors.InvalidUserId(userId));
                    }

                    // Update name and phone in domain model
                    domainUser.UpdateProfile(name, phoneNumber);
                    
                    // Save changes to domain user
                    await _userAggregateRepository.UpdateAsync(domainUser);
                    await _dbContext.SaveChangesAsync();

                    // Step 3: Commit transaction if everything succeeded
                    await transaction.CommitAsync();
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure(UserErrors.ProfileUpdateFailed(ex.Message));
                }
            });
        }
        catch (Exception ex)
        {
            return Result.Failure(UserErrors.ProfileUpdateFailed(ex.Message));
        }
    }

    private async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            return Result.Success();
        }
        
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Failure(UserErrors.DeletionFailed(errors));
    }
}
