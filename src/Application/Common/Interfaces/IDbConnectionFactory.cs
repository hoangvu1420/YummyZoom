using System.Data;

namespace YummyZoom.Application.Common.Interfaces;

public interface IDbConnectionFactory : IDisposable
{
    IDbConnection CreateConnection();
}
