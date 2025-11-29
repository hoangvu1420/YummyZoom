# Đề xuất Đặc tả Chức năng cho Báo cáo Đồ án YummyZoom

Tài liệu này phân tích và đề xuất 10 use case tiêu biểu nhất của hệ thống YummyZoom để đưa vào phần "Đặc tả chức năng" (Chương 2) của báo cáo đồ án tốt nghiệp.

## Tiêu chí lựa chọn
Các use case được lựa chọn dựa trên các tiêu chí sau:
1.  **Độ phức tạp và quan trọng:** Các chức năng cốt lõi thể hiện nghiệp vụ chính của hệ thống (Core Domain).
2.  **Tính bao phủ:** Đại diện cho các Actor chính (Khách hàng, Nhà hàng, Admin) và các module quan trọng (Onboarding, Menu, Order, TeamCart, Review).
3.  **Tính đặc thù:** Ưu tiên các tính năng đặc biệt của YummyZoom như **TeamCart** (Đặt hàng nhóm).
4.  **Sự sẵn có của tài liệu:** Dựa trên các biểu đồ phân rã đã có để đảm bảo tính nhất quán.

## Danh sách 10 Use Case Đề xuất

### Nhóm 1: Quản lý Nhà hàng & Onboarding (Restaurant & Admin)

#### 1. Đăng ký nhà hàng (Register Restaurant) ✅
*   **Mã UC:** UC-007 (theo diagram tổng quan)
*   **Tác nhân:** Chủ nhà hàng (Guest/New User)
*   **Mô tả:** Quy trình chủ nhà hàng cung cấp thông tin doanh nghiệp, giấy phép, thực đơn cơ bản để yêu cầu tham gia hệ thống.
*   **Lý do chọn:** Đây là quy trình phức tạp với nhiều trạng thái (Draft, Submitted, UnderReview), thể hiện logic nghiệp vụ chặt chẽ của module `RestaurantRegistrations`.
*   **Tham chiếu:** `usecase_restaurant_approval_decomposition.puml`

#### 2. Duyệt đăng ký nhà hàng (Approve Registration) ✅
*   **Mã UC:** UC-012
*   **Tác nhân:** Quản trị viên (Admin)
*   **Mô tả:** Admin xem xét hồ sơ đăng ký, yêu cầu bổ sung thông tin hoặc chấp thuận/từ chối.
*   **Lý do chọn:** Thể hiện quyền lực của Admin và quy trình kiểm duyệt chặt chẽ (Workflow).
*   **Tham chiếu:** `usecase_restaurant_approval_decomposition.puml`

#### 3. Quản lý thực đơn (Manage Menu) ✅
*   **Mã UC:** UC-008
*   **Tác nhân:** Chủ nhà hàng
*   **Mô tả:** Thêm, sửa, xóa món ăn, danh mục và các nhóm tùy chọn (topping, size).
*   **Lý do chọn:** Chức năng quản lý dữ liệu quan trọng nhất của nhà hàng. Cấu trúc Menu/MenuItem/CustomizationGroup trong Domain Design rất phong phú.
*   **Tham chiếu:** `usecase_menu_management_decomposition.puml`
*   **Trạng thái:** Đã hoàn thành đặc tả (2025-11-23)

### Nhóm 2: Khách hàng & Đặt hàng (Customer)

#### 4. Tìm kiếm nhà hàng (Search Restaurants) ✅
*   **Mã UC:** UC-002
*   **Tác nhân:** Khách hàng
*   **Mô tả:** Tìm kiếm theo tên, món ăn, loại hình ẩm thực và lọc kết quả.
*   **Lý do chọn:** Chức năng đầu tiên khách hàng sử dụng, liên quan đến module `Search` và Indexing.
*   **Trạng thái:** Đã hoàn thành đặc tả (2025-11-24)

#### 5. Đặt hàng cá nhân (Place Individual Order) ✅
*   **Mã UC:** UC-003
*   **Tác nhân:** Khách hàng
*   **Mô tả:** Quy trình chọn món, thêm vào giỏ, chọn địa chỉ, áp dụng mã giảm giá và thanh toán.
*   **Lý do chọn:** Use case kinh điển của ứng dụng Food Delivery, thể hiện luồng giao dịch chính (Transactional).
*   **Tham chiếu:** `usecase_individual_order_decomposition.puml`
*   **Trạng thái:** Đã hoàn thành đặc tả (2025-11-24)

### Nhóm 3: TeamCart - Tính năng Đột phá (Customer)

#### 6. Khởi tạo TeamCart (Create TeamCart) ✅
*   **Mã UC:** UC-004a (Phân rã từ UC-004)
*   **Tác nhân:** Khách hàng (Host)
*   **Mô tả:** Tạo giỏ hàng nhóm, thiết lập nhà hàng, thời gian chốt đơn và lấy link chia sẻ.
*   **Lý do chọn:** Bắt đầu quy trình TeamCart - tính năng USP (Unique Selling Point) của đồ án.
*   **Tham chiếu:** `usecase_teamcart_decomposition.puml`
*   **Trạng thái:** Đã hoàn thành đặc tả (2025-11-24)

#### 7. Tham gia TeamCart (Join TeamCart) ✅
*   **Mã UC:** UC-004b
*   **Tác nhân:** Khách hàng (Member)
*   **Mô tả:** Truy cập qua link chia sẻ, nhập tên (nếu là guest) và chọn món vào giỏ chung.
*   **Lý do chọn:** Thể hiện tính tương tác thời gian thực (Real-time) giữa các người dùng.
*   **Tham chiếu:** `usecase_teamcart_decomposition.puml`
*   **Trạng thái:** Đã hoàn thành đặc tả (2025-11-28)

#### 8. Chốt đơn TeamCart (Lock & Checkout TeamCart) ✅
*   **Mã UC:** UC-004c
*   **Tác nhân:** Khách hàng (Host)
*   **Mô tả:** Host khóa giỏ hàng, các thành viên thanh toán phần của mình, Host chốt đơn cuối cùng để gửi đi.
*   **Lý do chọn:** Logic phức tạp nhất về xử lý thanh toán phân tán và đồng bộ trạng thái.
*   **Tham chiếu:** `usecase_teamcart_decomposition.puml`
*   **Trạng thái:** Đã hoàn thành đặc tả (2025-11-29)

### Nhóm 4: Vận hành & Phản hồi (Operation)

#### 9. Xử lý đơn hàng (Process Order) ✅
*   **Mã UC:** UC-009
*   **Tác nhân:** Nhà hàng
*   **Mô tả:** Nhà hàng nhận thông báo, chấp nhận đơn, cập nhật trạng thái (Đang chuẩn bị -> Đã giao).
*   **Lý do chọn:** Hoàn thiện vòng đời của một đơn hàng từ phía cung cấp dịch vụ.
*   **Trạng thái:** Đã hoàn thành đặc tả (2025-11-29)

#### 10. Đánh giá nhà hàng (Review Restaurant) ✅
*   **Mã UC:** UC-006
*   **Tác nhân:** Khách hàng
*   **Mô tả:** Viết đánh giá và chấm điểm sau khi đơn hàng hoàn tất.
*   **Lý do chọn:** Tính năng quan trọng để xây dựng uy tín và dữ liệu cho hệ thống gợi ý.
*   **Trạng thái:** Đã hoàn thành đặc tả (2025-11-29)

## Kế hoạch thực hiện
1.  Sử dụng template bảng đặc tả trong `2_Dac_ta_chuc_nang.tex`.
2.  Điền chi tiết cho từng use case dựa trên logic trong code `Application` layer và Domain Design.
3.  Vẽ lại hoặc chèn các biểu đồ Activity (nếu cần thiết cho các UC phức tạp như TeamCart).

## Checklist & Quy trình Review trước khi viết Đặc tả

Để đảm bảo tính chính xác và đồng bộ giữa báo cáo và mã nguồn, cần thực hiện các bước review sau cho mỗi Use Case:

### Bước 1: Review Tài liệu Kiến trúc (Architecture Docs)
*   [ ] **Đọc `Docs/Architecture/Domain_Design.md`**:
    *   Xác định **Aggregate Root** liên quan (ví dụ: `Order`, `TeamCart`).
    *   Nắm vững các **Invariants** (bất biến) và **Enums** (Trạng thái) để viết Tiền điều kiện/Hậu điều kiện.
*   [ ] **Đọc `Docs/Architecture/Features-Design.md`**:
    *   Hiểu phạm vi chức năng (Scope) để không viết lan man.
*   [ ] **Xem Biểu đồ Use Case/Activity**:
    *   Kiểm tra `report/res/diagrams/` để đảm bảo các bước trong bảng đặc tả khớp với hình vẽ.

### Bước 2: Review Code (Application Layer)
*   **Vị trí:** `src/Application/[FeatureName]/Commands` hoặc `Queries`.
*   [ ] **Phân tích Command/Query (Input Data):**
    *   Mở file `...Command.cs`.
    *   Liệt kê các properties -> Đây chính là các dòng trong **Bảng dữ liệu đầu vào**.
    *   Note lại các trường bắt buộc (Required) hay tùy chọn (Optional).
*   [ ] **Phân tích Validator (Validation Rules):**
    *   Mở file `...CommandValidator.cs` (nếu có).
    *   Ghi lại các rule (ví dụ: `NotNull`, `MinimumLength`, `EmailAddress`) -> Điền vào cột "Điều kiện hợp lệ".
*   [ ] **Phân tích Handler (Flow & Logic):**
    *   Mở file `...CommandHandler.cs`.
    *   **Tiền điều kiện:** Tìm các đoạn check `if (...) throw ...` ở đầu hàm (ví dụ: check tồn tại, check trạng thái, check quyền).
    *   **Luồng sự kiện chính:** Theo dõi các bước gọi hàm (Repository.Add, DomainMethod, UnitOfWork.Save).
    *   **Luồng thay thế:** Các trường hợp `try-catch` hoặc logic `if-else` rẽ nhánh.

### Bước 3: Review Code (Domain Layer)
*   **Vị trí:** `src/Domain/Aggregates/[AggregateName]`.
*   [ ] **Phân tích Domain Methods:**
    *   Xem logic bên trong các method được gọi bởi Handler.
    *   Xác định các **Domain Events** được bắn ra (ví dụ: `OrderPlacedEvent`) -> Đây là cơ sở cho **Hậu điều kiện** (Hệ thống gửi thông báo, cập nhật read model...).

### Ví dụ Mapping
*   **Code:** `if (order.Status != OrderStatus.Pending) throw ...`
    *   -> **Spec (Tiền điều kiện):** Đơn hàng phải ở trạng thái Chờ xử lý.
*   **Code:** `RuleFor(x => x.Email).EmailAddress()`
    *   -> **Spec (Data Input):** Email phải đúng định dạng.
