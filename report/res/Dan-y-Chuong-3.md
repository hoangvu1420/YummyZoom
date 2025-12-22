Chào bạn,

Dựa trên cấu trúc bạn mong muốn và nội dung Chương 2 đã cung cấp (đặc biệt là các yêu cầu về **TeamCart**, **hiệu năng**, **tính toàn vẹn dữ liệu** và **trải nghiệm người dùng**), tôi đã tối ưu hóa lại dàn ý Chương 3.

Cấu trúc này đảm bảo tiêu chí: **Logic theo các khối chức năng**, **Gắn chặt với vấn đề ở Chương 2**, và **Có sự so sánh/biện luận khoa học**.

Dưới đây là dàn ý chi tiết:

---

# CHƯƠNG 3: CÔNG NGHỆ SỬ DỤNG VÀ GIẢI PHÁP KỸ THUẬT

## 3.1. Kiến trúc hệ thống và Ứng dụng Backend

Phần này giới thiệu nền tảng cốt lõi xử lý nghiệp vụ, đáp ứng yêu cầu về **Khả năng bảo trì** và **Độ tin cậy** (Mục 2.4).

### 3.1.1. Kiến trúc Clean Architecture và Domain-Driven Design (DDD)

* **Công nghệ/Phương pháp:** Clean Architecture (Robert C. Martin) kết hợp DDD.
* **Vấn đề giải quyết (Chương 2):**
* Giải quyết độ phức tạp của nghiệp vụ **TeamCart** (tách biệt logic thanh toán phân tán khỏi giao diện).
* Đảm bảo yêu cầu phi chức năng về **Khả năng bảo trì** và dễ dàng kiểm thử (Unit Test) logic lõi.


* **Lựa chọn thay thế:**
* *Layered Architecture (truyền thống):* Dễ bị phụ thuộc vòng, khó bảo trì khi dự án lớn.
* *Microservices:* Quá phức tạp về hạ tầng cho quy mô hiện tại (50-100 CCU), chi phí vận hành cao.


* **Lý do chọn:** Cân bằng tốt nhất giữa sự rõ ràng của code và chi phí triển khai cho quy mô đồ án, dễ dàng nâng cấp lên Microservices sau này nếu cần.
* **Tài liệu tham khảo:** [1] R. C. Martin, *Clean Architecture: A Craftsman's Guide to Software Structure and Design*.

### 3.1.2. Nền tảng phát triển: .NET 9 và ASP.NET Core

* **Công nghệ:** .NET 9 (Long Term Support/Preview tùy thời điểm) + ASP.NET Core Web API.
* **Vấn đề giải quyết (Chương 2):**
* Đáp ứng yêu cầu **Hiệu năng**: Xử lý 50-100 CCU ổn định, thời gian phản hồi < 1s (Mục 2.4).
* Cung cấp RESTful API chuẩn hóa cho đa nền tảng (Mobile & Web).


* **Lựa chọn thay thế:**
* *Node.js (Express/NestJS):* Hiệu năng xử lý tính toán (CPU bound) thấp hơn .NET trong một số trường hợp phức tạp.
* *Java (Spring Boot):* Cấu hình phức tạp hơn, thời gian khởi động (startup time) thường lâu hơn so với .NET hiện đại.


* **Lý do chọn:** Hiệu năng vượt trội của Kestrel web server, hỗ trợ mạnh mẽ lập trình bất đồng bộ (Async/Await) giúp tối ưu I/O.
* **Tài liệu tham khảo:** [2] Microsoft, "Performance improvements in ASP.NET Core 9".

## 3.2. Cơ sở dữ liệu và Lưu trữ

Phần này tập trung vào yêu cầu **Tính toàn vẹn dữ liệu** (ACID) cho giao dịch thanh toán và tốc độ truy xuất.

### 3.2.1. Hệ quản trị cơ sở dữ liệu quan hệ: PostgreSQL

* **Công nghệ:** PostgreSQL 16 + Entity Framework Core (ORM).
* **Vấn đề giải quyết (Chương 2):**
* Đảm bảo tính **ACID** cho các giao dịch thanh toán của TeamCart (tránh sai lệch tiền nong).
* Lưu trữ dữ liệu có cấu trúc phức tạp (Menu, Order, User).


* **Lựa chọn thay thế:**
* *MySQL:* Hỗ trợ các tính năng nâng cao (như JSON query, GIS) yếu hơn PostgreSQL.
* *SQL Server:* Chi phí bản quyền cao khi scale (dù bản Developer miễn phí), PostgreSQL là mã nguồn mở hoàn toàn.


* **Lý do chọn:** Độ ổn định cao, tuân thủ chuẩn SQL tốt nhất, hỗ trợ tốt kiểu dữ liệu địa lý (GIS) cho tính năng bản đồ sau này.
* **Tài liệu tham khảo:** [3] PostgreSQL Global Development Group, "PostgreSQL 16 Documentation".

### 3.2.2. Bộ nhớ đệm (Caching): Redis (Optional - nếu có dùng)

* *Lưu ý: Chỉ đưa vào nếu bạn thực sự dùng Redis, nếu không hãy nói về In-Memory Cache của .NET.*
* **Công nghệ:** Redis / Distributed Cache.
* **Vấn đề giải quyết:** Giảm tải cho database khi truy vấn danh sách món ăn/nhà hàng (Dữ liệu ít thay đổi nhưng đọc nhiều).
* **Lý do chọn:** Tốc độ đọc/ghi cực nhanh (sub-millisecond), hỗ trợ các cấu trúc dữ liệu linh hoạt.

## 3.3. Xác thực và Phân quyền (Authentication & Authorization)

Đáp ứng trực tiếp yêu cầu **Bảo mật** tại mục 2.4.

### 3.3.1. Cơ chế xác thực: JSON Web Token (JWT)

* **Công nghệ:** JWT (thư viện `System.IdentityModel.Tokens.Jwt`).
* **Vấn đề giải quyết:** Xác thực không trạng thái (stateless) cho hàng trăm người dùng mobile, không gây áp lực lưu session lên server.
* **Lựa chọn thay thế:** *Session-based Authentication* (khó mở rộng ngang, tốn tài nguyên server).
* **Lý do chọn:** Chuẩn công nghiệp cho Mobile App, dễ dàng tích hợp với các bên thứ 3.
* **Tài liệu tham khảo:** [4] IETF, "RFC 7519: JSON Web Token (JWT)".

### 3.3.2. Quản lý định danh: ASP.NET Core Identity

* **Công nghệ:** ASP.NET Core Identity.
* **Vấn đề giải quyết:** Quản lý User, Role (Customer, Restaurant, Admin), băm mật khẩu, bảo mật thông tin cá nhân.
* **Lý do chọn:** Framework bảo mật có sẵn, được kiểm chứng an toàn bởi Microsoft, tích hợp sâu vào hệ sinh thái .NET.

## 3.4. Ứng dụng dành cho khách hàng (Mobile App)

Đáp ứng yêu cầu **Trải nghiệm người dùng (UX)**, sự tiện lợi cho sinh viên/nhân viên văn phòng (Mục 2.1.3).

### 3.4.1. Nền tảng phát triển: Flutter & Dart

* **Công nghệ:** Flutter SDK (ngôn ngữ Dart).
* **Vấn đề giải quyết:**
* Xây dựng giao diện mượt mà (60fps) trên cả Android và iOS từ một codebase duy nhất.
* Hiện thực hóa UI phức tạp của **TeamCart** (đồng bộ trạng thái nhiều người dùng).


* **Lựa chọn thay thế:**
* *React Native:* Hiệu năng có thể thấp hơn do cầu nối (bridge) JavaScript, giao diện phụ thuộc vào component native của OS nên khó đồng nhất hoàn toàn.
* *Native (Kotlin/Swift):* Tốn gấp đôi nguồn lực phát triển và bảo trì.


* **Lý do chọn:** Khả năng kiểm soát từng pixel trên màn hình, Hot Reload giúp phát triển nhanh, hiệu năng gần như Native.
* **Tài liệu tham khảo:** [5] Google, "Flutter Architectural Overview".

### 3.4.2. Quản lý trạng thái và Tương tác API

* **Công nghệ:** Provider/BLoC (tùy code thực tế) + Dio.
* **Vấn đề giải quyết:** Quản lý luồng dữ liệu phức tạp khi người dùng thêm món, sửa món trong giỏ hàng chung.
* **Lý do chọn:** Tách biệt UI và Logic (MVVM pattern), dễ dàng debug và mở rộng tính năng.

### 3.4.3. Tích hợp bản đồ và Thanh toán

* **Bản đồ:** Mapbox (hoặc Google Maps Platform) - Giúp định vị và tính khoảng cách giao hàng.
* **Thanh toán:** Stripe SDK - Đáp ứng yêu cầu thanh toán an toàn, linh hoạt, hỗ trợ tách bill (nếu dùng tính năng split payment của Stripe) hoặc xử lý thẻ tín dụng an toàn.

## 3.5. Ứng dụng Web quản trị (Admin & Restaurant Portal)

Đáp ứng nhu cầu quản lý của **Đối tác nhà hàng** (Mục 2.1.3).

### 3.5.1. Nền tảng Frontend: Angular

* **Công nghệ:** Angular (v16+) + TypeScript.
* **Vấn đề giải quyết:** Xây dựng ứng dụng SPA (Single Page Application) quản trị với cấu trúc module chặt chẽ, phù hợp quy mô doanh nghiệp.
* **Lựa chọn thay thế:**
* *ReactJS:* Là thư viện (library) thay vì framework, cần cài thêm nhiều bên thứ 3 để có tính năng tương đương Angular (router, form validation).
* *VueJS:* Linh hoạt nhưng cộng đồng doanh nghiệp (enterprise) dùng Angular nhiều hơn cho các trang admin phức tạp.


* **Lý do chọn:** Kiến trúc hướng component rõ ràng, tích hợp sẵn HttpClient, Form handling mạnh mẽ, phù hợp tư duy lập trình hướng đối tượng (giống Backend).
* **Tài liệu tham khảo:** [6] Google, "Angular Documentation".

### 3.5.2. Thư viện giao diện: PrimeNG

* **Công nghệ:** PrimeNG + Tailwind CSS.
* **Vấn đề giải quyết:** Cung cấp các bảng dữ liệu (Data Grid), biểu đồ thống kê doanh thu trực quan cho nhà hàng.
* **Lý do chọn:** Bộ component đồ sộ chuyên cho admin dashboard, tiết kiệm thời gian thiết kế UI.

## 3.6. Cộng tác thời gian thực (Realtime Collaboration)

Đây là trái tim của tính năng **TeamCart**, đáp ứng yêu cầu độ trễ < 500ms (Mục 2.4).

### 3.6.1. Giao thức và Framework: SignalR

* **Công nghệ:** ASP.NET Core SignalR (sử dụng WebSockets làm transport chính).
* **Vấn đề giải quyết:**
* Đồng bộ trạng thái TeamCart tức thời: Khi A thêm món, màn hình của B và C cập nhật ngay lập tức.
* Thông báo đơn hàng mới cho nhà hàng (trên Web Angular) mà không cần reload trang.


* **Lựa chọn thay thế:**
* *Polling (Hỏi định kỳ):* Tốn băng thông, độ trễ cao, không tạo được cảm giác "thời gian thực".
* *Firebase Realtime Database:* Phụ thuộc nền tảng bên thứ 3, chi phí tăng nhanh khi lượng dữ liệu lớn.


* **Lý do chọn:** Tích hợp sẵn trong .NET, tự động chọn phương thức kết nối tốt nhất (WebSockets > Server-Sent Events > Long Polling), code đơn giản hóa việc quản lý connection (Hub).
* **Tài liệu tham khảo:** [7] Microsoft, "Introduction to ASP.NET Core SignalR".

---

## 4. Hướng dẫn viết chi tiết để đạt điểm cao

Để bài viết trong chương này thuyết phục và mang tính học thuật (dành cho đồ án tốt nghiệp), bạn hãy áp dụng các nguyên tắc sau:

1. **Luôn bắt đầu bằng "Vấn đề":** Đừng chỉ liệt kê "Tôi dùng .NET". Hãy viết: *"Để giải quyết yêu cầu về hiệu năng xử lý đồng thời cao được đặt ra ở Mục 2.4, hệ thống sử dụng nền tảng .NET 9..."*.
2. **So sánh có trọng tâm:** Khi so sánh với công nghệ khác, hãy so sánh dựa trên bối cảnh của đồ án (quy mô vừa, team nhỏ, cần phát triển nhanh) chứ không so sánh chung chung.
* *Ví dụ:* Đừng nói "Java chậm". Hãy nói "Với quy mô nhóm phát triển hạn chế và yêu cầu tích hợp chặt chẽ với Azure/Windows (nếu có), hệ sinh thái .NET mang lại tốc độ phát triển nhanh hơn so với cấu hình môi trường Java Spring".


3. **Trích dẫn tài liệu:**
* Khi nhắc đến định nghĩa (Clean Architecture, JWT), hãy dẫn nguồn sách hoặc RFC.
* Khi nhắc đến tính năng kỹ thuật (SignalR, Flutter), hãy dẫn nguồn Documentation chính thức (Microsoft Docs, Flutter.dev).


4. **Biểu đồ minh họa:** (Rất quan trọng)
* Nên có hình vẽ minh họa kiến trúc tổng thể (System Architecture Diagram).
* Hình vẽ mô hình Clean Architecture áp dụng trong dự án.
* Hình vẽ luồng hoạt động của SignalR trong TeamCart.
* *Gợi ý:* Tôi có thể tạo các sơ đồ này (dạng text/Mermaid) nếu bạn cần ở bước tiếp theo.



---

### Danh mục Tài liệu tham khảo (Dự kiến cho Chương 3)

**[1] Kiến trúc Clean Architecture**

* **Trích dẫn:** R. C. Martin, *Clean Architecture: A Craftsman's Guide to Software Structure and Design*. Upper Saddle River, NJ: Prentice Hall, 2017.
* **Loại tài liệu:** Sách chuyên khảo.


**[2] Nền tảng .NET 9 và ASP.NET Core**

* **Trích dẫn:** Microsoft, "What's new in .NET 9," *Microsoft Learn*, Nov. 2024. [Online].
* **Link:** [https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview)
* **Trích dẫn bổ sung (Về hiệu năng):** Microsoft, "ASP.NET Core Performance Best Practices," *Microsoft Learn*. [Online].
* **Link:** [https://learn.microsoft.com/en-us/aspnet/core/performance/performance-best-practices](https://learn.microsoft.com/en-us/aspnet/core/performance/performance-best-practices)

**[3] Cơ sở dữ liệu PostgreSQL**

* **Trích dẫn:** PostgreSQL Global Development Group, "PostgreSQL 16.0 Documentation," 2023. [Online].
* **Link:** [https://www.postgresql.org/docs/16/index.html](https://www.postgresql.org/docs/16/index.html)

**[4] Chuẩn xác thực JSON Web Token (JWT)**

* **Trích dẫn:** M. Jones, J. Bradley, and N. Sakimura, "JSON Web Token (JWT)," RFC 7519, Internet Engineering Task Force, May 2015. [Online].
* **Link:** [https://www.rfc-editor.org/rfc/rfc7519](https://www.rfc-editor.org/rfc/rfc7519)

**[5] Nền tảng Flutter (Kiến trúc)**

* **Trích dẫn:** Google, "Flutter Architectural Overview," *Flutter.dev*. [Online].
* **Link:** [https://docs.flutter.dev/resources/architectural-overview](https://docs.flutter.dev/resources/architectural-overview)

**[6] Framework Angular**

* **Trích dẫn:** Google, "Angular Documentation: Architecture Overview," *Angular.io*. [Online].
* **Link:** [https://angular.io/guide/architecture](https://angular.io/guide/architecture)

**[7] Công nghệ Real-time SignalR**

* **Trích dẫn:** Microsoft, "Introduction to ASP.NET Core SignalR," *Microsoft Learn*, 2023. [Online].
* **Link:** [https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)

**[8] Thư viện giao diện PrimeNG**

* **Trích dẫn:** PrimeTek, "PrimeNG Documentation," *PrimeFaces.org*. [Online].
* **Link:** [https://primeng.org/installation](https://primeng.org/installation)

**[9] Domain-Driven Design (Lý thuyết bổ trợ)**

* **Trích dẫn:** E. Evans, *Domain-Driven Design: Tackling Complexity in the Heart of Software*. Boston, MA: Addison-Wesley, 2003.

**[10] Clean Code (Nguyên lý mã nguồn)**

* **Trích dẫn:** R. C. Martin, *Clean Code: A Handbook of Agile Software Craftsmanship*. Prentice Hall, 2008.

---

### Hướng dẫn cách trích dẫn trong bài viết (Ví dụ)

Khi viết nội dung Chương 3, bạn sẽ gắn các số này vào cuối câu khẳng định. Ví dụ:

> "Hệ thống backend được xây dựng dựa trên nền tảng .NET 9 mới nhất của Microsoft để tối ưu hóa hiệu năng xử lý [2]. Kiến trúc tổng thể tuân theo mô hình Clean Architecture giúp tách biệt rõ ràng giữa logic nghiệp vụ và các yếu tố hạ tầng [1]."

> "Để giải quyết bài toán xác thực không trạng thái (stateless) cho ứng dụng di động, đồ án sử dụng chuẩn JSON Web Token (JWT) theo đặc tả RFC 7519 [4]."