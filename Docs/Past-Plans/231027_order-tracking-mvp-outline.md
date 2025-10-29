# Order Tracking – Push-first, Poll-as-needed (MVP)

Ngày: 2025-10-27
Tác giả: Backend + Client App
Phạm vi: Đồng bộ backend hiện tại với yêu cầu trong Docs/Jot-down.md để hỗ trợ tracking tình trạng order từ mobile app theo mô hình “Push-first, Poll-as-needed”. Tập trung MVP, hoãn các phần phức tạp.

## Progress Update (2025-10-27)
- Added per-order Version in domain and DB (EF mapping + migration), and incremented on user-visible changes.
- Exposed version in GET /api/v1/orders/{id}/status via OrderStatusDto and query handler.
- Implemented ETag for GET /api/v1/orders/{id}/status using Version.
  - Computes ETag: "order-<id>-v<version>" and Last-Modified from LastUpdateTimestamp.
  - Returns 304 when If-None-Match matches.
- Adjusted tests to account for the new version field.
- Build is green across projects.

- Implemented FCM data-only pushes from order event handlers with {orderId, version} and added Android/APNs delivery hints.
  - Enhanced IFcmService with SendMulticastDataAsync; updated FcmService for Android priority/TTL/collapse and APNs background headers.
  - Updated functional tests for OrderAccepted and OrderRejected handlers to verify FCM data push.

Impact on plan:
- ETag should now be computed from version (prefer `order-<id>-v<version>`), not timestamp.
- The Redis optimization (if any) remains optional as a cache for fast 304; source of truth is DB Version.

**Mục Tiêu MVP**
- Push dữ liệu nhẹ qua FCM khi đơn hàng đổi trạng thái để kích hoạt cập nhật phía client.
- Client luôn gọi GET `/api/v1/orders/{id}/status` trước khi hiển thị để có nguồn dữ liệu chuẩn.
- Hỗ trợ conditional responses (ETag) để giảm tải khi polling (304 khi không đổi).
- Đảm bảo độ tin cậy bằng outbox + retry (tận dụng hạ tầng sẵn có).

---

**Hiện Trạng Backend (đã có)**
- API Orders
  - `POST /api/v1/orders/initiate`, lifecycle: accept/reject/cancel/preparing/ready/delivered.
  - `GET /api/v1/orders/{id}`: chi tiết đầy đủ.
  - `GET /api/v1/orders/{id}/status`: payload gọn cho polling. DTO: `OrderStatusDto(OrderId, Status, LastUpdateTimestamp, EstimatedDeliveryTime)`
    - Mã nguồn: `src/Web/Endpoints/Orders.cs` (route) + `src/Application/Orders/Queries/GetOrderStatus/*` (handler/validator).
- Realtime (SignalR)
  - Hubs: `CustomerOrdersHub`, `RestaurantOrdersHub`. Broadcaster: `SignalROrderRealtimeNotifier`.
  - Event handlers domain cho các trạng thái: Placed, Accepted, Rejected, Cancelled, Preparing, ReadyForDelivery, Delivered, PaymentSucceeded/Failed.
- Outbox & Idempotency
  - Outbox messages + `OutboxProcessor` + hosted service → phát domain event ra MediatR có retry/backoff.
  - Interceptor tự động ghi domain events vào Outbox.
  - Idempotency middleware cho các lệnh POST quan trọng (đã dùng trong initiate, team cart...).
- Push infrastructure
  - `IFcmService` + `FcmService` (Firebase Admin SDK đã wiring). Có `SendNotificationAsync` và `SendDataMessageAsync`.
  - Đăng ký thiết bị: `Users /devices/register|unregister`, repo `UserDeviceSessionRepository` lưu token theo `userId`, truy vấn được tất cả token hoạt động của user.
- Bảo mật
  - `GET /orders/{id}/status` kiểm tra owner hoặc staff/owner restaurant, che 404 khi không có quyền.

Kết luận: nền tảng cốt lõi (status API, Outbox, event handlers, FCM service, registry token) đã sẵn để hiện thực mô hình Push-first, Poll-as-needed mà không cần thay đổi kiến trúc lớn.

---

**Khoảng Cách So Với Yêu Cầu (Docs/Jot-down.md)**
- Version đơn điệu cho dedupe phía client: Chưa có trường `version (int)` trong Order; hiện có `LastUpdateTimestamp` (tăng đơn điệu theo mỗi thay đổi trạng thái).
- Conditional responses: Chưa phát ETag/If-None-Match hoặc `sinceVersion` cho `/status` (handler có TODO cho ETag).
- FCM push on every state change: Event handlers hiện chỉ broadcast qua SignalR; chưa gửi FCM data `{orderId, version}` cho user.
- Cấu hình FCM data push:
  - Chưa set collapse-id/TTL/platform flags cho data-only (Android priority/ttl, iOS `content-available=1`, `apns-push-type=background`, `apns-collapse-id`).
- Topic per-order: Chưa cần cho MVP (hiện có direct token theo user, đủ dùng).
- Rate limiting `/status`: Chưa có hạn mức chuyên biệt (có thể bổ sung nhẹ sau khi có ETag).

---

**Đề Xuất Phương Án MVP**
1) “Version” sử dụng ngay `LastUpdateTimestamp`
- Định nghĩa tạm: `version = LastUpdateTimestampTicks` (UTC ticks hoặc epoch ms), tăng đơn điệu cùng transaction cập nhật trạng thái.
- Gửi `version` này trong FCM data và dùng để so sánh trên client (bỏ qua thông báo có `version` ≤ local).
- Giai đoạn sau có thể chuyển sang cột `Version (int)` thực thụ nếu cần.

2) ETag cho `/status`
- ETag tính từ `orderId + version` (ví dụ: `"order-<id>-v<version>"`).
- Hỗ trợ `If-None-Match` → trả `304 Not Modified` khi không đổi. Trả kèm `ETag`, `Last-Modified`, `Cache-Control: no-cache` trong 200.
- Tạm thời chưa cần `sinceVersion` (có thể thêm, nhưng ETag đủ cho MVP và đơn giản hơn).

3) FCM data push trên mọi thay đổi trạng thái
- Mỗi event handler Orders thêm bước gửi FCM data tới tất cả thiết bị hoạt động của customer:
  - Payload: `{ "orderId": "<guid>", "version": "<ticks>" }` (stringified ints OK cho tính tương thích).
  - Android: `priority=high`, `collapse_key=order_<id>`, `ttl≈300s`.
  - iOS/APNs: headers `apns-push-type=background`, `content-available=1`, `apns-collapse-id=order_<id>`, expiry ngắn.
- Dựa trên Outbox→MediatR→EventHandler sẵn có: nếu gửi FCM lỗi, handler ném lỗi để Outbox retry an toàn (idempotent).

4) Giữ nguyên SignalR cho dashboard/web
- Không thay đổi hợp đồng SignalR, chỉ bổ sung FCM cho mobile. Trên client mobile, ưu tiên Push→Poll; rơi về Poll-only khi không có token.

5) Quan sát & logging
- Log gửi FCM theo `orderId`, `userId`, số token, kết quả thành công/thất bại; đánh dấu token invalid khi FCM trả `Unregistered/InvalidArgument` (đã hỗ trợ trong `FcmService`).

---

**Outline Công Việc (Ưu Tiên MVP)**
1. Status ETag (API)
- [Code] `src/Application/Orders/Queries/GetOrderStatus/GetOrderStatusQueryHandler.cs`
  - Tính `etag = order-<id>-v<version>` từ `Version`.
  - Đọc `If-None-Match`, trả `304` khi trùng.
  - Trả header: `ETag`, `Last-Modified`, `Cache-Control` trong `200`.
- [Tests] `tests/Web.ApiContractTests/Orders/GetOrderStatusContractTests.cs`
  - Thêm test: lần 1 nhận `200 + ETag`; lần 2 kèm `If-None-Match` → `304`.

2. FCM Data Push cho Order Events
- [Code] Bổ sung vào từng handler tại `src/Application/Orders/EventHandlers/*`
  - Inject `IUserDeviceSessionRepository`, `IFcmService`.
  - Lấy token theo `order.CustomerId` → lặp gửi `SendDataMessageAsync(token, {orderId, version})`.
  - Cấu hình Android/APNs cho data-only trong `FcmService.SendDataMessageAsync` (priority, ttl, collapse key/id, content-available).
  - Nếu gửi lỗi có thể throw để Outbox retry (giữ idempotency).
- [Infra] `src/Infrastructure/Notifications/Firebase/FcmService.cs`
  - Sửa `SendDataMessageAsync`:
    - Set `AndroidConfig { Priority=High, CollapseKey=$"order_<id>", TimeToLive=… }`.
    - Set `Apns.Headers { apns-push-type=background, apns-expiration=…, apns-collapse-id=… }` và `Aps { ContentAvailable = true }`.
- [Tests]
  - Functional test: giả lập outbox→handler, stub `IFcmService` để assert đã gọi đúng payload và số lần gửi.

3. Chuẩn hóa payload và bảo mật
- [Spec] Chỉ gửi `{orderId, version}` (không PII); không chứa chi tiết đơn hàng.
- [Code] Đảm bảo handler không log nội dung PII trong payload.

4. Tối ưu client-facing `/status` (không bắt buộc nhưng nhẹ)
- [Code] Đảm bảo truy vấn lean (đang dùng Dapper, đủ nhanh). Bật gzip/br ở Web defaults (đã có theo project defaults, kiểm tra `Program.cs` nếu cần).

5. Tài liệu + OpenAPI
- [Docs] Cập nhật mô tả endpoint `/orders/{id}/status` về ETag/304, ví dụ response headers.
- [Docs] Ghi chú chiến lược Push-first, Poll-as-needed cho mobile.

---

**Chấp Nhận (Acceptance Criteria – MVP)**
- Nhận được FCM data `{orderId, version}` ngay khi trạng thái thay đổi (ít nhất: Placed, Accepted, Preparing, ReadyForDelivery, Delivered, Cancelled/Rejected, PaymentSucceeded/Failed).
- Client gọi `/status` và nếu không đổi với ETag → trả `304` trong ≤ 5 ms truy vấn DB (mục tiêu) trên môi trường nội bộ.
- Khi cùng một trạng thái được re-emit do retry, client bỏ qua nếu `version` ≤ local.
- Token invalid được đánh dấu `inactive` tự động.

---

**Hoãn Sau MVP (Nice-to-have)**
- Trường `Version (int)` thật trong bảng `Orders` (tăng atomically cùng trạng thái); unique `(orderId, version)`; expose `sinceVersion` query param.
- Per-order topic (FCM topic) khi fan-out lớn hoặc cần ACL theo phòng theo dõi.
- Rate limiting mềm cho `/status` theo user/order.
- Hybrid push (notification + data) cho các mốc quan trọng (ArrivingSoon/Delivered) với iOS NSE.
- Read-model riêng cho `/status` (materialized view) khi traffic cao.

---

**Rủi Ro & Giảm Thiểu**
- Đồng hồ hệ thống không đơn điệu tuyệt đối → dùng `LastUpdateTimestamp` có thể trùng khi hai thay đổi trong cùng tick.
  - Giảm thiểu: thực tế các thay đổi trạng thái nối tiếp; nếu cần, nâng cấp lên `Version (int)` sau MVP.
- Fan-out nhiều token cho 1 user → kiểm tra batch/multicast sau MVP; MVP gửi tuần tự hoặc theo nhóm nhỏ.

---

**Điểm Móc Mã Nguồn (tham khảo nhanh)**
- Status API: `src/Web/Endpoints/Orders.cs`, `src/Application/Orders/Queries/GetOrderStatus/*`, `src/Application/Orders/Queries/Common/OrderDtos.cs`.
- Event handlers: `src/Application/Orders/EventHandlers/*`.
- Outbox: `src/Infrastructure/Messaging/Outbox/*`, interceptor: `src/Infrastructure/Persistence/EfCore/Interceptors/ConvertDomainEventsToOutboxInterceptor.cs`.
- FCM: `src/Application/Common/Interfaces/IServices/IFcmService.cs`, `src/Infrastructure/Notifications/Firebase/FcmService.cs`.
- User tokens: `src/Infrastructure/Persistence/Repositories/UserDeviceSessionRepository.cs`.

---

**Kế Hoạch Thực Thi (đề xuất 2–3 ngày công)**
- Ngày 1: ETag cho `/status` + tests; chỉnh `FcmService` cho data-only flags.
- Ngày 2: Bổ sung gửi FCM vào các Order event handlers chính + tests stub `IFcmService`.
- Ngày 3: Dọn tài liệu, OpenAPI, thông số TTL/collapse theo môi trường.

> Ghi chú: Giữ nguyên SignalR cho web/ops; mobile đi theo Push-first → Poll-as-needed, tương thích tốt với kiến trúc hiện tại.



