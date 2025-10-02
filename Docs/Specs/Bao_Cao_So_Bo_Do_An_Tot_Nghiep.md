# Báo Cáo Sơ Bộ - Đồ Án Tốt Nghiệp: Hệ Thống Giao Đồ Ăn YummyZoom

**Dự án:** YummyZoom - Nền tảng giao đồ ăn trực tuyến

## 1. Giới Thiệu

Tài liệu này trình bày báo cáo sơ bộ về đồ án tốt nghiệp "Hệ thống giao đồ ăn YummyZoom". Dự án tập trung vào việc xây dựng một nền tảng toàn diện, kết nối người dùng (khách hàng) với các nhà hàng, cho phép đặt món, thanh toán và theo dõi đơn hàng một cách hiệu quả. Mục tiêu của dự án là áp dụng các kiến thức về kiến trúc phần mềm hiện đại, thiết kế hướng miền (Domain-Driven Design) và các công nghệ tiên tiến để tạo ra một sản phẩm có khả năng mở rộng và bảo trì cao.

## 2. Các Đối Tượng Người Dùng Chính Trong Hệ Thống

Hệ thống xác định ba đối tượng người dùng chính với vai trò và trách nhiệm rõ ràng:

1.  **Người Dùng (Khách hàng):**
    *   Là người sử dụng cuối, có nhu cầu đặt đồ ăn.
    *   Có các quyền như: tìm kiếm nhà hàng, xem thực đơn, tạo và quản lý giỏ hàng (cá nhân và nhóm), đặt hàng, thanh toán, theo dõi trạng thái đơn hàng, và để lại đánh giá.

2.  **Chủ Nhà Hàng (và Nhân viên nhà hàng):**
    *   Là đối tác kinh doanh của nền tảng.
    *   Có các quyền quản lý nhà hàng của mình, bao gồm: cập nhật thông tin nhà hàng, quản lý thực đơn (thêm/sửa/xóa món, cập nhật trạng thái "hết hàng"), tiếp nhận và xử lý đơn hàng (chấp nhận/từ chối, cập nhật trạng thái), tạo và quản lý các chương trình khuyến mãi (coupon).

3.  **Quản Trị Viên Hệ Thống (Admin):**
    *   Là người vận hành và giám sát toàn bộ nền tảng.
    *   Có quyền cao nhất, bao gồm: quản lý người dùng và nhà hàng (xác thực, khóa tài khoản), theo dõi tổng quan hoạt động của hệ thống (doanh thu, số lượng đơn hàng), hỗ trợ giải quyết các vấn đề phát sinh, và kiểm duyệt nội dung (đánh giá).

## 3. Các Tính Năng Chính Theo Thiết Kế

Các tính năng cốt lõi của hệ thống được thiết kế để phục vụ cho các nhóm người dùng khác nhau:

*   **Tính năng cho Khách hàng:**
    *   **Quản lý tài khoản:** Đăng ký, đăng nhập, quản lý thông tin cá nhân, địa chỉ, phương thức thanh toán.
    *   **Khám phá và Tìm kiếm:** Tìm kiếm nhà hàng theo tên, món ăn, ẩm thực, vị trí.
    *   **Đặt hàng:** Xem thực đơn, tùy chỉnh món ăn, thêm vào giỏ hàng và tiến hành đặt hàng.
    *   **Thanh toán:** Hỗ trợ nhiều phương thức thanh toán (mô phỏng), áp dụng mã giảm giá.
    *   **Theo dõi đơn hàng:** Cập nhật trạng thái đơn hàng theo thời gian thực.
    *   **Đánh giá:** Cho phép khách hàng đánh giá và viết bình luận cho nhà hàng sau khi hoàn thành đơn hàng.
    *   **TeamCart (Giỏ hàng nhóm):** Một tính năng đặc biệt cho phép nhiều người cùng tham gia vào một giỏ hàng, mỗi người tự chọn món và thanh toán phần của mình trước khi host xác nhận đặt hàng.

*   **Tính năng cho Nhà hàng:**
    *   **Quản lý hồ sơ nhà hàng:** Cập nhật thông tin, logo, giờ hoạt động.
    *   **Quản lý thực đơn:** Tạo và tùy chỉnh thực đơn, danh mục, món ăn, giá cả và hình ảnh.
    *   **Quản lý đơn hàng:** Nhận thông báo đơn hàng mới theo thời gian thực, chấp nhận/từ chối và cập nhật tiến trình chuẩn bị.
    *   **Quản lý khuyến mãi:** Tạo và quản lý các mã coupon với nhiều điều kiện áp dụng khác nhau.

*   **Tính năng cho Quản trị viên:**
    *   **Dashboard tổng quan:** Theo dõi các chỉ số quan trọng của hệ thống.
    *   **Quản lý người dùng và nhà hàng:** Xem, duyệt, và quản lý các tài khoản trên nền tảng.
    *   **Kiểm duyệt nội dung:** Quản lý các đánh giá của người dùng.

## 4. Các Khía Cạnh Kỹ Thuật

### 4.1. Hệ thống Backend

*   **Kiến trúc phần mềm:**
    Dự án được xây dựng dựa trên kiến trúc **Clean Architecture** kết hợp với các nguyên tắc của **Domain-Driven Design (DDD)**. Cấu trúc dự án được phân tách thành các lớp (layer) độc lập với quy tắc phụ thuộc chặt chẽ: `Domain`, `Application`, `Infrastructure`, và `Web (Presentation)`.
    Cách tiếp cận này giúp hệ thống trở nên linh hoạt, dễ kiểm thử (testable), dễ bảo trì và mở rộng.

*   **Công nghệ sử dụng:**
    *   **Nền tảng và Ngôn ngữ:** .NET 9, C#
    *   **Web Framework:** ASP.NET Core Web API
    *   **Cơ sở dữ liệu (Database):** PostgreSQL
    *   **Truy cập dữ liệu (Data Access):** Entity Framework Core (cho tác vụ ghi) và Dapper (cho tác vụ đọc hiệu năng cao), theo nguyên tắc CQRS.
    *   **Real-time:** ASP.NET Core SignalR để cập nhật trạng thái đơn hàng và các tương tác trên TeamCart theo thời gian thực.
    *   **Triển khai (Deployment):** Dự án được cấu hình để có thể triển khai trên nền tảng đám mây Azure.

### 4.2. Ứng dụng Frontend

Hệ thống bao gồm các ứng dụng frontend riêng biệt được thiết kế cho từng nhóm người dùng cụ thể.

*   **Ứng dụng cho Khách hàng (Customer App):**
    *   **Nền tảng:** Ứng dụng di động (Mobile App) được xây dựng bằng **Flutter**, cho phép chạy trên cả hai hệ điều hành iOS và Android từ một codebase duy nhất.
    *   **Kiến trúc:** Ứng dụng tuân theo kiến trúc phân lớp rõ ràng (Presentation, Data, Domain) và sử dụng các thư viện hiện đại như Provider cho quản lý trạng thái và Go Router cho điều hướng.
    *   **Tiến độ:** Giao diện người dùng cho các tính năng chính như trang chủ, danh sách nhà hàng, và chi tiết nhà hàng đã được xây dựng. Tuy nhiên, ứng dụng **chưa được tích hợp với API của backend** và hiện đang hoạt động với dữ liệu giả (mock data). Nền tảng kiến trúc đã vững chắc, sẵn sàng cho việc tích hợp trong giai đoạn tới.

*   **Ứng dụng cho Quản trị viên và Nhà hàng (Admin & Restaurant App):**
    *   **Nền tảng:** Ứng dụng web (Web App) sẽ được phát triển bằng framework **Angular**.
    *   **Tiến độ:** Ứng dụng này được lên kế hoạch phát triển trong giai đoạn sau, sau khi các API của backend đã hoàn thiện và ổn định.

## 5. Tiến Độ Hiện Tại và Kế Hoạch Tiếp Theo

*   **Tiến độ hiện tại:**
    Về phía backend, dự án đã hoàn thành việc xây dựng và kiểm thử các miền (domain) nghiệp vụ cốt lõi. Các luồng tính năng chính cho người dùng cuối và chủ nhà hàng đã được triển khai. Về phía frontend, ứng dụng cho khách hàng đã có giao diện cơ bản cho các luồng chính nhưng chưa kết nối với backend.

*   **Kế hoạch tiếp theo:**
    Giai đoạn phát triển tiếp theo sẽ tập trung vào hai mảng chính: (1) Tích hợp ứng dụng Flutter của khách hàng với các API backend đã có. (2) Bắt đầu xây dựng các tính năng và API dành cho quản trị viên, song song với việc phát triển ứng dụng web bằng Angular để phục vụ cho vai trò quản trị và chủ nhà hàng.
