# Future Plan: Menu Item Detail + Availability API

Ngày: 2025-10-31
Phạm vi: Web (Endpoints), Application (Queries/DTOs), Docs, Tests
Tác động: Non-breaking (v1)

## Mục tiêu
- Thêm 2 endpoint công khai:
  - GET `/api/v1/restaurants/{restaurantId}/menu-items/{itemId}` (chi tiết món)
  - GET `/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability` (khả dụng nhanh)
- Tuân thủ chuẩn versioning, caching, contract testing.

## Deliverables
- Application queries + handlers (Dapper) với cache-aside:
  - `GetMenuItemPublicDetailsQuery` (TTL 2m)
  - `GetMenuItemAvailabilityQuery` (TTL 15s)
- Web endpoints trong `Restaurants` group (public) với header caching hợp lý.
- Docs:
  - API design: `Docs/API-Design/MenuItem-Details-and-Availability-API.md`
  - API reference bổ sung (sau khi chốt): `Docs/API-Documentation/API-Reference/Customer/02-Restaurants-and-Menus.md` (update mục Menu Item Detail)
- Tests: Contract + Functional cơ bản.

## Kế hoạch thực hiện (Checklist)
- [ ] Application: Create folder `Queries/Public/GetMenuItemDetails` (+ Query/Handler/Validator/DTOs)
- [ ] Application: Create folder `Queries/Public/GetMenuItemAvailability` (+ Query/Handler/Validator/DTOs)
- [ ] SQL: Viết Dapper SQL cho details (joins MenuItems, CustomizationGroups, CustomizationChoices, MenuItemSalesSummaries, RestaurantReviewSummaries)
- [ ] Web: Map GET details + availability vào `src/Web/Endpoints/Restaurants.cs` (public group, v1)
- [ ] Web: Thêm ETag/Last-Modified cho details, Cache-Control cho cả 2
- [ ] Tests: Web.ApiContractTests cho 2 endpoint (200/404, headers, shape)
- [ ] Tests: Application.FunctionalTests cho cache TTL + ETag 304
- [ ] Docs: Cập nhật API-Documentation reference sau khi merge

## Rủi ro & Phương án
- Customization groups nhiều → payload lớn: HTTP caching (ETag/304) + TTL 2m để giảm băng thông.
- Inventory chưa có: trả `stock = null`; bổ sung sau (read model + invalidation tags).
- Rating theo item chưa có: dùng rating mức nhà hàng; flag TODO cho chuyển đổi khi có dữ liệu.

## Timeline gợi ý
- Ngày 1: Queries/DTOs + SQL + unit test nhẹ
- Ngày 2: Web endpoints + contract tests
- Ngày 3: Functional tests (cache/etag) + docs API-Documentation
- Ngày 4: Review/Refactor + chuẩn hoá thông số cache + rollout

## Tham chiếu
- Caching Guide: `Docs/Development-Guidelines/Caching_Guide.md`
- App Layer: `Docs/Development-Guidelines/Application_Layer_Guidelines.md`
- API Versioning: `Docs/API-Design/API_Versioning.md`
- Helpers: `src/Web/Infrastructure/Http/HttpCaching.cs`, `src/Web/Infrastructure/EndpointExtensions.cs`

