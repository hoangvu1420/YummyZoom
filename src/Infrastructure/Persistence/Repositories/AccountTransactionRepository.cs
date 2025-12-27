using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.AccountTransactionEntity;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class AccountTransactionRepository : IAccountTransactionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AccountTransactionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(AccountTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.AccountTransactions.AddAsync(transaction, cancellationToken);
    }
}
