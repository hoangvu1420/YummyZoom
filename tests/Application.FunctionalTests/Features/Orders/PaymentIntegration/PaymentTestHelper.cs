using Stripe;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;

/// <summary>
/// Combined test helper class providing utilities for Stripe payment testing and test data building.
/// Combines functionality of StripeTestHelper and PaymentTestDataBuilder from the implementation plan.
/// </summary>
public static class PaymentTestHelper
{
    #region Stripe Test Helper Methods

    /// <summary>
    /// Confirms a payment intent using Stripe SDK with the specified payment method.
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID to confirm.</param>
    /// <param name="paymentMethod">The test payment method to use (e.g., pm_card_visa).</param>
    /// <returns>The confirmed PaymentIntent.</returns>
    public static async Task<PaymentIntent> ConfirmPaymentAsync(string paymentIntentId, string paymentMethod)
    {
        var service = new PaymentIntentService();
        try
        {
            // happy-path: card succeeds (or SCA in-page)
            return await service.ConfirmAsync(paymentIntentId, new PaymentIntentConfirmOptions
            {
                PaymentMethod = paymentMethod
            });
        }
        catch (StripeException)
        {
            // for declined cards and “requires_action” cases, ConfirmAsync throws,
            // so grab the current state of the intent and return that instead.
            return await service.GetAsync(paymentIntentId);
        }
    }

    /// <summary>
    /// Generates a realistic Stripe webhook JSON payload for testing.
    /// </summary>
    /// <param name="eventType">The webhook event type (e.g., payment_intent.succeeded).</param>
    /// <param name="paymentIntentId">The payment intent ID for the event.</param>
    /// <param name="amount">The payment amount in cents.</param>
    /// <param name="currency">The payment currency (default: usd).</param>
    /// <param name="metadata">Optional metadata dictionary to include in the payment intent.</param>
    /// <returns>JSON string representing the webhook payload.</returns>
    public static string GenerateWebhookPayload(string eventType, string paymentIntentId, long amount = 2500, string currency = "usd", IDictionary<string, string>? metadata = null)
    {
        var webhookPayload = new
        {
            id = $"evt_{Guid.NewGuid().ToString("N")[..24]}",
            @object = "event",
            api_version = "2025-06-30.basil",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            data = new
            {
                @object = new
                {
                    id = paymentIntentId,
                    @object = "payment_intent",
                    amount = amount,
                    currency = currency,
                    status = GetPaymentIntentStatusFromEvent(eventType),
                    metadata = metadata ?? new Dictionary<string, string>(),
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    client_secret = $"{paymentIntentId}_secret_{Guid.NewGuid().ToString("N")[..16]}"
                }
            },
            livemode = false,
            pending_webhooks = 1,
            request = new
            {
                id = $"req_{Guid.NewGuid().ToString("N")[..24]}",
                idempotency_key = (string?)null
            },
            type = eventType
        };

        return JsonSerializer.Serialize(webhookPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Generates a valid Stripe webhook signature for the given payload and secret.
    /// </summary>
    /// <param name="payload">The webhook payload JSON string.</param>
    /// <param name="secret">The webhook endpoint secret.</param>
    /// <param name="timestamp">Optional timestamp (defaults to current time).</param>
    /// <returns>The webhook signature header value.</returns>
    public static string GenerateWebhookSignature(string payload, string secret, long? timestamp = null)
    {
        timestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var signedPayload = $"{timestamp}.{payload}";
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var signature = Convert.ToHexString(hash).ToLowerInvariant();

        return $"t={timestamp},v1={signature}";
    }

    /// <summary>
    /// Creates a test payment intent using Stripe SDK for integration testing.
    /// </summary>
    /// <param name="amount">Amount in cents.</param>
    /// <param name="currency">Currency code (default: usd).</param>
    /// <param name="metadata">Optional metadata to attach.</param>
    /// <returns>The created PaymentIntent.</returns>
    public static async Task<PaymentIntent> CreateTestPaymentIntentAsync(
        long amount = 2500,
        string currency = "usd",
        Dictionary<string, string>? metadata = null)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = amount,
            Currency = currency,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        return await service.CreateAsync(options);
    }

    #endregion

    #region Payment Test Data Builder Methods

    /// <summary>
    /// Builds a valid InitiateOrderCommand for online payment testing using real MenuItemIds.
    /// </summary>
    /// <param name="customerId">Optional customer ID (generates new if not provided).</param>
    /// <param name="restaurantId">Optional restaurant ID (generates new if not provided).</param>
    /// <param name="menuItemIds">List of real MenuItemIds to use in the order.</param>
    /// <param name="paymentMethod">Payment method (default: CreditCard).</param>
    /// <param name="tipAmount">Tip amount (default: 5.00).</param>
    /// <returns>A valid InitiateOrderCommand for testing.</returns>
    public static InitiateOrderCommand BuildValidOnlineOrderCommand(
        Guid? customerId = null,
        Guid? restaurantId = null,
        List<Guid>? menuItemIds = null,
        string paymentMethod = "CreditCard",
        decimal? tipAmount = 5.00m)
    {
        return new InitiateOrderCommand(
            CustomerId: customerId ?? Guid.NewGuid(),
            RestaurantId: restaurantId ?? Guid.NewGuid(),
            Items: menuItemIds != null ? BuildTestOrderItems(menuItemIds) : new List<OrderItemDto>(),
            DeliveryAddress: BuildTestDeliveryAddress(),
            PaymentMethod: paymentMethod,
            SpecialInstructions: "Test order - please handle with care",
            CouponCode: null,
            TipAmount: tipAmount,
            TeamCartId: null
        );
    }

    /// <summary>
    /// Builds a valid InitiateOrderCommand for Cash on Delivery (COD) payment testing.
    /// </summary>
    /// <param name="customerId">Optional customer ID (generates new if not provided).</param>
    /// <param name="restaurantId">Optional restaurant ID (generates new if not provided).</param>
    /// <param name="menuItemIds">List of real MenuItemIds to use in the order.</param>
    /// <returns>A valid InitiateOrderCommand for COD testing.</returns>
    public static InitiateOrderCommand BuildValidCODOrderCommand(
        Guid? customerId = null,
        Guid? restaurantId = null,
        List<Guid>? menuItemIds = null)
    {
        return new InitiateOrderCommand(
            CustomerId: customerId ?? Guid.NewGuid(),
            RestaurantId: restaurantId ?? Guid.NewGuid(),
            Items: menuItemIds != null ? BuildTestOrderItems(menuItemIds) : new List<OrderItemDto>(),
            DeliveryAddress: BuildTestDeliveryAddress(),
            PaymentMethod: "CashOnDelivery",
            SpecialInstructions: "COD test order",
            CouponCode: null,
            TipAmount: null,
            TeamCartId: null
        );
    }

    /// <summary>
    /// Builds a valid InitiateOrderCommand with a coupon for testing discount scenarios.
    /// </summary>
    /// <param name="couponCode">The coupon code to apply.</param>
    /// <param name="customerId">Optional customer ID (generates new if not provided).</param>
    /// <param name="restaurantId">Optional restaurant ID (generates new if not provided).</param>
    /// <param name="menuItemIds">List of real MenuItemIds to use in the order.</param>
    /// <returns>A valid InitiateOrderCommand with coupon for testing.</returns>
    public static InitiateOrderCommand BuildOrderCommandWithCoupon(
        string couponCode,
        Guid? customerId = null,
        Guid? restaurantId = null,
        List<Guid>? menuItemIds = null)
    {
        return new InitiateOrderCommand(
            CustomerId: customerId ?? Guid.NewGuid(),
            RestaurantId: restaurantId ?? Guid.NewGuid(),
            Items: menuItemIds != null ? BuildTestOrderItems(menuItemIds) : new List<OrderItemDto>(),
            DeliveryAddress: BuildTestDeliveryAddress(),
            PaymentMethod: "CreditCard",
            SpecialInstructions: "Test order with coupon",
            CouponCode: couponCode,
            TipAmount: 3.00m,
            TeamCartId: null
        );
    }

    /// <summary>
    /// Builds a list of test order items for testing using provided MenuItemIds.
    /// </summary>
    /// <param name="menuItemIds">List of actual MenuItemIds to use in the order.</param>
    /// <returns>List of OrderItemDto for testing.</returns>
    public static List<OrderItemDto> BuildTestOrderItems(List<Guid> menuItemIds)
    {
        var items = new List<OrderItemDto>();

        for (int i = 0; i < menuItemIds.Count; i++)
        {
            items.Add(new OrderItemDto(
                MenuItemId: menuItemIds[i],
                Quantity: i + 1 // Vary quantities: 1, 2, 3, etc.
            ));
        }

        return items;
    }

    /// <summary>
    /// Builds a list of test order items for testing (legacy method for backward compatibility).
    /// </summary>
    /// <param name="itemCount">Number of different items to include (default: 2).</param>
    /// <returns>List of OrderItemDto for testing.</returns>
    [Obsolete("Use BuildTestOrderItems(List<Guid> menuItemIds) instead for real MenuItem entities")]
    public static List<OrderItemDto> BuildTestOrderItems(int itemCount = 2)
    {
        var items = new List<OrderItemDto>();

        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new OrderItemDto(
                MenuItemId: Guid.NewGuid(),
                Quantity: i + 1 // Vary quantities: 1, 2, 3, etc.
            ));
        }

        return items;
    }

    /// <summary>
    /// Builds a list of test order items for testing with a restaurant context.
    /// This method creates actual MenuItem entities in the database for the specified restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant ID to create menu items for.</param>
    /// <param name="itemCount">Number of different items to create (default: 2).</param>
    /// <returns>List of OrderItemDto with real MenuItemIds for testing.</returns>
    public static async Task<List<OrderItemDto>> BuildTestOrderItemsAsync(Guid restaurantId, int itemCount = 2)
    {
        // Create Menu entity for the restaurant
        var menu = Menu.Create(
            RestaurantId.Create(restaurantId),
            "Test Menu",
            "Test menu description").Value;

        await AddAsync(menu);

        // Create MenuCategory for organizing menu items
        var menuCategory = MenuCategory.Create(
            menu.Id,
            "Test Category",
            1).Value;

        await AddAsync(menuCategory);

        // Create MenuItem entities
        var menuItemIds = new List<Guid>();
        for (int i = 0; i < itemCount; i++)
        {
            var menuItem = MenuItem.Create(
                RestaurantId.Create(restaurantId),
                menuCategory.Id,
                $"Test Item {i + 1}",
                $"Description for test item {i + 1}",
                new Money(10.00m + (i * 2.50m), "USD")).Value;

            await AddAsync(menuItem);
            menuItemIds.Add(menuItem.Id.Value);
        }

        // Build OrderItemDto list using the real MenuItemIds
        return BuildTestOrderItems(menuItemIds);
    }

    /// <summary>
    /// Builds a test delivery address for testing.
    /// </summary>
    /// <param name="street">Optional street address.</param>
    /// <param name="city">Optional city name.</param>
    /// <param name="state">Optional state.</param>
    /// <param name="zipCode">Optional ZIP code.</param>
    /// <param name="country">Optional country.</param>
    /// <returns>A valid DeliveryAddressDto for testing.</returns>
    public static DeliveryAddressDto BuildTestDeliveryAddress(
        string? street = null,
        string? city = null,
        string? state = null,
        string? zipCode = null,
        string? country = null)
    {
        return new DeliveryAddressDto(
            Street: street ?? "123 Test Street, Apt 4B",
            City: city ?? "Test City",
            State: state ?? "CA",
            ZipCode: zipCode ?? "90210",
            Country: country ?? "US"
        );
    }

    /// <summary>
    /// Builds an InitiateOrderCommand with invalid data for negative testing.
    /// </summary>
    /// <param name="invalidField">The field to make invalid.</param>
    /// <param name="menuItemIds">List of real MenuItemIds to use in the order.</param>
    /// <returns>An InitiateOrderCommand with invalid data.</returns>
    public static InitiateOrderCommand BuildInvalidOrderCommand(string invalidField = "items", List<Guid>? menuItemIds = null)
    {
        return invalidField.ToLowerInvariant() switch
        {
            "items" => new InitiateOrderCommand(
                CustomerId: Guid.NewGuid(),
                RestaurantId: Guid.NewGuid(),
                Items: new List<OrderItemDto>(), // Empty items list
                DeliveryAddress: BuildTestDeliveryAddress(),
                PaymentMethod: "CreditCard"
            ),
            "address" => new InitiateOrderCommand(
                CustomerId: Guid.NewGuid(),
                RestaurantId: Guid.NewGuid(),
                Items: menuItemIds != null ? BuildTestOrderItems(menuItemIds) : new List<OrderItemDto>(),
                DeliveryAddress: new DeliveryAddressDto("", "", "", "", ""), // Empty address
                PaymentMethod: "CreditCard"
            ),
            "paymentmethod" => new InitiateOrderCommand(
                CustomerId: Guid.NewGuid(),
                RestaurantId: Guid.NewGuid(),
                Items: menuItemIds != null ? BuildTestOrderItems(menuItemIds) : new List<OrderItemDto>(),
                DeliveryAddress: BuildTestDeliveryAddress(),
                PaymentMethod: "" // Empty payment method
            ),
            _ => throw new ArgumentException($"Unknown invalid field: {invalidField}")
        };
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Maps webhook event types to corresponding PaymentIntent status values.
    /// </summary>
    /// <param name="eventType">The webhook event type.</param>
    /// <returns>The corresponding PaymentIntent status.</returns>
    private static string GetPaymentIntentStatusFromEvent(string eventType)
    {
        return eventType switch
        {
            "payment_intent.succeeded" => "succeeded",
            "payment_intent.payment_failed" => "requires_payment_method",
            "payment_intent.canceled" => "canceled",
            "payment_intent.requires_action" => "requires_action",
            "payment_intent.processing" => "processing",
            _ => "requires_payment_method"
        };
    }

    #endregion
}
