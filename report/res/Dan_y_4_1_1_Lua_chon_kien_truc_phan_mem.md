# Dàn ý 4.1.1 Lựa chọn kiến trúc phần mềm (YummyZoom)

## Mục tiêu và phạm vi (theo template)
- Độ dài 1-3 trang; giải thích ngắn gọn về kiến trúc được chọn và cách áp dụng vào hệ thống cụ thể.
- Tập trung vào kiến trúc phần mềm (Clean Architecture + DDD, có nhắc CQRS như một quy ước triển khai).
- Mô tả rõ "áp dụng lý thuyết vào sản phẩm": thành phần lớp/bộ phận tương ứng trong hệ thống thực tế.

## Dàn ý đề xuất (chi tiết)
1) Mở đầu và lý do đặt vấn đề
   - Bối cảnh: YummyZoom có nghiệp vụ phức tạp (TeamCart, thanh toán, báo cáo), đa kênh client (mobile, web, admin) và cần độ tin cậy cao.
   - Mục tiêu kiến trúc: tách biệt nghiệp vụ với hạ tầng, dễ mở rộng/kiểm thử, tránh phụ thuộc chặt vào DB/UI.

2) Lựa chọn kiến trúc: Clean Architecture + DDD (có CQRS)
   - Clean Architecture: quy tắc phụ thuộc hướng tâm, Domain ở trung tâm; các lớp ngoài không chi phối domain.
   - DDD: mô hình hóa nghiệp vụ bằng Aggregate/Entity/Value Object/Domain Event; dùng ngôn ngữ thống nhất theo miền.
   - CQRS: quy ước tách đọc/ghi để tối ưu hiệu năng và làm rõ trách nhiệm use case.

3) Phạm vi kiến trúc hiện tại (monolithic modular) và khả năng mở rộng
   - Hiện tại: triển khai đơn khối nhưng tách theo module/layer rõ ràng (Domain, Application, Infrastructure, Web).
   - Khả năng mở rộng: có thể tách dần theo module thành Microservices khi quy mô lớn hơn, do ranh giới miền đã được xác định.
   - Lý do không chọn Microservices ngay: tối ưu chi phí vận hành và thời gian phát triển trong phạm vi đồ án.

4) Áp dụng vào YummyZoom: mapping từ lý thuyết sang mã nguồn
   - Lớp Domain (nghiệp vụ cốt lõi, độc lập hạ tầng):
     - Ví dụ mã: `src/Domain/TeamCartAggregate/TeamCart.cs`, `src/Domain/Common/ValueObjects/Money.cs`,
       `src/Domain/Common/Models/Entity.cs`, `src/Domain/Common/Models/DomainEventBase.cs`.
   - Lớp Application (use case, orchestration, policy):
     - Command/Query + handler, validation/authorization pipeline.
     - Ví dụ: `src/Application/TeamCarts/Commands/AddItemToTeamCart/AddItemToTeamCartCommandHandler.cs`,
       `src/Application/Common/Behaviours/ValidationBehaviour.cs`.
   - Lớp Infrastructure (triển khai kỹ thuật):
     - Data access, outbox, external services.
     - Ví dụ: `src/Infrastructure/Persistence/DbConnectionFactory.cs`,
       `src/Infrastructure/Persistence/EfCore/Interceptors/ConvertDomainEventsToOutboxInterceptor.cs`.
   - Lớp Web/Presentation (endpoint mỏng):
     - Chỉ chuyển yêu cầu vào Application, trả kết quả chuẩn hóa.
     - Ví dụ: `src/Web/Endpoints/Restaurants.Dashboard.cs`.

5) Bounded Context trong domain (trình bày chính bằng sơ đồ)
   - Trình bày sơ đồ tổng thể các bounded context là chính.
   - Phần chữ chỉ nêu ngắn gọn tên các context để dẫn vào sơ đồ.

6) Mức độ áp dụng DDD trong code thực tế (ưu tiên ví dụ TeamCart)
   - Trình bày đầy đủ các khối: Aggregate Root, Entity con, Value Object, Domain Event, Invariants.
   - Ví dụ TeamCart:
     - Aggregate Root: `TeamCart` quản lý trạng thái cart, thành viên, item, thanh toán.
     - Value Object: `Money`, `ShareableLinkToken`.
     - Domain Event: `TeamCartCreated`, `ItemAddedToTeamCart`, `TeamCartLockedForPayment`.
     - Invariants: mỗi cart có Host, chỉ Host sửa tài chính, trạng thái kiểm soát theo vòng đời.

7) CQRS trong hệ thống (có nêu luồng đọc/ghi)
   - Đọc: query handler dùng Dapper/SQL thuần để lấy read model hiệu năng cao
     (vd `GetRestaurantDashboardSummaryQuery`).
   - Ghi: command handler dùng repository + unit of work, thao tác aggregate trong Domain.
   - Lợi ích: tối ưu hiệu năng đọc, tránh phụ thuộc trực tiếp vào mô hình ghi.

8) Domain Event và cơ chế xử lý bất đồng bộ (khái quát + 1 ví dụ)
   - Domain Event phát sinh trong Domain, được chuyển vào outbox ở Infrastructure, sau đó handler xử lý.
   - Tác dụng: giảm coupling, đảm bảo tính nhất quán cuối cùng (eventual consistency).
   - Ví dụ: `TeamCartCreated` được đẩy vào outbox và handler cập nhật read model/khởi tạo dữ liệu phục vụ hiển thị giỏ hàng nhóm.

9) Ưu điểm
   - Bảo trì/kiểm thử tốt nhờ tách lớp; domain độc lập DB/UI.
   - DDD làm rõ nghiệp vụ phức tạp; CQRS tối ưu hiệu năng.
   - Dễ mở rộng về sau (tách module thành service).

10) Nhược điểm/chi phí (nêu vừa đủ)
   - Tăng độ phức tạp, nhiều lớp và quy ước.
   - Thiết kế domain ban đầu tốn thời gian.
   - Cần kỷ luật nhất quán khi phát triển.

11) Kết luận ngắn
   - Kiến trúc được chọn cân bằng giữa chất lượng (bảo trì/kiểm thử) và chi phí triển khai trong phạm vi đồ án.

## Những điểm đáng chú ý (rút ra từ codebase)
- Domain sử dụng Aggregate Root + Value Object (vd `TeamCart`, `Money`), gom nghiệp vụ vào domain layer, hạn chế phụ thuộc DB/UI.
- Domain event được lưu và đẩy qua outbox để xử lý bất đồng bộ, giảm coupling (xem `ConvertDomainEventsToOutboxInterceptor`).
- Application dùng MediatR + pipeline behaviors (validation, authorization, logging), đóng vai trò "use case orchestration".
- CQRS rõ ràng: đọc dùng Dapper/SQL thuần (vd dashboard query), ghi thông qua repository + unit of work.
- Web layer mỏng, không chứa nghiệp vụ, chỉ chuyển tiếp yêu cầu và trả kết quả.

## Câu hỏi mở cần làm rõ khi viết 4.1.1
- Sơ đồ bounded context sẽ đặt ở mục 4.1.2 hay nhắc trước ngay trong 4.1.1?

## Cấu trúc chính thức xử lý nội dung (Finalized Structure)
Dựa trên các ý tưởng trên, đây là cấu trúc các mục con sẽ viết vào báo cáo (Mục 4.1.1):

### 1. Giới thiệu kiến trúc tổng thể
- **Tuyên bố:** Sử dụng Clean Architecture kết hợp Domain-Driven Design (DDD).
- **Mô hình triển khai:** Monolithic Modular (Đơn khối module hóa).
- **Lý do lựa chọn:**
  - Phù hợp quy mô nhóm và thời gian phát triển (tránh phức tạp của Microservices).
  - Giải quyết bài toán nghiệp vụ phức tạp của TeamCart (tách biệt core domain).
  - Dễ dàng tách thành Microservices trong tương lai nhờ ranh giới module rõ ràng.

### 2. Tổ chức các tầng kiến trúc (Layered Architecture Visualization)
Mô tả chi tiết cách ánh xạ từ lý thuyết Clean Architecture vào project structure của YummyZoom:
- **Core Layer (Domain):** Chứa các Business Rules, Entities (TeamCart, Order), Value Objects. Không phụ thuộc framework/DB.
- **Application Layer:** Chứa Use Cases (Command/Query handlers), Validation, Integration Interfaces. Điều phối luồng dữ liệu.
- **Infrastructure Layer:** Implement interfaces (EF Core Repositories, Redis Cache, Cloudinary Service, Email).
- **Presentation Layer (Web):** Minimal APIs, Controllers, SignalR Hubs. Chỉ làm nhiệm vụ nhận request và trả response.

### 3. Thiết kế chiến lược (Strategic Design - DDD)
- **Bounded Contexts:** Chia hệ thống thành các context logic:
  - *Identity Context:* Quản lý user, auth.
  - *Catalog Context:* Quản lý menu, nhà hàng.
  - *Ordering Context:* Xử lý đặt hàng, thanh toán.
  - *TeamCart Context:* Xử lý giỏ hàng nhóm (trọng tâm).
- Minh họa bằng sơ đồ Context Map (nếu có).

### 4. Thiết kế chiến thuật & Các mẫu kỹ thuật (Tactical Design & Patterns)
- **Domain Model Pattern:** Sử dụng Aggregate Root để đảm bảo tính nhất quán (VD: TeamCart Aggregate).
- **CQRS Pattern:**
  - *Write Side:* EF Core + Domain Logic (Consistency).
  - *Read Side:* Dapper/Raw SQL (Performance cho Dashboard).
- **Domain Events & Outbox Pattern:**
  - Xử lý tác vụ phụ (gửi mail, thông báo, cập nhật thống kê) bất đồng bộ.
  - Đảm bảo tính toàn vẹn dữ liệu giữa các module (Eventual Consistency).

### 5. Kết luận
- Khẳng định kiến trúc giúp hệ thống: Dễ bảo trì, Hiệu năng cao (nhờ CQRS/Caching), và Dễ mở rộng.
