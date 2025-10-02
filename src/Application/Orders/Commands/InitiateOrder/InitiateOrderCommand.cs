using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Orders.Commands.InitiateOrder;

[Authorize]
public record InitiateOrderCommand(
    Guid CustomerId,
    Guid RestaurantId,
    List<OrderItemDto> Items,
    DeliveryAddressDto DeliveryAddress,
    string PaymentMethod,
    string? SpecialInstructions = null,
    string? CouponCode = null,
    decimal? TipAmount = null,
    Guid? TeamCartId = null
) : IRequest<Result<InitiateOrderResponse>>;

public record OrderItemDto(
    Guid MenuItemId,
    int Quantity,
    List<OrderItemCustomizationRequestDto>? Customizations = null
);

public record OrderItemCustomizationRequestDto(
    Guid CustomizationGroupId,
    List<Guid> ChoiceIds
);

public record DeliveryAddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country
);

public record InitiateOrderResponse(
    OrderId OrderId,
    string OrderNumber,
    Money TotalAmount,
    string? PaymentIntentId = null,
    string? ClientSecret = null
);
