# API Design: Menu Item Details + Availability (v1)

Mục tiêu: Bổ sung 2 API công khai để client render trang chi tiết món và kiểm tra khả dụng nhanh, tuân thủ chuẩn hiện có (versioning, CQRS, caching, contract tests).

- Endpoint 1: GET `/api/v1/restaurants/{restaurantId}/menu-items/{itemId}` — trả về chi tiết đầy đủ 1 món.
- Endpoint 2: GET `/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability` — trả về trạng thái khả dụng/tồn kho nhanh với cache ngắn.

Tài liệu này mô tả hợp đồng JSON, nguồn dữ liệu, chiến lược cache, và lộ trình implement theo kiến trúc Clean Architecture + CQRS của YummyZoom.

## Kiến trúc & Quy chuẩn liên quan
- Versioning: URL segment v1, nhóm theo `EndpointGroupBase`, map qua `MapVersionedEndpoints()`; tham chiếu Docs/API-Design/API_Versioning.md.
- Web layer: Minimal API, nhóm route theo class, chuẩn response `WithStandardResults<T>()`; tham chiếu `src/Web/Infrastructure/EndpointExtensions.cs`.
- Application layer: Query dùng Dapper + SQL trực tiếp (CQRS); tham chiếu Docs/Development-Guidelines/Application_Layer_Guidelines.md.
- Caching:
  - Server-side: Cache-aside qua `ICacheableQuery<T>` + `CachingBehaviour`; TTL hợp lý, key có version; tham chiếu Docs/Development-Guidelines/Caching_Guide.md.
  - HTTP caching: ETag yếu + Last-Modified + Cache-Control cho payload công khai nặng; helper `HttpCaching` hiện có.
- Read models hiện có có thể tận dụng:
  - `FullMenuViews` (JSON menu + `LastRebuiltAt`) để tính ETag/Last-Modified nhanh và invalidation thống nhất.
  - `MenuItemSalesSummaries` cho `soldCount` (lifetime/rolling).
  - `RestaurantReviewSummaries` cho rating tổng (restaurant-level).

## Hợp đồng API (đề xuất)

### 1) GET /api/v1/restaurants/{restaurantId}/menu-items/{itemId}
- Authorization: Public
- Mục đích: Render trang chi tiết món với nhóm tuỳ chọn (customizations).
- HTTP caching: Hỗ trợ `ETag` (weak) và `Last-Modified` (ưu tiên dựa trên `FullMenuViews.LastRebuiltAt`).

Response 200 (application/json):
```json
{
  "restaurantId": "r123",
  "itemId": "i456",
  "name": "Bún đậu đầy đủ",
  "description": "Mẹt bún đậu …",
  "imageUrl": "https://...",
  "basePrice": 45000,
  "currency": "VND",
  "isAvailable": true,
  "soldCount": 1400,
  "rating": 4.6,
  "reviewCount": 120,
  "customizationGroups": [
    {
      "groupId": "addons",
      "name": "Gọi thêm",
      "type": "multi",           
      "required": false,
      "min": 0,
      "max": 10,
      "items": [
        {"id":"addon1","name":"Dồi sụn","priceDelta":10000,"default":false,"outOfStock":false},
        {"id":"addon2","name":"Chả cốm","priceDelta":10000}
      ]
    },
    {
      "groupId": "sauce",
      "name": "Nước chấm",
      "type": "radio",
      "required": true,
      "items": [
        {"id":"s1","name":"Mắm tôm","priceDelta":0,"default":true},
        {"id":"s2","name":"Nước mắm","priceDelta":0}
      ]
    }
  ],
  "notesHint": "Cho quán biết thêm về yêu cầu của bạn.",
  "limits": {"minQty":1, "maxQty":99},
  "upsell": [
    {"itemId":"i789","name":"Nem rán","price":10000,"imageUrl":"..."}
  ],
  "etag": "W/\"abc123\"",
  "lastModified": "2025-10-30T12:00:00Z"
}
```

Lưu ý hợp đồng:
- `rating`/`reviewCount`: hiện có dữ liệu mức nhà hàng; item-level rating để mở rộng sau. Rating của item cho bằng rating nhà hàng.
- `soldCount`: lấy từ `MenuItemSalesSummaries.LifetimeQuantity` (nếu null → 0).
- `type`: "radio" khi `MinSelections=1 && MaxSelections=1`; "multi" ngược lại.
- `required`: `MinSelections > 0`.
- `priceDelta`: từ `CustomizationChoices.PriceAdjustment_*` (đổi tiền về đơn vị của item/cửa hàng nếu cần; v1 giữ nguyên currency đồng nhất với item).
- `outOfStock`: hiện chưa quản trị tồn kho ở nhóm/choice → luôn `false` (để ngỏ mở rộng).
- `etag`/`lastModified`: phản ánh trạng thái rebuilt menu gần nhất hoặc `max(LastModified)` liên quan.

Mã lỗi: chuẩn `WithStandardResults<T>()` (400/404/409/500) + 304 Not Modified khi validator match.

### 2) GET /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability
- Authorization: Public
- Mục đích: Trả về khả dụng nhanh cho UI (poll hoặc refetch định kỳ).
- Server-side caching: TTL ngắn 10–30s (đề xuất 15s). Optional HTTP `Cache-Control: public, max-age=15`.

Response 200:
```json
{
  "restaurantId": "r123",
  "itemId": "i456",
  "isAvailable": true,
  "stock": null,
  "checkedAt": "2025-10-30T12:00:00Z",
  "ttlSeconds": 15
}
```

Ghi chú: `stock` hiện chưa khả dụng → `null`; mở rộng khi có inventory.

## Nguồn dữ liệu & ánh xạ
- Bảng chính: `MenuItems` (base fields, `IsAvailable`, `DietaryTagIds`, `AppliedCustomizations` JSON, `LastModified`).
- Customizations:
  - `AppliedCustomizations` (JSON trên MenuItems) → danh sách group áp dụng cho item (id + display order/name).
  - `CustomizationGroups` join theo các id trên, lấy `GroupName`, `MinSelections`, `MaxSelections`.
  - `CustomizationChoices` join theo group, lấy `Name`, `PriceAdjustment_*`, `IsDefault`, `DisplayOrder`.
- Bán chạy: `MenuItemSalesSummaries` → `LifetimeQuantity`.
- Đánh giá: `RestaurantReviewSummaries` → `AverageRating`, `TotalReviews` (mức nhà hàng).
- Thời điểm sửa đổi: 
  - Phương án A (đơn giản, nhất quán): sử dụng `FullMenuViews.LastRebuiltAt` (đã rebuild khi item/customization đổi) để sinh ETag/Last-Modified.
  - Phương án B (granular): lấy `max(LastModified)` từ `MenuItems`, `CustomizationGroups`, `CustomizationChoices` liên quan.

## Caching chiến lược
- Details Query: `ICacheableQuery<Result<MenuItemPublicDetailsDto>>`
  - Key: `restaurant:{rid}:menu-item:v1:{iid}` (guid format `N`).
  - TTL: 2 phút. Tags: `restaurant:{rid}:menu`, `restaurant:{rid}:items` (mục tiêu cho invalidation theo event; trước mắt dựa TTL).
  - HTTP caching: ETag yếu + Last-Modified từ `FullMenuViews.LastRebuiltAt`, `Cache-Control: public, max-age=120`.
- Availability Query: `ICacheableQuery<Result<MenuItemAvailabilityDto>>`
  - Key: `restaurant:{rid}:menu-item-availability:v1:{iid}`.
  - TTL: 15s. Tag: `restaurant:{rid}:availability`.
  - HTTP header: `Cache-Control: public, max-age=15`. Không cần ETag (tối ưu hóa đơn giản), có thể thêm sau nếu cần.

## Kế hoạch implement

1) Application layer (Queries + DTOs)
- Thư mục: `src/Application/Restaurants/Queries/Public/GetMenuItemDetails/`
  - `GetMenuItemPublicDetailsQuery.cs` (implements `ICacheableQuery<Result<MenuItemPublicDetailsDto>>`).
  - `GetMenuItemPublicDetailsQueryHandler.cs` (Dapper; joins và build DTO; tính lastModified; đọc `FullMenuViews` để dựng ETag nếu cần trong Web).
  - `GetMenuItemPublicDetailsQueryValidator.cs` (validate Guid not empty).
  - DTOs: `MenuItemPublicDetailsDto`, `CustomizationGroupDto`, `CustomizationChoiceDto`, `UpsellSuggestionDto`.
- Thư mục: `src/Application/Restaurants/Queries/Public/GetMenuItemAvailability/`
  - `GetMenuItemAvailabilityQuery.cs` (ICacheableQuery với TTL 15s).
  - `GetMenuItemAvailabilityQueryHandler.cs` (dùng `IMenuItemRepository.IsAvailableAsync` hoặc Dapper để check `MenuItems.IsAvailable` và `Restaurants.IsAcceptingOrders`).
  - `GetMenuItemAvailabilityQueryValidator.cs`.

2) Web layer (Endpoints)
- Cập nhật `src/Web/Endpoints/Restaurants.cs` (nhóm public):
  - GET `/{restaurantId:guid}/menu-items/{itemId:guid}` → gọi `GetMenuItemPublicDetailsQuery`.
    - Set `ETag`, `Last-Modified`, `Cache-Control: public, max-age=120`; trả 304 nếu match (dùng `HttpCaching`).
  - GET `/{restaurantId:guid}/menu-items/{itemId:guid}/availability` → gọi `GetMenuItemAvailabilityQuery`.
    - Set `Cache-Control: public, max-age=15`.
  - Áp dụng `.MapToApiVersion(1, 0)` theo hướng dẫn versioning.
  - `.WithStandardResults<T>()` phù hợp; production shapes tuân thủ template lỗi chuẩn.

3) Tests
- Contract tests (`tests/Web.ApiContractTests`):
  - Path, status codes, header validators (`ETag`, `Last-Modified`, `Cache-Control`).
  - Schema fields có/không bắt buộc, kiểu dữ liệu, enum `type` (radio|multi).
- Functional tests (`tests/Application.FunctionalTests`):
  - Details: cache hit/miss, ETag 304.
  - Availability: TTL 15s; thay đổi `IsAvailable` → cache chưa hết TTL vẫn trả giá trị cũ; sau TTL trả mới.

4) Observability
- Log timing của handler; hit/miss cache (`CachingBehaviour` đã log mức Debug/Warning).
- Metric roadmap: hit/miss/eviction (như Caching_Guide đã đề cập).

5) Rollout & Backward compatibility
- Đây là bổ sung non-breaking cho v1; không cần bump version.
- Khi thay đổi shape sau này → bump key `v2` và cân nhắc `v2` API nếu breaking.

## Quy tắc & chỗ còn mở
- `upsell`: v1 tính đơn giản theo cùng category, ưu tiên `Rolling30DayQuantity` hoặc `LifetimeQuantity`, giới hạn 3 item. Sau mở rộng sang collaborative filtering.
- `rating`/`reviewCount`: v1 dùng mức nhà hàng; item-level rating đưa vào roadmap.
- `notesHint`/`limits`: v1 cố định từ config/appsettings; có thể override theo nhà hàng trong tương lai.
- Tồn kho: chưa có schema, `stock=null`. Khi có inventory, bổ sung read model + invalidation.

## Phụ lục: Bảng/Model tham chiếu nhanh
- `MenuItems` (Id, RestaurantId, MenuCategoryId, Name, Description, BasePrice_*, ImageUrl, IsAvailable, DietaryTagIds JSON, AppliedCustomizations JSON, LastModified)
- `CustomizationGroups` (Id, RestaurantId, GroupName, MinSelections, MaxSelections, IsDeleted, LastModified)
- `CustomizationChoices` (PK (CustomizationGroupId, ChoiceId), Name, PriceAdjustment_*, IsDefault, DisplayOrder)
- `MenuItemSalesSummaries` (RestaurantId, MenuItemId, LifetimeQuantity, ...)
- `RestaurantReviewSummaries` (RestaurantId, AverageRating, TotalReviews)
- `FullMenuViews` (RestaurantId, MenuJson, LastRebuiltAt)

