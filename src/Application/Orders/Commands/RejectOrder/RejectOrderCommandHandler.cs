using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Common;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Commands.RejectOrder;

public sealed class RejectOrderCommandHandler : IRequestHandler<RejectOrderCommand, Result<OrderLifecycleResultDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;
    private readonly ILogger<RejectOrderCommandHandler> _logger;

    public RejectOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser,
        ILogger<RejectOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<OrderLifecycleResultDto>> Handle(RejectOrderCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1. Auth baseline
            if (_currentUser.DomainUserId is null)
            {
                _logger.LogWarning("Unauthenticated user attempting to reject order {OrderId}", request.OrderId);
                throw new UnauthorizedAccessException();
            }

            // 2. Load order
            var orderId = OrderId.Create(request.OrderId);
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                return Result.Failure<OrderLifecycleResultDto>(RejectOrderErrors.NotFound);
            }

            // 3. Restaurant consistency
            if (order.RestaurantId.Value != request.RestaurantGuid)
            {
                _logger.LogWarning("Restaurant mismatch for order {OrderId}. Provided {ProvidedRestaurantId} actual {ActualRestaurantId}", order.Id.Value, request.RestaurantGuid, order.RestaurantId.Value);
                throw new ForbiddenAccessException();
            }

            // 4. Domain transition (idempotent if already rejected)
            var rejectResult = order.Reject();
            if (rejectResult.IsFailure)
            {
                if (order.Status == OrderStatus.Rejected)
                {
                    _logger.LogInformation("Order {OrderId} already rejected; returning current state.", order.Id.Value);
                    return Result.Success(order.ToLifecycleDto());
                }
                _logger.LogWarning("Failed to reject order {OrderId}: {Error}", order.Id.Value, rejectResult.Error.Description);
                return Result.Failure<OrderLifecycleResultDto>(rejectResult.Error);
            }

            // 5. Persist + log + return
            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderId} rejected. Reason length: {ReasonLength}", order.Id.Value, request.RejectionReason?.Length ?? 0);
            return Result.Success(order.ToLifecycleDto());
        }, cancellationToken);
    }
}

public static class RejectOrderErrors
{
    public static Error NotFound => Error.NotFound(
        "RejectOrder.NotFound", "The specified order was not found.");
}
