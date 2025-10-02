using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(OrderId orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetByPaymentGatewayReferenceIdAsync(string paymentGatewayReferenceId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the IDs of all active orders for a specific customer.
    /// Active orders are those in non-terminal states (Placed, Accepted, Preparing, ReadyForDelivery, OutForDelivery).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveOrderIdsForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
