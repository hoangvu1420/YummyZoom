using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Common;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Commands.AcceptOrder;

public sealed class AcceptOrderCommandHandler : IRequestHandler<AcceptOrderCommand, Result<OrderLifecycleResultDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;
    private readonly ILogger<AcceptOrderCommandHandler> _logger;

    public AcceptOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
    IUser currentUser,
        ILogger<AcceptOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<OrderLifecycleResultDto>> Handle(AcceptOrderCommand request, CancellationToken cancellationToken)
    {
        // Transaction wrapper
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1. Auth baseline
            if (_currentUser.DomainUserId is null)
            {
                _logger.LogWarning("Unauthenticated user attempting to accept order {OrderId}", request.OrderId);
                throw new UnauthorizedAccessException();
            }

            // 2. Load order
            var orderId = OrderId.Create(request.OrderId);
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                return Result.Failure<OrderLifecycleResultDto>(AcceptOrderErrors.NotFound);
            }

            // 3. Restaurant consistency
            if (order.RestaurantId.Value != request.RestaurantGuid)
            {
                _logger.LogWarning("Restaurant mismatch for order {OrderId}. Provided {ProvidedRestaurantId} actual {ActualRestaurantId}", order.Id.Value, request.RestaurantGuid, order.RestaurantId.Value);
                throw new ForbiddenAccessException();
            }

            // 4. Domain transition (idempotent if already accepted)
            var acceptResult = order.Accept(request.EstimatedDeliveryTime);
            if (acceptResult.IsFailure)
            {
                if (order.Status == OrderStatus.Accepted)
                {
                    _logger.LogInformation("Order {OrderId} already accepted; returning current state.", order.Id.Value);
                    return Result.Success(order.ToLifecycleDto());
                }
                _logger.LogWarning("Failed to accept order {OrderId}: {Error}", order.Id.Value, acceptResult.Error.Description);
                return Result.Failure<OrderLifecycleResultDto>(acceptResult.Error);
            }

            // 5. Persist + return
            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderId} accepted with ETA {Eta}", order.Id.Value, order.EstimatedDeliveryTime);
            return Result.Success(order.ToLifecycleDto());
        }, cancellationToken);
    }
}

public static class AcceptOrderErrors
{
    public static Error NotFound => Error.NotFound(
        "AcceptOrder.NotFound", "The specified order was not found.");

    public static Error AlreadyAccepted => Error.Conflict(
        "AcceptOrder.AlreadyAccepted", "The order has already been accepted.");
}
