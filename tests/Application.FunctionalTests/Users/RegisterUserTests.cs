using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors; 
using YummyZoom.Infrastructure.Identity; 
using YummyZoom.Application.Users.Commands.RegisterUser; 
using YummyZoom.SharedKernel.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace YummyZoom.Application.FunctionalTests.Users;

using static Testing;

public class RegisterUserTests : BaseTestFixture
{
    [SetUp] 
    public async Task TestSetup()
    {
        await SetupForUserRegistrationTestsAsync();
    }
    
    [Test]
    public async Task RegisterUser_WithValidData_ShouldSucceedAndCreateBothUsers()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = "Reg User",
            Email = "register.test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var returnedUserId = result.Value;
        returnedUserId.Should().NotBeEmpty(); 

        // Verify ApplicationUser created
        var appUser = await FindAsync<ApplicationUser>(returnedUserId);
        appUser.Should().NotBeNull();
        appUser!.UserName.Should().Be(command.Email); // Username is always email
        appUser.Email.Should().Be(command.Email);
        appUser.Id.Should().Be(returnedUserId); // Verify the ID matches

        // Verify Domain UserAggregate created
        var domainUserId = UserId.Create(returnedUserId);
        var domainUser = await FindAsync<User>(domainUserId); 
        domainUser.Should().NotBeNull();
        domainUser!.Id.Should().Be(domainUserId);
        // Assert the Name property
        domainUser.Name.Should().Be(command.Name);
        domainUser.Email.Should().Be(command.Email);
        // Assuming AuditableEntityInterceptor uses IUser.Id (string representation of Guid)
        domainUser.CreatedBy.Should().Be(returnedUserId.ToString());
    }

    [Test]
    public async Task RegisterUser_ShouldAssignCustomerRole()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = "Role Test User",
            Email = "role.test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var returnedUserId = result.Value;

        // Get the created user
        var appUser = await FindAsync<ApplicationUser>(returnedUserId);
        appUser.Should().NotBeNull();

        // Verify the user is assigned to the Customer role
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var isInRole = await userManager.IsInRoleAsync(appUser!, Roles.Customer);
        isInRole.Should().BeTrue("User should be assigned to the Customer role");
    }

    [Test]
    public async Task RegisterUser_WithDuplicateEmail_ShouldReturnFailureResult()
    {
        // Arrange
        var command1 = new RegisterUserCommand 
        {
            Name = "Dupe Email",
            Email = "dupe.email@example.com", // Duplicate Email
            Password = "Password123!"
        };
        var result1 = await SendAsync(command1);
        result1.ShouldBeSuccessful();

        var command2 = new RegisterUserCommand 
        {
            Name = "Another Person",
            Email = "dupe.email@example.com", // Duplicate Email
            Password = "Password456!"
        };

        // Act
        var result2 = await SendAsync(command2);

        // Assert
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be(UserErrors.DuplicateEmail(command2.Email).Code);
    }


    [Test]
    public async Task RegisterUser_WithMissingFields_ShouldFailValidation()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = null, // Testing missing Name, a required field
            Email = "missing.fields@example.com",
            Password = "Password123!"
        };

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        // The ValidationBehaviour should catch this before the handler
        await act.Should().ThrowAsync<ValidationException>();
    }
    
    [Test]
    public async Task RegisterUser_WithInvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = "Invalid Email",
            Email = "invalid-email", // Invalid format
            Password = "Password123!"
        };

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
    
     [Test]
    public async Task RegisterUser_WithShortPassword_ShouldFailValidation()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Name = "Short Password",
            Email = "short.pw@example.com", 
            Password = "123" // Too short
        };

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
