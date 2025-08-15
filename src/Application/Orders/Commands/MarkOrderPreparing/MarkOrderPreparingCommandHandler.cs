using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Common;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Commands.MarkOrderPreparing;

public sealed class MarkOrderPreparingCommandHandler : IRequestHandler<MarkOrderPreparingCommand, Result<OrderLifecycleResultDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;
    private readonly ILogger<MarkOrderPreparingCommandHandler> _logger;

    public MarkOrderPreparingCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser,
        ILogger<MarkOrderPreparingCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<OrderLifecycleResultDto>> Handle(MarkOrderPreparingCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1. Auth baseline
            if (_currentUser.DomainUserId is null)
            {
                _logger.LogWarning("Unauthenticated user attempting to mark order {OrderId} as preparing", request.OrderId);
                throw new UnauthorizedAccessException();
            }

            // 2. Load order
            var orderId = OrderId.Create(request.OrderId);
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                return Result.Failure<OrderLifecycleResultDto>(MarkOrderPreparingErrors.NotFound);
            }

            // 3. Restaurant consistency
            if (order.RestaurantId.Value != request.RestaurantGuid)
            {
                _logger.LogWarning("Restaurant mismatch for order {OrderId}. Provided {ProvidedRestaurantId} actual {ActualRestaurantId}", order.Id.Value, request.RestaurantGuid, order.RestaurantId.Value);
                throw new ForbiddenAccessException();
            }

            // 4. Idempotency check
            if (order.Status == OrderStatus.Preparing)
            {
                _logger.LogInformation("Order {OrderId} already in Preparing status.", order.Id.Value);
                return Result.Success(order.ToLifecycleDto());
            }

            // 5. Domain transition
            var markResult = order.MarkAsPreparing();
            if (markResult.IsFailure)
            {
                _logger.LogWarning("Failed to mark order {OrderId} as preparing: {Error}", order.Id.Value, markResult.Error.Description);
                return Result.Failure<OrderLifecycleResultDto>(markResult.Error);
            }

            // 6. Persist + return
            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderId} marked as Preparing", order.Id.Value);
            return Result.Success(order.ToLifecycleDto());
        }, cancellationToken);
    }
}

public static class MarkOrderPreparingErrors
{
    public static Error NotFound => Error.NotFound(
        "MarkOrderPreparing.NotFound", "The specified order was not found.");
}
