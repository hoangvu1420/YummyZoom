using Dapper;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public OrderRepository(ApplicationDbContext dbContext, IDbConnectionFactory dbConnectionFactory)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsSplitQuery()
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<Order?> GetByPaymentGatewayReferenceIdAsync(string paymentGatewayReferenceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsSplitQuery()
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.PaymentTransactions.Any(pt => pt.PaymentGatewayReferenceId == paymentGatewayReferenceId), cancellationToken);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _dbContext.Orders.Update(order);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Guid>> GetActiveOrderIdsForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT o."Id"
            FROM "Orders" o
            WHERE o."CustomerId" = @CustomerId
              AND o."Status" = ANY(@ActiveStatuses)
            ORDER BY o."PlacementTimestamp" DESC
            """;

        var orderIds = await connection.QueryAsync<Guid>(
            new CommandDefinition(sql,
                new { CustomerId = customerId, ActiveStatuses = OrderQueryConstants.ActiveStatuses },
                cancellationToken: cancellationToken));

        return orderIds.ToList();
    }
}
