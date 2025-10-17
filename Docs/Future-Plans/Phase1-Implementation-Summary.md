# Seeding Module Extension - Phase 1 Foundation

This directory contains the infrastructure and utilities for extending the seeding module to support business flow records (Orders, Coupons, Reviews).

## Phase 1 Deliverables ✅

### 1. Configuration Extensions
- **File**: `SeedingConfigurationExtensions.cs`
- **Added Methods**:
  - `GetCouponBundleOptions()` - Retrieves Coupon seeder configuration
  - `GetOrderSeedingOptions()` - Retrieves Order seeder configuration
  - `GetReviewSeedingOptions()` - Retrieves Review seeder configuration
- **Helper Methods**: Support for int, decimal, and Dictionary<string, int> configuration values

### 2. Options Classes
Created three new options classes in the `Options/` directory:

#### CouponBundleOptions.cs
- `ReportOnly` - Preview mode without making changes
- `OverwriteExisting` - Update existing coupons
- `CouponGlobs` - File patterns for loading bundles

#### OrderSeedingOptions.cs
- `OrdersPerRestaurant` - Number of orders per restaurant
- `StatusDistribution` - Distribution of order statuses (%)
- `CouponUsagePercentage` - % of orders using coupons
- `OnlinePaymentPercentage` - % online vs COD
- `CreateRealisticTimestamps` - Spread timestamps over history
- `OrderHistoryDays` - Days to spread timestamps
- `GenerateSpecialInstructions` - Add random instructions
- `TipPercentage` - % of orders with tips
- `MinItemsPerOrder` / `MaxItemsPerOrder` - Item count range

#### ReviewSeedingOptions.cs
- `ReviewPercentage` - % of delivered orders that get reviews
- `ReplyPercentage` - % of reviews that get restaurant replies
- `GenerateComments` - Whether to add comment text
- `PositiveComments[]` - Pool of positive Vietnamese comment templates (e.g., "Đồ ăn ngon và giao hàng nhanh!", "Chất lượng tuyệt vời!")
- `NeutralComments[]` - Pool of neutral Vietnamese comment templates (e.g., "Bữa ăn tạm được", "Ổn")
- `NegativeComments[]` - Pool of negative Vietnamese comment templates (e.g., "Đồ ăn đến lạnh", "Giao hàng lâu quá")
- `ReplyTemplates[]` - Pool of Vietnamese restaurant reply templates (e.g., "Cảm ơn phản hồi của bạn!", "Rất vui vì bạn thích món ăn!")

### 3. Base Utilities for Command-Based Seeding

#### CommandBasedSeeder.cs
Abstract base class for seeders that use MediatR commands:
- `ExecuteCommandAsUserAsync<TResponse>()` - Execute commands with user context
- `LogProgress()` / `LogWarning()` / `LogError()` - Consistent logging

Includes `SeedingUserContext` implementation of `IUser` for authentication during seeding.

#### SeedingDataGenerator.cs
Static utility class for generating realistic Vietnamese test data for Hanoi context:

**Address Generation**:
- `GenerateRandomDeliveryAddress()` - Creates realistic Vietnamese addresses in Hanoi
  - Districts: Hoàn Kiếm, Ba Đình, Đống Đa, Hai Bà Trưng, Thanh Xuân, Cầu Giấy, Tây Hồ, Long Biên
  - Authentic street names: "24 Lê Văn Hưu", "49 Bát Đàn", "14 Chả Cá", etc.
  - Proper postal codes by district (e.g., Hoàn Kiếm: 11016-11018)
  - City: "Hà Nội", Country: "Vietnam"

**Special Instructions** (Vietnamese):
- `GenerateSpecialInstructions()` - Random delivery instructions in Vietnamese
  - Examples: "Vui lòng bấm chuông cửa", "Để ở cửa nhà", "Gọi điện khi tới", "Không hành", "Thêm tương ớt riêng"

**Tip Generation**:
- `GenerateTipAmount()` - Random tip amounts in VND (₫10,000 - ₫50,000 typical range)

**Timestamp Generation**:
- `GenerateRealisticTimestamp()` - Spreads over history period
- `GenerateTimestampForOrderStatus()` - Status-appropriate timestamps

**Item Selection**:
- `SelectRandomItems<T>()` - Random selection from list
- `SelectByWeight<T>()` - Weighted probability selection

**Other Utilities**:
- `GeneratePaymentGatewayReference()` - Fake payment IDs
- `GenerateQuantity()` - Realistic item quantities

### 4. Directory Structure
Created the following directory structure:
```
Seeding/
├── Seeders/
│   ├── CouponSeeders/      (ready for Phase 2)
│   ├── OrderSeeders/       (ready for Phase 2)
│   └── ReviewSeeders/      (ready for Phase 3)
└── Data/
    └── Coupons/            (ready for coupon bundle files)
```

### 5. DependencyInjection Updates
Updated `DependencyInjection.cs`:
- Organized seeder registration with comments
- Added placeholder comments for future seeders
- Ready for Phase 2-3 seeder registration

## Configuration Example

Add to `appsettings.Development.json`:

```json
{
  "Seeding": {
    "Profile": "Development",
    "EnableIdempotentSeeding": true,
    "SeedTestData": true,
    "EnabledSeeders": {
      "Role": true,
      "User": true,
      "Tag": true,
      "RestaurantBundle": true,
      "CouponBundle": false,
      "Order": false,
      "Review": false
    },
    "SeederSettings": {
      "CouponBundle": {
        "ReportOnly": false,
        "OverwriteExisting": false,
        "CouponGlobs": ["*.coupon.json"]
      },
      "Order": {
        "OrdersPerRestaurant": 15,
        "StatusDistribution": {
          "Delivered": 60,
          "Accepted": 10,
          "Preparing": 10,
          "ReadyForDelivery": 5,
          "Cancelled": 10,
          "Rejected": 5
        },
        "CouponUsagePercentage": 30,
        "OnlinePaymentPercentage": 70,
        "CreateRealisticTimestamps": true,
        "OrderHistoryDays": 90,
        "GenerateSpecialInstructions": true,
        "TipPercentage": 40,
        "MinItemsPerOrder": 1,
        "MaxItemsPerOrder": 5
      },
      "Review": {
        "ReviewPercentage": 40,
        "ReplyPercentage": 50,
        "GenerateComments": true
      }
    }
  }
}
```

## Next Steps

### Phase 2: Coupon Bundle Seeding (Week 1-2)
1. Create `CouponBundleDto.cs` and related DTOs
2. Implement `CouponBundleValidation.cs`
3. Build `CouponBundleSeeder.cs`
4. Create sample coupon JSON files
5. Register seeder in DI
6. Test idempotency

### Phase 3: Order Seeding (Week 2-3)
1. Create `OrderSeeder.cs` with command integration
2. Implement scenario generator
3. Integrate with `OrderFinancialService`
4. Add state transition logic
5. Performance optimization

### Phase 4: Review Seeding (Week 3)
1. Create `ReviewSeeder.cs` with command integration
2. Implement rating distribution
3. Generate contextual comments
4. Add restaurant replies

## Files Created in Phase 1

```
✅ Options/CouponBundleOptions.cs
✅ Options/OrderSeedingOptions.cs
✅ Options/ReviewSeedingOptions.cs
✅ SeedingConfigurationExtensions.cs (extended)
✅ CommandBasedSeeder.cs
✅ SeedingDataGenerator.cs
✅ DependencyInjection.cs (updated)
✅ Seeders/CouponSeeders/ (directory)
✅ Seeders/OrderSeeders/ (directory)
✅ Seeders/ReviewSeeders/ (directory)
✅ Data/Coupons/ (directory)
```

## Testing the Foundation

To verify Phase 1 foundation is working:

1. **Build the solution**:
   ```bash
   dotnet build
   ```

2. **Verify configuration loading**:
   The new options should be loadable via the extension methods without errors.

3. **Check directory structure**:
   Ensure all new directories exist and are ready for Phase 2-4 implementations.

## Notes

- The `SeedingUserContext` may need adjustments based on the actual `IUser` implementation
- Command-based seeders will need careful testing for authentication context handling
- All utility methods in `SeedingDataGenerator` use a single `Random` instance for consistency
- Configuration parsing supports multiple formats: JsonObject, JsonElement, Dictionary, and JSON strings

---

**Phase 1 Status**: ✅ Complete  
**Date**: 2025-10-17  
**Ready for**: Phase 2 - Coupon Bundle Seeding

---

## 🎯 Next Steps

### Immediate (Phase 2 - Week 1-2)
**Coupon Bundle Seeding**:
1. Create `CouponBundleDto.cs` with JSON serialization attributes
2. Implement `CouponBundleValidation.cs` for bundle validation
3. Build `CouponBundleSeeder.cs` implementing `ISeeder`
4. Create 5-10 sample coupon JSON files for different scenarios
5. Register seeder in `DependencyInjection.cs`
6. Write unit tests for validation logic
7. Test idempotency and error handling

### Short Term (Phase 3 - Week 2-3)
**Order Seeding**:
1. Create `OrderSeeder.cs` extending `CommandBasedSeeder`
2. Implement `OrderScenarioGenerator.cs` for realistic order scenarios
3. Integrate with `InitiateOrderCommand` handler
4. Add order state transition logic
5. Performance optimization with bulk operations
6. Integration testing

### Medium Term (Phase 4 - Week 3)
**Review Seeding**:
1. Create `ReviewSeeder.cs` extending `CommandBasedSeeder`
2. Implement rating distribution algorithm
3. Generate contextual comments based on rating
4. Add restaurant reply generation
5. Integration with `CreateReviewCommand`

---

## 🎉 Phase 1 Status: COMPLETE

All foundation components are in place and verified. The project is ready for Phase 2 implementation.

**Date Completed**: October 17, 2025  
**Build Status**: ✅ Passing  
**Test Status**: ✅ Compiles Successfully  
**Documentation**: ✅ Complete  

### 🌏 Vietnamese Localization

All seeding data generators have been localized for the Vietnamese/Hanoi context to match the existing restaurant bundles:

- **Addresses**: Authentic Hanoi addresses with proper districts (Hoàn Kiếm, Ba Đình, etc.) and postal codes
- **Special Instructions**: Vietnamese delivery instructions ("Vui lòng bấm chuông cửa", "Gọi điện khi tới", etc.)
- **Review Comments**: Complete Vietnamese comment pools for positive, neutral, and negative reviews
- **Restaurant Replies**: Vietnamese reply templates matching local service culture
- **Currency**: VND-appropriate tip amounts and pricing references

This ensures generated test data is realistic and contextually appropriate for the Vietnamese market.

---

## 🔗 Related Documents

- Design Document: `Docs/Future-Plans/Seeding-Module-Extension-Design.md`
- Phase 1 Guide: `src/Infrastructure/Persistence/EfCore/Seeding/README-Phase1.md`
- Project Documentation: `Docs/Architecture/YummyZoom_Project_Documentation.md`

---

**Prepared by**: AI Assistant  
**Review Status**: Ready for Team Review  
**Approval**: Pending
