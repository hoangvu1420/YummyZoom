# Outline chi tiết - Thiết kế lớp (Chương 4)

Tài liệu này là outline chi tiết cho \subsection{Thiết kế lớp}, gồm danh sách lớp tiêu biểu cần đặc tả và các luồng nghiệp vụ để vẽ biểu đồ trình tự. Nội dung này dùng để chuẩn bị viết vào báo cáo.

## 1) Định hướng chung
- Tập trung vào lớp miền (Domain) theo Clean Architecture, ưu tiên các aggregate root có nhiều quy tắc nghiệp vụ.
- Chỉ mô tả ngắn gọn vai trò của một số lớp Application layer để làm rõ vai trò điều phối, không đặc tả quá sâu vì lớp mỏng.

## 2) Các lớp được lựa chọn để đặc tả chi tiết

### 2.1 Order (Aggregate root)
Nguồn: `src/Domain/OrderAggregate/Order.cs`

Vai trò: Quản lý vòng đời đơn hàng, tính nhất quán tổng tiền, trạng thái và sự kiện miền.

Thuộc tính chính:
- `OrderNumber`, `Status`, `PlacementTimestamp`, `LastUpdateTimestamp`, `Version`
- `EstimatedDeliveryTime`, `ActualDeliveryTime`, `SpecialInstructions`
- `DeliveryAddress`, `Subtotal`, `DiscountAmount`, `DeliveryFee`, `TipAmount`, `TaxAmount`, `TotalAmount`
- `CustomerId`, `RestaurantId`, `SourceTeamCartId`, `AppliedCouponId`
- `OrderItems`, `PaymentTransactions`

Phương thức nghiệp vụ chính:
- Tạo đơn: `Create(...)` (các overload, kiểm tra item, địa chỉ, tổng tiền, tạo giao dịch thanh toán)
- Chuyển trạng thái: `Accept`, `Reject`, `Cancel`, `MarkAsPreparing`, `MarkAsReadyForDelivery`, `MarkAsDelivered`
- Xác nhận thanh toán: `RecordPaymentSuccess`, `RecordPaymentFailure`

Sự kiện miền tiếp phát:
- `OrderCreated`, `OrderPlaced`, `OrderAccepted`, `OrderRejected`, `OrderPreparing`, `OrderReadyForDelivery`, `OrderDelivered`, `OrderPaymentSucceeded`, `OrderPaymentFailed`, `OrderCancelled`

Mermaid lớp:
```mermaid
classDiagram
class Order {
  +string OrderNumber
  +OrderStatus Status
  +DateTime PlacementTimestamp
  +DateTime LastUpdateTimestamp
  +long Version
  +DateTime? EstimatedDeliveryTime
  +DateTime? ActualDeliveryTime
  +string SpecialInstructions
  +DeliveryAddress DeliveryAddress
  +Money Subtotal
  +Money DiscountAmount
  +Money DeliveryFee
  +Money TipAmount
  +Money TaxAmount
  +Money TotalAmount
  +UserId CustomerId
  +RestaurantId RestaurantId
  +TeamCartId? SourceTeamCartId
  +CouponId? AppliedCouponId
  +IReadOnlyList~OrderItem~ OrderItems
  +IReadOnlyList~PaymentTransaction~ PaymentTransactions
  +Create(OrderId orderId, UserId customerId, RestaurantId restaurantId, DeliveryAddress deliveryAddress, ...)
  +Accept(DateTime estimatedDeliveryTime, DateTime? timestamp)
  +Reject(DateTime? timestamp)
  +Cancel(DateTime? timestamp)
  +MarkAsPreparing(DateTime? timestamp)
  +MarkAsReadyForDelivery(DateTime? timestamp)
  +MarkAsDelivered(DateTime? timestamp)
  +RecordPaymentSuccess(string paymentGatewayReferenceId, DateTime? timestamp)
  +RecordPaymentFailure(string paymentGatewayReferenceId, DateTime? timestamp)
}
```

### 2.2 TeamCart (Aggregate root)
Nguồn: `src/Domain/TeamCartAggregate/TeamCart.cs`

Vai trò: Quản lý giỏ hàng nhóm, thành viên, đồng bộ thanh toán từng thành viên, và chuyển đổi sang Order.

Thuộc tính chính:
- `RestaurantId`, `HostUserId`, `Status`, `ShareToken`, `Deadline`, `CreatedAt`, `ExpiresAt`
- `Members`, `Items`, `MemberPayments`
- `QuoteVersion`, `GrandTotal`, `MemberTotals`
- `TipAmount`, `AppliedCouponId`

Phương thức nghiệp vụ chính:
- Quản lý thành viên: `AddMember`, `SetDeadline`, `ValidateJoinToken`, `IsExpired`
- Quản lý item: `AddItem`, `UpdateItemQuantity`, `RemoveItem`
- Chuyển trạng thái: `LockForPayment`, `FinalizePricing`, `MarkAsExpired`, `MarkAsConverted`
- Thanh toán: `CommitToCashOnDelivery`, `RecordSuccessfulOnlinePayment`, `RecordFailedOnlinePayment`
- Tính toán: `ComputeQuoteLite`, `GetMemberQuote` (bao gồm chia phí/đóng góp)
- Áp dụng tài chính: `ApplyTip`, `ApplyCoupon`, `RemoveCoupon`

Sự kiện miền tiêu biểu:
- `TeamCartCreated`, `MemberJoined`, `ItemAddedToTeamCart`, `ItemQuantityUpdatedInTeamCart`, `ItemRemovedFromTeamCart`
- `TeamCartLockedForPayment`, `TeamCartPricingFinalized`, `TeamCartQuoteUpdated`, `TeamCartReadyForConfirmation`, `TeamCartConverted`
- `OnlinePaymentSucceeded`, `OnlinePaymentFailed`, `MemberCommittedToPayment`, `TipAppliedToTeamCart`, `CouponAppliedToTeamCart`, `CouponRemovedFromTeamCart`

Mermaid lớp:
```mermaid
classDiagram
class TeamCart {
  +TeamCartId Id
  +RestaurantId RestaurantId
  +UserId HostUserId
  +TeamCartStatus Status
  +ShareableLinkToken ShareToken
  +DateTime? Deadline
  +DateTime CreatedAt
  +DateTime ExpiresAt
  +IReadOnlyList~TeamCartMember~ Members
  +IReadOnlyList~TeamCartItem~ Items
  +IReadOnlyList~MemberPayment~ MemberPayments
  +long QuoteVersion
  +Money GrandTotal
  +IReadOnlyDictionary~UserId, Money~ MemberTotals
  +Money TipAmount
  +CouponId? AppliedCouponId
  +Create(UserId hostUserId, RestaurantId restaurantId, string hostName, DateTime? deadline)
  +AddMember(UserId userId, string name, MemberRole role)
  +SetDeadline(UserId requestingUserId, DateTime deadline)
  +IsExpired()
  +AddItem(UserId userId, MenuItemId menuItemId, MenuCategoryId menuCategoryId, ...)
  +UpdateItemQuantity(UserId requestingUserId, TeamCartItemId itemId, int newQuantity)
  +RemoveItem(UserId requestingUserId, TeamCartItemId itemId)
  +LockForPayment(UserId requestingUserId)
  +FinalizePricing(UserId requestingUserId)
  +MarkAsExpired()
  +ValidateJoinToken(string token)
  +CommitToCashOnDelivery(UserId userId, Money amount)
  +RecordSuccessfulOnlinePayment(UserId userId, Money amount, string transactionId)
  +RecordFailedOnlinePayment(UserId userId, Money amount)
  +ApplyTip(UserId requestingUserId, Money tipAmount)
  +ApplyCoupon(UserId requestingUserId, CouponId couponId)
  +RemoveCoupon(UserId requestingUserId)
  +ComputeQuoteLite(IReadOnlyDictionary~UserId, Money~ memberItemSubtotals, Money feesTotal, ...)
  +GetMemberQuote(UserId userId)
  +MarkAsConverted()
}
```

### 2.3 Restaurant (Aggregate root)
Nguồn: `src/Domain/RestaurantAggregate/Restaurant.cs`

Vai trò: Quản lý hồ sơ nhà hàng và các quy tắc trạng thái (xác thực, mở/ đóng nhận đơn).

Thuộc tính chính:
- `Name`, `LogoUrl`, `BackgroundImageUrl`, `Description`, `CuisineType`
- `Location`, `GeoCoordinates`, `ContactInfo`, `BusinessHours`
- `IsVerified`, `IsAcceptingOrders`
- Thuộc tính audit và soft delete

Phương thức nghiệp vụ chính:
- Tạo mới: `Create(...)` (kiểm tra dữ liệu vào, tạo value object)
- Vòng đời: `Verify`, `AcceptOrders`, `DeclineOrders`, `MarkAsDeleted`
- Cập nhật chi tiết: `ChangeName`, `UpdateDescription`, `ChangeCuisineType`, `UpdateLogo`, `UpdateBackgroundImage`, `ChangeLocation`, `ChangeGeoCoordinates`, `UpdateContactInfo`, `UpdateBusinessHours`
- Cập nhật tổng hợp: `UpdateBranding`, `UpdateBasicInfo`, `UpdateCompleteProfile`

Sự kiện miền tiêu biểu:
- `RestaurantCreated`, `RestaurantVerified`, `RestaurantAcceptingOrders`, `RestaurantNotAcceptingOrders`, `RestaurantDeleted`
- `RestaurantNameChanged`, `RestaurantDescriptionChanged`, `RestaurantCuisineTypeChanged`, `RestaurantLocationChanged`, `RestaurantContactInfoChanged`, `RestaurantBusinessHoursChanged`, `RestaurantBrandingUpdated`, `RestaurantProfileUpdated`

Mermaid lớp:
```mermaid
classDiagram
class Restaurant {
  +string Name
  +string LogoUrl
  +string BackgroundImageUrl
  +string Description
  +string CuisineType
  +Address Location
  +GeoCoordinates? GeoCoordinates
  +ContactInfo ContactInfo
  +BusinessHours BusinessHours
  +bool IsVerified
  +bool IsAcceptingOrders
  +Create(string name, string? logoUrl, string? backgroundImageUrl, string description, ...)
  +Verify()
  +AcceptOrders()
  +DeclineOrders()
  +MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy)
  +ChangeName(string name)
  +UpdateDescription(string description)
  +ChangeCuisineType(string cuisineType)
  +UpdateLogo(string? logoUrl)
  +UpdateBackgroundImage(string? backgroundImageUrl)
  +ChangeLocation(Address location)
  +ChangeGeoCoordinates(double latitude, double longitude)
  +ChangeLocation(string street, string city, string state, string zipCode, string country)
  +UpdateContactInfo(ContactInfo contactInfo)
  +UpdateContactInfo(string phoneNumber, string email)
  +UpdateBusinessHours(BusinessHours businessHours)
  +UpdateBusinessHours(string hours)
  +UpdateBranding(string name, string? logoUrl, string description)
  +UpdateBasicInfo(string name, string description, string cuisineType)
  +UpdateCompleteProfile(string name, string description, string cuisineType, string? logoUrl, ...)
}
```

### 2.4 MenuItem (Aggregate root)
Nguồn: `src/Domain/MenuItemAggregate/MenuItem.cs`

Vai trò: Quản lý món ăn thuộc nhà hàng, tình trạng sẵn sàng, giá, thông tin tùy chọn.

Thuộc tính chính:
- `RestaurantId`, `MenuCategoryId`, `Name`, `Description`, `BasePrice`, `ImageUrl`, `IsAvailable`
- `DietaryTagIds`, `AppliedCustomizations`
- Thuộc tính audit và soft delete

Phương thức nghiệp vụ chính:
- Tạo mới: `Create(...)`
- Cập nhật: `UpdateDetails(...)`, `UpdatePrice`, `AssignToCategory`
- Trạng thái: `MarkAsAvailable`, `MarkAsUnavailable`, `ChangeAvailability`
- Tùy chọn: `AssignCustomizationGroup`, `RemoveCustomizationGroup`, `SetDietaryTags`
- Xóa mềm: `MarkAsDeleted(...)`

Sự kiện miền tiêu biểu:
- `MenuItemCreated`, `MenuItemDetailsUpdated`, `MenuItemPriceChanged`, `MenuItemAvailabilityChanged`, `MenuItemAssignedToCategory`, `MenuItemCustomizationAssigned`, `MenuItemCustomizationRemoved`, `MenuItemDietaryTagsUpdated`, `MenuItemDeleted`

Mermaid lớp:
```mermaid
classDiagram
class MenuItem {
  +RestaurantId RestaurantId
  +MenuCategoryId MenuCategoryId
  +string Name
  +string Description
  +Money BasePrice
  +string? ImageUrl
  +bool IsAvailable
  +IReadOnlyList~TagId~ DietaryTagIds
  +IReadOnlyList~AppliedCustomization~ AppliedCustomizations
  +Create(RestaurantId restaurantId, MenuCategoryId menuCategoryId, string name, string description, ...)
  +UpdateDetails(string name, string description)
  +UpdateDetails(string name, string description, Money basePrice, string? imageUrl)
  +UpdatePrice(Money newPrice)
  +AssignToCategory(MenuCategoryId newCategoryId)
  +MarkAsAvailable()
  +MarkAsUnavailable()
  +ChangeAvailability(bool isAvailable)
  +AssignCustomizationGroup(AppliedCustomization customization)
  +RemoveCustomizationGroup(CustomizationGroupId groupId)
  +SetDietaryTags(List~TagId~? tagIds)
  +MarkAsDeleted()
  +MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy)
}
```

## 3) Sơ đồ trình tự cho các use case tiêu biểu

### 3.1 Khởi tạo đơn hàng (Initiate Order)
Nguồn chính: `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommandHandler.cs`, `src/Domain/OrderAggregate/Order.cs`

Tóm tắt luồng:
- Người dùng gửi yêu cầu đặt hàng.
- Application layer kiểm tra quyền, kiểm tra nhà hàng, lấy danh sách món, kiểm tra tùy chọn.
- Tính phí và thuế, tạo payment intent nếu thanh toán online.
- Tạo Order trong Domain, lưu vào kho dữ liệu.

Mermaid trình tự:
```mermaid
sequenceDiagram
autonumber
actor Customer
participant API as Web/API
participant Handler as InitiateOrderCommandHandler
participant RestaurantRepo as IRestaurantRepository
participant MenuRepo as IMenuItemRepository
participant CouponRepo as ICouponRepository
participant Payment as IPaymentGatewayService
participant Financial as OrderFinancialService
participant OrderAR as Order
participant OrderRepo as IOrderRepository

Customer ->> API: Đặt hàng (items, payment, địa chỉ)
API ->> Handler: Handle(request)
Handler ->> RestaurantRepo: GetById(restaurantId)
RestaurantRepo -->> Handler: Restaurant
Handler ->> MenuRepo: GetByIds(menuItemIds)
MenuRepo -->> Handler: MenuItems
Handler ->> CouponRepo: GetByCode(couponCode)
CouponRepo -->> Handler: Coupon?
Handler ->> Financial: CalculateSubtotal / ValidateDiscount / CalculateFinalTotal
Financial -->> Handler: subtotal, discount, total
alt Online payment
  Handler ->> Payment: CreatePaymentIntent(total, metadata)
  Payment -->> Handler: paymentIntentId, clientSecret
end
Handler ->> OrderAR: Create(...)
OrderAR -->> Handler: Order
Handler ->> OrderRepo: AddAsync(order)
OrderRepo -->> Handler: OK
Handler -->> API: InitiateOrderResponse
API -->> Customer: Kết quả + clientSecret (nếu có)
```

### 3.2 Chuyển TeamCart thành Order
Nguồn chính: `src/Application/TeamCarts/Commands/ConvertTeamCartToOrder/ConvertTeamCartToOrderCommandHandler.cs`, `src/Domain/Services/TeamCartConversionService.cs`

Tóm tắt luồng:
- Kiểm tra TeamCart ở trạng thái ReadyToConfirm, kiểm tra QuoteVersion.
- Tính phí, thuế, giảm giá (nếu có coupon).
- Domain service chuyển đổi TeamCart thành Order và cập nhật trạng thái TeamCart.
- Lưu Order và TeamCart.

Mermaid trình tự:
```mermaid
sequenceDiagram
autonumber
actor Host
participant API as Web/API
participant Handler as ConvertTeamCartToOrderCommandHandler
participant CartRepo as ITeamCartRepository
participant CouponRepo as ICouponRepository
participant Financial as OrderFinancialService
participant Converter as TeamCartConversionService
participant OrderRepo as IOrderRepository

Host ->> API: Yêu cầu chuyển TeamCart thành Order
API ->> Handler: Handle(request)
Handler ->> CartRepo: GetById(teamCartId)
CartRepo -->> Handler: TeamCart
Handler ->> CouponRepo: GetById(appliedCouponId)
CouponRepo -->> Handler: Coupon?
Handler ->> Financial: ValidateAndCalculateDiscountForTeamCartItems
Financial -->> Handler: discount
Handler ->> Converter: ConvertToOrder(cart, address, discount, fee, tax)
Converter -->> Handler: (Order, TeamCart)
Handler ->> CartRepo: UpdateAsync(teamCart)
Handler ->> OrderRepo: AddAsync(order)
OrderRepo -->> Handler: OK
Handler -->> API: ConvertTeamCartToOrderResponse
API -->> Host: Kết quả
```

### 3.3 Cập nhật trạng thái đơn hàng (Accept -> Prepare -> Deliver)
Nguồn chính: `src/Domain/OrderAggregate/Order.cs` và các event handler liên quan.

Tóm tắt luồng:
- Nhân viên nhà hàng cập nhật trạng thái đơn hàng.
- Domain kiểm tra trạng thái hợp lệ, cập nhật và phát sự kiện miền.
- Application layer xử lý sự kiện (gửi realtime, thông báo, cập nhật read model).

Mermaid trình tự:
```mermaid
sequenceDiagram
autonumber
actor Staff
participant API as Web/API
participant Handler as OrderStatusCommandHandler
participant OrderRepo as IOrderRepository
participant OrderAR as Order
participant EventHandler as OrderEventHandlers

Staff ->> API: Cập nhật trạng thái đơn hàng
API ->> Handler: Handle(command)
Handler ->> OrderRepo: GetById(orderId)
OrderRepo -->> Handler: Order
Handler ->> OrderAR: Accept/MarkAsPreparing/MarkAsReadyForDelivery/MarkAsDelivered
OrderAR -->> Handler: Result + DomainEvents
Handler ->> OrderRepo: UpdateAsync(order)
Handler -->> EventHandler: Xử lý sự kiện (thông báo, realtime)
Handler -->> API: Kết quả
API -->> Staff: Xác nhận
```

## 4) Gợi ý trình bày trong báo cáo
- Mỗi lớp nên trình bày theo cấu trúc: vai trò, thuộc tính chính, phương thức nghiệp vụ, ràng buộc và sự kiện miền.
- Mỗi biểu đồ cần có đoạn văn giải thích ngắn về mục tiêu và ý nghĩa.
