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
        // ==> TRACING BLOCK <==
        Console.WriteLine("\n--- [Repository] OrderRepository.AddAsync ---");
        Console.WriteLine($"Attempting to add Order with ID: {order.Id.Value}");
        Console.WriteLine($"  - Order Status: {order.Status}");
        Console.WriteLine($"  - Number of OrderItems: {order.OrderItems.Count}");
        foreach (var item in order.OrderItems)
        {
            Console.WriteLine($"    -> Item ID: {item.Id.Value} | Name: {item.Snapshot_ItemName} | Quantity: {item.Quantity}");
        }
        Console.WriteLine($"  - Number of PaymentTransactions: {order.PaymentTransactions.Count}");
        foreach (var pt in order.PaymentTransactions)
        {
            Console.WriteLine($"    -> Transaction ID: {pt.Id.Value} | Status: {pt.Status} | RefID: {pt.PaymentGatewayReferenceId ?? "N/A"} | Amount: {pt.Amount.Amount} {pt.Amount.Currency}");
        }
        Console.WriteLine("   - Total Amount: " + order.TotalAmount.Amount + " " + order.TotalAmount.Currency);
        
        Console.WriteLine("--- Handing over to EF Core DbContext... ---\n");
        // ==> END OF TRACING BLOCK <==

        try
        {
            await _dbContext.Orders.AddAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"!!!!!! EF CORE EXCEPTION in AddAsync !!!!!!");
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    public async Task<Order?> GetByIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<Order?> GetByPaymentGatewayReferenceIdAsync(string paymentGatewayReferenceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
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
