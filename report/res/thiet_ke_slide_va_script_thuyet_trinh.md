# Thiết kế Slide và Script Thuyết trình Đồ án Tốt nghiệp YummyZoom

Tài liệu này phác thảo cấu trúc slide, gợi ý thiết kế hình ảnh và kịch bản (script) thuyết trình cho đồ án YummyZoom. Nội dung bám sát báo cáo đồ án tốt nghiệp.

## Tổng quan
- **Thời lượng ước tính:** 15-20 phút.
- **Phong cách:** Hiện đại, tối giản (Minimalist), tập trung vào biểu đồ và hình ảnh minh họa (Show, don't just tell).
- **Lưu ý:** Các phần trong ngoặc `[...]` là chỉ dẫn hành động hoặc ghi chú cho người thuyết trình.

---

## Phần 1: Giới thiệu (Đặt vấn đề & Mục tiêu)

### Slide 1: Trang bìa
- **Tiêu đề:** YummyZoom - Hệ thống Đặt đồ ăn trực tuyến & Giỏ hàng nhóm (TeamCart)
- **Thông tin phụ:**
    - Đồ án tốt nghiệp Kỹ sư Phần mềm
    - Sinh viên thực hiện: [Tên sinh viên]
    - Giảng viên hướng dẫn: [Tên GVHD]
- **Hình ảnh:** Logo YummyZoom lớn, hình nền mờ là giao diện ứng dụng hoặc mockup điện thoại bắt mắt.
- **Script:**
    "Xin chào Hội đồng và các bạn. Em là [Tên], hôm nay em xin phép trình bày về đồ án tốt nghiệp của mình với đề tài: **YummyZoom - Hệ thống đặt đồ ăn trực tuyến tập trung vào trải nghiệm đặt hàng theo nhóm.**"

### Slide 2: Nội dung trình bày (Agenda)
- **Thiết kế:** Danh sách 5 mục chính với icon tương ứng, dàn trải ngang hoặc dọc thoáng mắt.
    1. Giới thiệu & Đặt vấn đề
    2. Khảo sát & Phân tích yêu cầu
    3. Công nghệ & Kiến trúc hệ thống
    4. Kết quả thực nghiệm (Demo)
    5. Đóng góp & Hướng phát triển
- **Script:**
    "Bài thuyết trình của em sẽ đi qua 5 phần chính, tương ứng với cấu trúc của báo cáo. Bắt đầu từ việc đặt vấn đề, phân tích yêu cầu, lựa chọn công nghệ, sau đó là demo kết quả đạt được và cuối cùng là tổng kết các đóng góp nổi bật của đồ án."

### Slide 3: Đặt vấn đề (Context & Problem)
- **Bố cục:** Chia đôi.
    - Bên trái: "Thực trạng": Hình ảnh minh họa thị trường giao đồ ăn sôi động, biểu tượng Grab/Shopee.
    - Bên phải: "Vấn đề (Pain point)": Icon người dùng đang bối rối, hình ảnh hóa đơn dài và máy tính cầm tay (chia tiền thủ công).
    - Text highlight: "Quy trình thanh toán nhóm phức tạp", "Gánh nặng cho người chủ trì".
- **Script:**
    "Thị trường giao đồ ăn tại Việt Nam đang phát triển rất mạnh mẽ. Tuy nhiên, có một 'nỗi đau' mà người dùng văn phòng và sinh viên thường gặp phải: đó là **đặt hàng theo nhóm**. Hiện tại, quy trình này trên các app lớn vẫn khá thủ công ở khâu thanh toán. Một người phải trả hết, rồi đi 'đòi nợ' từng người sau. Việc này gây phiền toái, nhầm lẫn và tốn thời gian."

### Slide 4: Mục tiêu & Giải pháp (Objectives)
- **Thiết kế:** Hình ảnh điện thoại hiển thị tính năng TeamCart của YummyZoom.
- **Key points:**
    - Xây dựng hệ thống MVP (Minimum Viable Product).
    - **Tính năng lõi:** TeamCart (Giỏ hàng nhóm).
    - **Điểm khác biệt:** Tách biệt thanh toán (Split Payment), Đồng bộ thời gian thực (Real-time).
- **Script:**
    "Để giải quyết vấn đề đó, mục tiêu của YummyZoom là xây dựng một nền tảng giao đồ ăn hoàn chỉnh, nhưng tập trung tối đa vào **trải nghiệm TeamCart**. Hệ thống cho phép nhiều người cùng tham gia một giỏ hàng, chọn món trong thời gian thực, và quan trọng nhất là **tách bạch khoản thanh toán** cho từng cá nhân ngay trên ứng dụng."

---

## Phần 2: Khảo sát & Phân tích yêu cầu

### Slide 5: Tổng quan chức năng & Tác nhân
- **Hình ảnh:** Biểu đồ Use Case tổng quát (lấy từ *Hình 2.1: Biểu đồ use case tổng quát* trong báo cáo).
- **Thiết kế:** Làm nổi bật 3 nhóm tác nhân chính: Khách hàng (Mobile), Nhà hàng (Web Portal), Admin (Web Portal).
- **Script:**
    "Về mặt chức năng, hệ thống được thiết kế phục vụ 3 nhóm đối tượng.
    Thứ nhất là **Khách hàng**, sử dụng Mobile App để đặt món cá nhân hoặc theo nhóm.
    Thứ hai là **Nhà hàng**, có cổng Portal riêng để quản lý thực đơn và xử lý đơn hàng.
    Và thứ ba là **Quản trị viên**, chịu trách nhiệm kiểm duyệt và vận hành nền tảng."

### Slide 6: Phân tích nghiệp vụ TeamCart
- **Hình ảnh:** Biểu đồ hoạt động quy trình TeamCart (lấy từ *Hình 2.7: Biểu đồ hoạt động - Quy trình đặt hàng nhóm*).
- **Thiết kế:** Zoom vào các bước quan trọng: "Tạo phòng" -> "Mời thành viên" -> "Chọn món đồng thời" -> "Chốt đơn & Thanh toán riêng".
- **Script:**
    "Đây là quy trình nghiệp vụ cốt lõi của TeamCart. Điểm đặc biệt là luồng xử lý đồng thời: khi Host tạo nhóm, các thành viên tham gia có thể thêm/bớt món cùng lúc. Hệ thống phải xử lý xung đột dữ liệu và đồng bộ trạng thái tức thì đến tất cả thiết bị."

---

## Phần 3: Công nghệ sử dụng

### Slide 7: Kiến trúc hệ thống (Architecture)
- **Tiêu đề:** Clean Architecture & Domain-Driven Design (DDD)
- **Hình ảnh:** Biểu đồ kiến trúc phân lớp (Onion/Clean Architecture layers) hoặc *Hình 4.1: Biểu đồ gói tổng quan*.
- **Key words:** Domain-Centric, Independent of Frameworks, Testable.
- **Script:**
    "Để quản lý nghiệp vụ phức tạp của TeamCart, em lựa chọn kiến trúc **Clean Architecture** kết hợp với **Domain-Driven Design (DDD)**.
    Kiến trúc này giúp tách biệt hoàn toàn logic nghiệp vụ (Domain) ra khỏi hạ tầng kỹ thuật. Nhờ đó, hệ thống dễ dàng kiểm thử, bảo trì và mở rộng sau này mà không bị phụ thuộc chặt vào Database hay UI."

### Slide 8: Technology Stack (Backend & Hạ tầng)
- **Thiết kế:** Các logo công nghệ sắp xếp gọn gàng.
    - **Core:** .NET 9, ASP.NET Core Web API.
    - **Database:** PostgreSQL (Lưu trữ chính), Redis (Cache & Real-time State).
    - **Orchestration:** .NET Aspire (Quản lý resources).
    - **Deploy:** Azure Container Apps, Docker.
- **Script:**
    "Về công nghệ Backend, em sử dụng **.NET 9** mới nhất để đảm bảo hiệu năng cao. **PostgreSQL** dùng để lưu trữ dữ liệu bền vững, trong khi **Redis** đóng vai trò cực kỳ quan trọng: vừa là Cache, vừa là nơi lưu trữ trạng thái giỏ hàng nhóm để truy xuất cực nhanh. Toàn bộ hệ thống được điều phối bằng **.NET Aspire** và triển khai trên **Azure**."

### Slide 9: Technology Stack (Frontend & Mobile)
- **Thiết kế:** Chia đôi màn hình.
    - **Mobile App:** Logo Flutter + Dart. Ghi chú: "Trải nghiệm mượt mà (60fps), Cross-platform".
    - **Web Portal:** Logo Angular + PrimeNG + TailwindCSS. Ghi chú: "Single Page Application (SPA), Quản trị chuyên nghiệp".
- **Script:**
    "Ở phía người dùng, ứng dụng Mobile được phát triển bằng **Flutter**, mang lại trải nghiệm mượt mà gần như Native trên cả Android và iOS.
    Còn với phía Nhà hàng và Admin, em sử dụng **Angular** kết hợp **PrimeNG** để xây dựng một trang quản trị chuyên nghiệp, tối ưu cho việc thao tác dữ liệu nhiều trên máy tính."

### Slide 10: Giải pháp Real-time (Thời gian thực)
- **Tiêu đề:** Công nghệ lõi cho TeamCart
- **Hình ảnh:** Sơ đồ minh họa SignalR đẩy data xuống các Client A, B, C. Logo SignalR + Firebase Cloud Messaging (FCM).
- **Script:**
    "Trái tim của tính năng TeamCart là khả năng cộng tác thời gian thực. Em sử dụng **SignalR** qua giao thức WebSocket.
    Khi một người thêm món, Server sẽ đẩy ngay tín hiệu xuống các máy khác, độ trễ chỉ tính bằng mili-giây. Ngoài ra, **Firebase (FCM)** được dùng để gửi thông báo đẩy khi App chạy ngầm, đảm bảo người dùng không lỡ bất kỳ lời mời hay cập nhật nào."

---

## Phần 4: Thiết kế, Triển khai & Demo

### Slide 11: Thiết kế Cơ sở dữ liệu (Database Design)
- **Hình ảnh:** Hình ảnh ERD lược giản (lấy từ *Hình 4.8 hoặc 4.9: ERD cho nhóm Order/TeamCart*), không cần hiện hết tất cả các bảng, chỉ hiện các thực thể chính: User, Order, TeamCart, Restaurant.
- **Script:**
    "Cơ sở dữ liệu được thiết kế chặt chẽ để đảm bảo tính toàn vẹn, đặc biệt là các bảng liên quan đến tiền nong và đơn hàng. Đây là một phần của lược đồ ERD tập trung vào module Order và TeamCart."

### Slide 12: Showcase - Ứng dụng Khách hàng
- **Hình ảnh:** 3 màn hình điện thoại ghép lại (Screenshot Home, Menu, Món ăn). Có thể dùng *Hình 4.14*.
- **Script:**
    "Sau đây là kết quả triển khai thực tế. Đây là giao diện ứng dụng khách hàng: Thiết kế hiện đại, tập trung vào hình ảnh món ăn. Người dùng có thể tìm kiếm, xem thực đơn và thêm món dễ dàng."

### Slide 13: Showcase - Tính năng TeamCart (Highlight)
- **Hình ảnh:** Ảnh động (GIF) hoặc 2 ảnh tĩnh so sánh màn hình của Host và Member (*Hình 4.15*).
- **Script:**
    "Và đây là tính năng TeamCart. Màn hình bên trái là chủ phòng, bên phải là thành viên. Mọi thao tác thêm bớt món đều được đồng bộ tức thì.
    Quy trình thanh toán (Checkout) cũng được chia tách rõ ràng: Chủ phòng chốt đơn, sau đó mỗi thành viên tự chọn phương thức thanh toán cho phần của mình."

### Slide 14: Showcase - Web Quản trị Nhà hàng
- **Hình ảnh:** Màn hình Live Orders (*Hình 4.17*) hoặc Dashboard thống kê.
- **Script:**
    "Đối với Nhà hàng, giao diện 'Live Orders' dạng bảng Kanban giúp nhân viên bếp dễ dàng theo dõi: Đơn mới -> Đang nấu -> Sẵn sàng giao. Giao diện này cũng cập nhật Real-time khi có đơn mới nổ ra."

### Slide 15: Kết quả Kiểm thử & Hiệu năng
- **Nội dung:** Bảng tóm tắt kết quả kiểm thử (Pass 93.3%).
- **Biểu đồ (nếu có thể vẽ lại):** Biểu đồ cột so sánh độ trễ (Latency) trung bình (~200ms).
- **Script:**
    "Hệ thống đã trải qua quá trình kiểm thử kỹ lưỡng với hơn 45 kịch bản (Test cases), đạt tỷ lệ Pass trên 93%.
    Về hiệu năng, độ trễ đồng bộ trung bình của TeamCart đo được chỉ khoảng 150-300ms trong điều kiện mạng 4G, hoàn toàn đáp ứng tốt trải nghiệm người dùng thực tế."

---

## Phần 5: Kết luận & Đóng góp

### Slide 16: Giải pháp kỹ thuật nổi bật
- **Thiết kế:** 3 cột icon đại diện cho 3 đóng góp lớn (dựa trên Chương 5).
    1. **Clean Arch + DDD:** Quản lý nghiệp vụ phức tạp.
    2. **Optimistic Concurrency:** Xử lý xung đột dữ liệu (Check-and-Set) khi nhiều người cùng sửa giỏ hàng.
    3. **High-perf Realtime:** SignalR + Redis State.
- **Script:**
    "Tổng kết lại, đồ án có 3 đóng góp kỹ thuật nổi bật:
    Thứ nhất, áp dụng thành công Clean Architecture và DDD cho bài toán nghiệp vụ phức tạp.
    Thứ hai, giải quyết bài toán xung đột dữ liệu (Concurrency) trong TeamCart bằng cơ chế Optimistic Concurrency Control.
    Và thứ ba, xây dựng kiến trúc đồng bộ thời gian thực hiệu năng cao."

### Slide 17: Hạn chế & Hướng phát triển
- **Bố cục:** Tuyến tính (Timeline).
    - **Hiện tại:** MVP, Thanh toán giả lập (Sandbox), Chưa có App Tài xế.
    - **Tương lai:** Tích hợp Payment thật, App cho Shipper, Gợi ý món ăn bằng AI.
- **Script:**
    "Tất nhiên, sản phẩm vẫn còn những hạn chế của một đồ án sinh viên như chưa tích hợp cổng thanh toán Production hay chưa có App riêng cho tài xế.
    Hướng phát triển tiếp theo của em là hoàn thiện các module này, đồng thời tích hợp thêm AI để gợi ý món ăn thông minh hơn cho người dùng."

### Slide 18: Lời cảm ơn & Q&A
- **Nội dung:**
    "CẢM ƠN THẦY CÔ VÀ CÁC BẠN ĐÃ LẮNG NGHE"
    [Thông tin liên hệ/Email]
- **Script:**
    "Em xin kết thúc phần trình bày tại đây. Em rất mong nhận được những ý kiến đóng góp và câu hỏi từ quý Thầy Cô trong Hội đồng ạ. Em xin cảm ơn!"

---
## Các câu hỏi có thể phát sinh (Q&A Preparation)
*Phần này chuẩn bị sẵn để trả lời hội đồng, không đưa lên slide.*

1.  **Hỏi:** Làm sao để xử lý khi 2 người cùng sửa số lượng 1 món ăn cùng lúc trong TeamCart?
    *   **Trả lời:** Em sử dụng cơ chế **Optimistic Concurrency Control** với trường `Version` trong Redis. Khi cập nhật, hệ thống kiểm tra version. Nếu lệch version (do người khác đã sửa trước), request sẽ bị từ chối và Client sẽ tự động retry sau khi lấy dữ liệu mới nhất, đảm bảo tính nhất quán (Consistency).

2.  **Hỏi:** Tại sao chọn Monolithic Modular mà không phải Microservices?
    *   **Trả lời:** Với quy mô nhân sự 1 người và phạm vi đồ án, Monolithic Modular giúp cân bằng giữa việc quản lý độ phức tạp code và chi phí vận hành (DevOps). Tuy nhiên, nhờ thiết kế Clean Arch chia Module rõ ràng (Bounded Contexts), hệ thống hoàn toàn sẵn sàng để tách ra Microservices khi cần scale lên.

3.  **Hỏi:** SignalR chịu tải được bao nhiêu user?
    *   **Trả lời:** Trong thử nghiệm của em với Azure Container Apps (1 replica), hệ thống chịu tốt khoảng 50-100 CCU. Để scale lớn hơn, em sẽ sử dụng **Azure SignalR Service** (managed service) để gánh tải kết nối thay cho server ứng dụng.
