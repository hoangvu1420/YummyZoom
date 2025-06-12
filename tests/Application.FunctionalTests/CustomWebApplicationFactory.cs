using System.Data.Common;
using YummyZoom.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.FunctionalTests;

using static Testing;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection;
    private readonly string _connectionString;

    public CustomWebApplicationFactory(DbConnection connection, string connectionString)
    {
        _connection = connection;
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:YummyZoomDb", _connectionString);
        builder.ConfigureTestServices(services =>
        {
            services
                .RemoveAll<IUser>()
                .AddTransient(provider =>
                {
                    var userIdGuid = GetUserId(); 
                    var mock = new Mock<IUser>();
                    mock.Setup(s => s.Id).Returns(userIdGuid?.ToString());
                    if (userIdGuid.HasValue)
                    {
                        mock.Setup(s => s.DomainId).Returns(UserId.Create(userIdGuid.Value));
                    }
                    return mock.Object;
                });

            // Mock IFcmService for testing
            services
                .RemoveAll<IFcmService>()
                .AddTransient(provider =>
                {
                    var mock = new Mock<IFcmService>();
                    
                    // Mock successful multicast notification sending
                    mock.Setup(s => s.SendMulticastNotificationAsync(
                            It.IsAny<IEnumerable<string>>(), 
                            It.IsAny<string>(), 
                            It.IsAny<string>(), 
                            It.IsAny<Dictionary<string, string>>()))
                        .ReturnsAsync(Result.Success<List<string>>(new List<string>()));
                    
                    // Mock successful single notification sending
                    mock.Setup(s => s.SendNotificationAsync(
                            It.IsAny<string>(), 
                            It.IsAny<string>(), 
                            It.IsAny<string>(), 
                            It.IsAny<Dictionary<string, string>>()))
                        .ReturnsAsync(Result.Success());
                        
                    // Mock successful data message sending
                    mock.Setup(s => s.SendDataMessageAsync(
                            It.IsAny<string>(), 
                            It.IsAny<Dictionary<string, string>>()))
                        .ReturnsAsync(Result.Success());
                            
                    return mock.Object;
                });
        });
    }
}
