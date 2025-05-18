using System.Data.Common;
using YummyZoom.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YummyZoom.Domain.UserAggregate.ValueObjects;

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
        });
    }
}
