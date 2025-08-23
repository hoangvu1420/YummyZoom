using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Commands.Common;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<OrderLifecycleResultDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<OrderLifecycleResultDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        // Transaction wrapper for consistency & outbox
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1. Auth baseline (must be signed in)
            if (_currentUser.DomainUserId is null)
            {
                _logger.LogWarning("Unauthenticated user attempting to cancel order {OrderId}", request.OrderId);
                throw new UnauthorizedAccessException();
            }

            // 2. Load + basic existence / restaurant consistency
            var orderId = OrderId.Create(request.OrderId);
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                return Result.Failure<OrderLifecycleResultDto>(CancelOrderErrors.NotFound);
            }
            if (order.RestaurantId.Value != request.RestaurantGuid)
            {
                _logger.LogWarning("Restaurant mismatch for order {OrderId}. Provided {ProvidedRestaurantId} actual {ActualRestaurantId}", order.Id.Value, request.RestaurantGuid, order.RestaurantId.Value);
                throw new ForbiddenAccessException();
            }

            // 3. Determine actor (explicit acting user optional for staff tooling)
            var actingUserId = request.ActingUserDomainId ?? _currentUser.DomainUserId;
            var principal = _currentUser.Principal!;
            var isCustomerActor = actingUserId == order.CustomerId;

            // 4. Permission classification (restaurant staff/owner or admin)
            var restaurantIdString = order.RestaurantId.Value.ToString();
            bool hasRestaurantStaffPermission = principal.HasClaim("permission", $"{Roles.RestaurantStaff}:{restaurantIdString}")
                                                || principal.HasClaim("permission", $"{Roles.RestaurantOwner}:{restaurantIdString}");
            bool isAdmin = principal.IsInRole(Roles.Administrator) || principal.HasClaim("permission", $"{Roles.UserAdmin}:*");

            // 5. Authorization matrix evaluation
            if (!isCustomerActor && !hasRestaurantStaffPermission && !isAdmin)
            {
                _logger.LogWarning("User {UserId} lacks permission to cancel order {OrderId}", actingUserId!.Value, order.Id.Value);
                throw new ForbiddenAccessException();
            }

            // 6. Customer-specific status restriction
            if (isCustomerActor && order.Status is not (OrderStatus.Placed or OrderStatus.Accepted))
            {
                _logger.LogWarning("Customer {UserId} cannot cancel order {OrderId} in status {Status}", actingUserId!.Value, order.Id.Value, order.Status);
                return Result.Failure<OrderLifecycleResultDto>(CancelOrderErrors.InvalidStatusForCustomer);
            }

            // 7. Domain transition (idempotent handling if already cancelled)
            var cancelResult = order.Cancel();
            if (cancelResult.IsFailure)
            {
                if (order.Status == OrderStatus.Cancelled)
                {
                    _logger.LogInformation("Order {OrderId} already cancelled; returning current state.", order.Id.Value);
                    return Result.Success(order.ToLifecycleDto());
                }
                _logger.LogWarning("Failed to cancel order {OrderId}: {Error}", order.Id.Value, cancelResult.Error.Description);
                return Result.Failure<OrderLifecycleResultDto>(cancelResult.Error);
            }

            // 8. Persist & map
            await _orderRepository.UpdateAsync(order, cancellationToken);
            var actorType = isCustomerActor ? "Customer" : isAdmin ? "Admin" : "RestaurantStaff";
            _logger.LogInformation("Order {OrderId} cancelled by {ActorType} {ActorId}. Reason length: {ReasonLength}", order.Id.Value, actorType, actingUserId!.Value, request.Reason?.Length ?? 0);

            // 9. Return lifecycle DTO
            return Result.Success(order.ToLifecycleDto());
        }, cancellationToken);
    }
}

public static class CancelOrderErrors
{
    public static Error NotFound => Error.NotFound(
        "CancelOrder.NotFound", "The specified order was not found.");

    public static Error InvalidStatusForCustomer => Error.Conflict(
        "CancelOrder.InvalidStatusForCustomer", "Order cannot be cancelled by customer at this stage.");
}
