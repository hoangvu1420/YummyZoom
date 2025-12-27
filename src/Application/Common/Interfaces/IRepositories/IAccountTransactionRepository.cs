using YummyZoom.Domain.AccountTransactionEntity;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IAccountTransactionRepository
{
    Task AddAsync(AccountTransaction transaction, CancellationToken cancellationToken = default);
}
