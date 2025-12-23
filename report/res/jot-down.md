# Dàn ý chi tiết cho phần 4.1.1. Lựa chọn kiến trúc phần mềm

Dựa trên phân tích mã nguồn (`src/Domain`, `src/Application`, `src/Infrastructure`, `src/Web`) và yêu cầu chuyển nội dung từ Chương 3, dưới đây là kế hoạch nội dung chi tiết.

## 1. Yêu cầu điều chỉnh
*   **Chương 3 (Mục 3.1):** Cần rút gọn lại. Chỉ nêu "Sử dụng .NET 9 và ASP.NET Core" làm nền tảng công nghệ. Phần giải thích chi tiết về Clean Architecture và DDD sẽ được chuyển sang Chương 4 để người đọc tập trung vào "Thiết kế" ở chương này.
*   **Chương 4 (Mục 4.1.1):** Sẽ là nơi trình bày sâu về kiến trúc, kết hợp lý thuyết và thực tế project.

## 2. Cấu trúc nội dung đề xuất cho 4.1.1

### A. Tổng quan về kiến trúc (Introduction)
*   **Khẳng định:** Hệ thống áp dụng **Clean Architecture** (Kiến trúc sạch) kết hợp **Domain-Driven Design (DDD)**.
*   **Mô hình tham chiếu:** Vẽ/chèn hình minh họa các vòng tròn đồng tâm (Core ở giữa, Infra ở ngoài).
*   **Lý do cốt lõi:**
    *   Tách biệt logic nghiệp vụ phức tạp của *TeamCart* (Giỏ hàng nhóm) khỏi các yếu tố kỹ thuật.
    *   Dễ dàng bảo trì và mở rộng khi quy mô dự án (số lượng file trong `src/Application` lên tới ~480 file) tăng lên.

### B. Chi tiết các tầng trong YummyZoom (Mapping mã nguồn)
Phần này cần "map" lý thuyết vào cấu trúc thư mục thực tế của bạn:

#### 1. Lớp Miền (Domain Layer) - `src/Domain`
*   **Vai trò:** Là trái tim của hệ thống, chứa các quy tắc nghiệp vụ bất biến. Không phụ thuộc vào bất kỳ thư viện bên ngoài nào (No dependencies).
*   **Thành phần áp dụng:**
    *   **Entities & Aggregate Roots:** Các thực thể chính như `Restaurant`, `Order`, `Cart`. (Ví dụ: Một `Cart` quản lý danh sách `CartItem` và logic tính toá tổng tiền).
    *   **Value Objects:** Các đối tượng định danh bằng giá trị (ví dụ: `Address`, `Money`).
    *   **Domain Events:** Cơ chế xử lý các sự kiện nghiệp vụ (ví dụ: `OrderCreatedEvent`, `TeamCartCompletedEvent`) giúp tách rời các tác vụ phụ (gửi mail, thông báo).

#### 2. Lớp Ứng dụng (Application Layer) - `src/Application`
*   **Vai trò:** Điều phối luồng công việc (Use Cases). Chỉ đạo Domain objects thực hiện nhiệm vụ.
*   **Mô hình áp dụng: CQRS (Command Query Responsibility Segregation)**
    *   **Commands:** Các tác vụ thay đổi dữ liệu (Create/Update/Delete). Sử dụng thư viện **MediatR** để xử lý (Handler).
    *   **Queries:** Các tác vụ đọc dữ liệu tối ưu.
    *   **DTOs:** Đối tượng chuyển đổi dữ liệu, giúp ẩn đi cấu trúc Domain khỏi User Interface.
*   *Lưu ý:* Thư mục này trong project khá lớn, chứng tỏ dự án tuân thủ nghiêm ngặt việc tách nhỏ từng Use Case (Single Responsibility Principle).

#### 3. Lớp Hạ tầng (Infrastructure Layer) - `src/Infrastructure`
*   **Vai trò:** Cài đặt chi tiết các giao diện (Interfaces) đã định nghĩa ở Application/Domain.
*   **Thành phần:**
    *   **Persistence:** Cấu hình Entity Framework Core, `DbContext`, Repositories (tương tác PostgreSQL).
    *   **Services:** Gửi Email, Upload file (Cloudinary), Payment (Stripe).

#### 4. Lớp Giao diện (Presentation/Web Layer) - `src/Web`
*   **Vai trò:** Điểm tiếp nhận yêu cầu từ Mobile App/Admin Web.
*   **Thành phần:**
    *   **Controllers/Minimal APIs:** Nhận HTTP Request -> Gọi MediatR (Application) -> Trả về Response.
    *   Không chứa logic nghiệp vụ (Thin Controllers).

### C. Đánh giá Ưu/Nhược điểm (Critical Evalutation)

*   **Ưu điểm (Tại sao phù hợp?):**
    *   **Testability:** Có thể viết Unit Test cho Domain/Application mà không cần Database thật.
    *   **Flexibility:** Dễ dàng thay thế Database (ví dụ từ SQL Server sang Postgres) hoặc thư viện Email mà không sửa code nghiệp vụ.
    *   **Parallel Development:** Team có thể làm việc song song (người làm UI, người làm logic, người làm DB) nhờ các Interfaces rõ ràng.
*   **Nhược điểm:**
    *   **Complexity:** Số lượng file tăng lên nhiều (do mỗi use case là một class riêng biệt).
    *   **Learning Curve:** Cần thời gian để team làm quen với mô hình CQRS/MediatR.
*   **Kết luận:** Với tính chất đồ án tốt nghiệp cần sự "chuyên nghiệp" và logic phức tạp của bài toán ShopeeFood/GrabFood clone, kiến trúc này là sự đầu tư xứng đáng.

## 3. Các câu hỏi mở (Cần bạn kiểm tra code để viết chính xác 100%)
1.  **Dùng CQRS thuần hay lai?** (Trong `Application` tách folder `Queries` và `Commands` riêng biệt hay để chung theo Feature?)
2.  **Repository Pattern:** Project có dùng Generic Repository (Repository<T>) hay Specific Repository (`OrderRepository`) hay dùng thẳng `DbContext` trong Application Layer (cách tiếp cận Clean Arch hiện đại)?
3.  **SharedKernel:** Có thư mục `src/SharedKernel`, bên trong chứa gì? (Thường là BaseEntity, Result Pattern?).

## 4. Hành động tiếp theo
1.  Di chuyển nội dung từ 3.1.1 sang file mới hoặc vị trí mới trong chương 4.
2.  Bổ sung các chi tiết kỹ thuật như tên folder, tên thư viện (MediatR, EF Core) vào bài viết.
