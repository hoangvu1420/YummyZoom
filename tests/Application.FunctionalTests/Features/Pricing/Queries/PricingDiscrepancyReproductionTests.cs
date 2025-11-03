using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Pricing.Queries.GetPricingPreview;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Pricing.Queries;

/// <summary>
/// Test to reproduce the exact pricing discrepancy reported by frontend team between
/// pricing preview (77,640 VND) and order initiation (69,000 VND) using seeded Vietnamese restaurant data.
/// </summary>
[TestFixture]
public class PricingDiscrepancyReproductionTests : BaseTestFixture
{
    private Guid _bunChaRestaurantId;
    private Guid _bunChaMenuItemId;
    private Guid _anThemCustomizationGroupId;
    private Guid _themNemCuaBeChoiceId;
    private Guid _customerId;

    [SetUp]
    public async Task SetUpTestData()
    {
        // Trigger seeding to ensure Vietnamese restaurant data is in place
        await TriggerSeedingAsync();
        
        // Find the seeded "Bún Chả Hương Liên" restaurant and related data
        await LocateSeededVietnameseRestaurantDataAsync();
        
        // Set up user authentication
        _customerId = Testing.TestData.DefaultCustomerId;
        SetUserId(_customerId);
    }

    private async Task TriggerSeedingAsync()
    {
        Console.WriteLine("🌱 DEBUG: Triggering database seeding...");
        
        using var scope = TestInfrastructure.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<YummyZoom.Infrastructure.Persistence.EfCore.ApplicationDbContextInitialiser>();
        
        try
        {
            await initialiser.SeedAsync();
            Console.WriteLine("✅ DEBUG: Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ DEBUG: Database seeding failed: {ex.Message}");
            throw;
        }
    }

    private async Task LocateSeededVietnameseRestaurantDataAsync()
    {
        Console.WriteLine("🔍 DEBUG: Looking for seeded Vietnamese restaurant data...");

        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YummyZoom.Infrastructure.Persistence.EfCore.ApplicationDbContext>();

        // Find Bún Chả Hương Liền restaurant
        var restaurants = await context.Restaurants
            .AsNoTracking()
            .ToListAsync();
        
        // Filter on client side to avoid EF translation issues
        restaurants = restaurants
            .Where(r => r.Name.Contains("Bún Chả") || r.Name.Contains("Hương Liên"))
            .ToList();

        Console.WriteLine($"🔍 DEBUG: Found {restaurants.Count} restaurants with 'Bún Chả' or 'Hương Liên' in name");
        foreach (var restaurant in restaurants)
        {
            Console.WriteLine($"🔍 DEBUG: Restaurant: {restaurant.Name} (ID: {restaurant.Id.Value})");
        }

        _bunChaRestaurantId = restaurants.FirstOrDefault()?.Id.Value ?? Guid.Empty;
        if (_bunChaRestaurantId == Guid.Empty)
        {
            throw new InvalidOperationException("Could not find seeded Vietnamese restaurant 'Bún Chả Hương Liên'");
        }

        Console.WriteLine($"✅ DEBUG: Using restaurant ID: {_bunChaRestaurantId}");

        // Find "Bún chả" menu item (50,000 VND)
        var allMenuItems = await context.MenuItems
            .AsNoTracking()
            .ToListAsync();
        var menuItems = allMenuItems
            .Where(mi => mi.RestaurantId.Value == _bunChaRestaurantId)
            .Where(mi => mi.Name.Contains("Bún chả") || mi.Name.Contains("Bun cha"))
            .ToList();

        Console.WriteLine($"🔍 DEBUG: Found {menuItems.Count} menu items with 'Bún chả' in name");
        foreach (var item in menuItems)
        {
            Console.WriteLine($"🔍 DEBUG: Menu Item: {item.Name} - Price: {item.BasePrice.Amount} {item.BasePrice.Currency} (ID: {item.Id.Value})");
        }

        _bunChaMenuItemId = menuItems.FirstOrDefault(mi => 
            mi.Name.Equals("Bún chả", StringComparison.OrdinalIgnoreCase) && 
            mi.BasePrice.Amount == 50000m)?.Id.Value ?? Guid.Empty;

        if (_bunChaMenuItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Could not find 'Bún chả' menu item with 50,000 VND price");
        }

        Console.WriteLine($"✅ DEBUG: Using menu item ID: {_bunChaMenuItemId}");

        // Find "Ăn thêm" customization group
        var allCustomizationGroups = await context.CustomizationGroups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(cg => cg.Choices)
            .ToListAsync();
        var customizationGroups = allCustomizationGroups
            .Where(cg => cg.RestaurantId.Value == _bunChaRestaurantId)
            .ToList();

        Console.WriteLine($"🔍 DEBUG: Found {customizationGroups.Count} customization groups for restaurant");
        foreach (var group in customizationGroups)
        {
            Console.WriteLine($"🔍 DEBUG: Customization Group: {group.GroupName} (ID: {group.Id.Value})");
        }

        var anThemGroup = customizationGroups.FirstOrDefault(cg => 
            cg.GroupName.Contains("Ăn thêm") || cg.GroupName.Contains("An them"));

        if (anThemGroup is null)
        {
            throw new InvalidOperationException("Could not find 'Ăn thêm' customization group");
        }

        _anThemCustomizationGroupId = anThemGroup.Id.Value;
        Console.WriteLine($"✅ DEBUG: Using customization group ID: {_anThemCustomizationGroupId}");

        // Find "Thêm nem cua bể" choice (8,000 VND adjustment)
        var choices = anThemGroup.Choices;
        Console.WriteLine($"🔍 DEBUG: Found {choices.Count} choices in 'Ăn thêm' group");
        foreach (var choice in choices)
        {
            Console.WriteLine($"🔍 DEBUG: Choice: {choice.Name} - Adjustment: {choice.PriceAdjustment.Amount} {choice.PriceAdjustment.Currency} (ID: {choice.Id.Value})");
        }

        var themNemCuaBeChoice = choices.FirstOrDefault(c => 
            c.Name.Contains("nem cua bể") || c.Name.Contains("nem cua be"));

        if (themNemCuaBeChoice is null)
        {
            throw new InvalidOperationException("Could not find 'Thêm nem cua bể' choice");
        }

        _themNemCuaBeChoiceId = themNemCuaBeChoice.Id.Value;
        Console.WriteLine($"✅ DEBUG: Using choice ID: {_themNemCuaBeChoiceId}");
        Console.WriteLine($"✅ DEBUG: Choice price adjustment: {themNemCuaBeChoice.PriceAdjustment.Amount} VND");
    }

    [Test]
    public async Task PricingPreview_vs_OrderInitiation_ShouldHaveSameTotal_BunChaWithCustomization()
    {
        Console.WriteLine("\n🧪 TEST: Starting pricing discrepancy reproduction test...");

        // Step 1: Get Pricing Preview (this should return 77,640 VND)
        Console.WriteLine("\n📊 STEP 1: Getting pricing preview...");
        
        var pricingQuery = new GetPricingPreviewQuery(
            RestaurantId: _bunChaRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(
                    MenuItemId: _bunChaMenuItemId,
                    Quantity: 1,
                    Customizations: new List<PricingPreviewCustomizationDto>
                    {
                        new(
                            CustomizationGroupId: _anThemCustomizationGroupId,
                            ChoiceIds: new List<Guid> { _themNemCuaBeChoiceId }
                        )
                    }
                )
            },
            CouponCode: null,
            TipAmount: 0
        );

        var pricingResult = await SendAsync(pricingQuery);
        
        Console.WriteLine($"📊 DEBUG: Pricing preview result success: {pricingResult.IsSuccess}");
        if (pricingResult.IsFailure)
        {
            Console.WriteLine($"❌ DEBUG: Pricing preview failed: {pricingResult.Error}");
        }

        pricingResult.ShouldBeSuccessful();
        var pricingResponse = pricingResult.Value;

        Console.WriteLine($"📊 DEBUG: Pricing Preview Results:");
        Console.WriteLine($"  - Subtotal: {pricingResponse.Subtotal.Amount} {pricingResponse.Subtotal.Currency}");
        Console.WriteLine($"  - Delivery Fee: {pricingResponse.DeliveryFee?.Amount} {pricingResponse.DeliveryFee?.Currency}");
        Console.WriteLine($"  - Tax: {pricingResponse.TaxAmount.Amount} {pricingResponse.TaxAmount.Currency}");
        Console.WriteLine($"  - Total: {pricingResponse.TotalAmount.Amount} {pricingResponse.TotalAmount.Currency}");
        Console.WriteLine($"  - Notes Count: {pricingResponse.Notes.Count}");

        foreach (var note in pricingResponse.Notes)
        {
            Console.WriteLine($"  - Note: [{note.Type}] {note.Code}: {note.Message}");
        }

        // Step 2: Initiate Order (this should return 69,000 VND but we expect same as pricing preview)
        Console.WriteLine("\n🛒 STEP 2: Initiating order...");

        var initiateCommand = new InitiateOrderCommand(
            CustomerId: _customerId,
            RestaurantId: _bunChaRestaurantId,
            Items: new List<OrderItemDto>
            {
                new(
                    MenuItemId: _bunChaMenuItemId,
                    Quantity: 1,
                    Customizations: new List<OrderItemCustomizationRequestDto>
                    {
                        new(
                            CustomizationGroupId: _anThemCustomizationGroupId,
                            ChoiceIds: new List<Guid> { _themNemCuaBeChoiceId }
                        )
                    }
                )
            },
            DeliveryAddress: new DeliveryAddressDto(
                Street: "56 Phố Huế, Phường Ngô Quyền",
                City: "Hà Nội",
                State: "Hà Nội",
                ZipCode: "100000",
                Country: "VN"
            ),
            PaymentMethod: "CashOnDelivery",
            SpecialInstructions: null,
            CouponCode: null,
            TipAmount: 0,
            TeamCartId: null
        );

        var orderResult = await SendAsync(initiateCommand);
        
        Console.WriteLine($"🛒 DEBUG: Order initiation result success: {orderResult.IsSuccess}");
        if (orderResult.IsFailure)
        {
            Console.WriteLine($"❌ DEBUG: Order initiation failed: {orderResult.Error}");
        }

        orderResult.ShouldBeSuccessful();
        var orderResponse = orderResult.Value;

        Console.WriteLine($"🛒 DEBUG: Order Initiation Results:");
        Console.WriteLine($"  - Order ID: {orderResponse.OrderId}");
        Console.WriteLine($"  - Order Number: {orderResponse.OrderNumber}");
        Console.WriteLine($"  - Total Amount: {orderResponse.TotalAmount.Amount} {orderResponse.TotalAmount.Currency}");

        // Step 3: Fetch the created order to examine its financial breakdown
        Console.WriteLine("\n🔍 STEP 3: Examining created order details...");

        var createdOrder = await TestDatabaseManager.FindOrderAsync(orderResponse.OrderId);
        createdOrder.Should().NotBeNull();

        Console.WriteLine($"🔍 DEBUG: Created Order Financial Breakdown:");
        Console.WriteLine($"  - Subtotal: {createdOrder!.Subtotal.Amount} {createdOrder.Subtotal.Currency}");
        Console.WriteLine($"  - Discount: {createdOrder.DiscountAmount.Amount} {createdOrder.DiscountAmount.Currency}");
        Console.WriteLine($"  - Delivery Fee: {createdOrder.DeliveryFee.Amount} {createdOrder.DeliveryFee.Currency}");
        Console.WriteLine($"  - Tip: {createdOrder.TipAmount.Amount} {createdOrder.TipAmount.Currency}");
        Console.WriteLine($"  - Tax: {createdOrder.TaxAmount.Amount} {createdOrder.TaxAmount.Currency}");
        Console.WriteLine($"  - Total: {createdOrder.TotalAmount.Amount} {createdOrder.TotalAmount.Currency}");

        Console.WriteLine($"🔍 DEBUG: Order Items ({createdOrder.OrderItems.Count}):");
        foreach (var item in createdOrder.OrderItems)
        {
            Console.WriteLine($"  - Item: {item.Snapshot_ItemName}");
            Console.WriteLine($"    - Base Price: {item.Snapshot_BasePriceAtOrder.Amount} {item.Snapshot_BasePriceAtOrder.Currency}");
            Console.WriteLine($"    - Quantity: {item.Quantity}");
            Console.WriteLine($"    - Line Total: {item.LineItemTotal.Amount} {item.LineItemTotal.Currency}");
            Console.WriteLine($"    - Customizations ({item.SelectedCustomizations.Count}):");
            foreach (var customization in item.SelectedCustomizations)
            {
                Console.WriteLine($"      - {customization.Snapshot_CustomizationGroupName}: {customization.Snapshot_ChoiceName} (+{customization.Snapshot_ChoicePriceAdjustmentAtOrder.Amount} {customization.Snapshot_ChoicePriceAdjustmentAtOrder.Currency})");
            }
        }

        // Step 4: Compare the totals - they should be identical
        Console.WriteLine("\n⚖️ STEP 4: Comparing totals...");
        
        var pricingTotal = pricingResponse.TotalAmount.Amount;
        var orderTotal = orderResponse.TotalAmount.Amount;
        var difference = Math.Abs(pricingTotal - orderTotal);

        Console.WriteLine($"⚖️ DEBUG: Pricing Preview Total: {pricingTotal} VND");
        Console.WriteLine($"⚖️ DEBUG: Order Initiation Total: {orderTotal} VND");
        Console.WriteLine($"⚖️ DEBUG: Difference: {difference} VND");

        if (difference > 0.01m) // Allow for small floating point differences
        {
            Console.WriteLine($"❌ DISCREPANCY FOUND: Expected totals to match, but found difference of {difference} VND");
            
            // Additional debugging: Compare subtotals
            var pricingSubtotal = pricingResponse.Subtotal.Amount;
            var orderSubtotal = createdOrder.Subtotal.Amount;
            var subtotalDifference = Math.Abs(pricingSubtotal - orderSubtotal);
            
            Console.WriteLine($"🔍 DEBUG: Subtotal comparison:");
            Console.WriteLine($"  - Pricing Subtotal: {pricingSubtotal} VND");
            Console.WriteLine($"  - Order Subtotal: {orderSubtotal} VND");
            Console.WriteLine($"  - Subtotal Difference: {subtotalDifference} VND");
            
            if (subtotalDifference > 0.01m)
            {
                Console.WriteLine($"🚨 SUBTOTAL MISMATCH: This indicates customizations are being handled differently!");
            }
        }

        // Assertion: The totals should be identical
        orderResponse.TotalAmount.Amount.Should().Be(pricingResponse.TotalAmount.Amount,
            "Pricing preview and order initiation should calculate the same total amount");

        // Additional assertions for debugging
        createdOrder.Subtotal.Amount.Should().Be(pricingResponse.Subtotal.Amount,
            "Subtotals should match between pricing preview and actual order");
    }
}
