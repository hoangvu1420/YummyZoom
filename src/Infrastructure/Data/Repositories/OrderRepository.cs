using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OrderRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
} 
