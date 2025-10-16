Tôi sẽ đọc tài liệu khảo sát sơ bộ và các tài liệu liên quan để tạo dàn ý chi tiết cho Chương 1. 

Searched for files matching `**/report/res/**/*.md`, 4 matches

Read [](file:///e%3A/source/repos/CA/YummyZoom/Docs/Architecture/Database_Schema.md#1-1), lines 1 to 50

Read [](file:///e%3A/source/repos/CA/YummyZoom/Docs/Architecture/Features-Design.md)

Read [](file:///e%3A/source/repos/CA/YummyZoom/report/res/kh%E1%BA%A3o%20s%C3%A1t%20s%C6%A1%20b%E1%BB%99%20-%20Ph%C3%A2n%20t%C3%ADch%20Y%C3%AAu%20c%E1%BA%A7u%20Ch%E1%BB%A9c%20n%C4%83ng.md#1-1), lines 1 to 200

Read [](file:///e%3A/source/repos/CA/YummyZoom/report/res/kh%E1%BA%A3o%20s%C3%A1t%20s%C6%A1%20b%E1%BB%99%20-%20Ph%C3%A2n%20t%C3%ADch%20Y%C3%AAu%20c%E1%BA%A7u%20Ch%E1%BB%A9c%20n%C4%83ng.md#200-200), lines 200 to 342

Read [](file:///e%3A/source/repos/CA/YummyZoom/Docs/Architecture/Domain_Design.md#50-50), lines 50 to 100

Dựa trên việc đọc tài liệu khảo sát sơ bộ và các tài liệu dự án, tôi sẽ tạo một dàn ý chi tiết cho **Chương 1: Giới thiệu đề tài** của báo cáo đồ án YummyZoom:

---

# DÀN Ý CHƯƠNG 1: GIỚI THIỆU ĐỀ TÀI - DỰ ÁN YUMMYZOOM

## **📋 THÔNG TIN TỔNG QUAN**
- **Độ dài**: 3-6 trang
- **Mục tiêu**: Làm nổi bật vấn đề, xác định mục tiêu rõ ràng, giới thiệu định hướng giải pháp
- **Tông điệu**: Khoa học, có dẫn chứng, không cảm tính

---

## **1.1 ĐẶT VẤN ĐỀ** (1-1.5 trang)

Khi đặt vấn đề, sinh viên cần làm nổi bật mức độ cấp thiết, tầm quan trọng và/hoặc quy mô của bài toán của mình.

Gợi ý cách trình bày cho sinh viên: Xuất phát từ tình hình thực tế gì, dẫn đến vấn đề hoặc bài toán gì. Vấn đề hoặc bài toán đó, nếu được giải quyết, đem lại lợi ích gì, cho những ai, còn có thể được áp dụng vào các lĩnh vực khác nữa không. Sinh viên cần lưu ý phần này chỉ trình bày vấn đề, tuyệt đối không trình bày giải pháp.

### **A. Bối cảnh - Xu hướng FoodTech tại Việt Nam**
- Cách mạng công nghiệp 4.0 và sự bùng nổ của FoodTech
- Thị trường giao đồ ăn online Việt Nam:
  - Tăng trưởng 26% năm 2024 (cao nhất Đông Nam Á)
  - GMV từ 1.4 tỷ USD (2023) → 1.8 tỷ USD (2024) [Nguồn: Momentum Works]
  - Thói quen đặt đồ ăn qua app: 30% người dùng chọn hình thức này cho bữa trưa
- Sự phổ biến của smartphone và lối sống bận rộn → nhu cầu tiện lợi

### **B. Hiện trạng thị trường - Thế song cực**
- GrabFood (48% thị phần) vs ShopeeFood (47% thị phần)
- Cạnh tranh khốc liệt bằng:
  - Chiến dịch khuyến mãi "đốt tiền"
  - Hệ sinh thái dịch vụ đa dạng
  - Chương trình loyalty và gói thành viên

### **C. Vấn đề cần giải quyết - "Nỗi đau" chưa được đáp ứng**
- **Thị trường ngách**: Sinh viên đại học và nhân viên văn phòng
  - Đặc điểm: Đặt đồ ăn theo nhóm thường xuyên (giờ nghỉ trưa ngắn)
  - Nhu cầu: Giải pháp nhanh, đơn giản, công bằng trong thanh toán

- **"Nỗi đau" hiện tại với tính năng Group Order**:
  - Một người (chủ nhóm) phải thanh toán toàn bộ
  - Phải mất công thu tiền từ từng thành viên
  - Gây bất tiện, ngại ngùng, mất thời gian
  - Rủi ro: Quên thu tiền, người khác quên trả

- **Tầm quan trọng của bài toán**:
  - Tác động đến trải nghiệm ăn uống tập thể
  - Tiềm năng thị trường lớn (văn hóa ăn chung ở VN)
  - Cải thiện hiệu quả sử dụng thời gian

**⚠️ LƯU Ý**: Phần này KHÔNG trình bày giải pháp, chỉ tập trung vào vấn đề!

---

## **1.2 MỤC TIÊU VÀ PHẠM VI ĐỀ TÀI** (1.5-2 trang)

Sinh viên trước tiên cần trình bày tổng quan các kết quả của các nghiên cứu hiện nay cho bài toán giới thiệu ở phần \ref{section:1.1} (đối với đề tài nghiên cứu), hoặc về các sản phẩm hiện tại/về nhu cầu của người dùng (đối với đề tài ứng dụng). Tiếp đến, sinh viên tiến hành so sánh và đánh giá tổng quan các sản phẩm/nghiên cứu này.

Dựa trên các phân tích và đánh giá ở trên, sinh viên khái quát lại các hạn chế hiện tại đang gặp phải. Trên cơ sở đó, sinh viên sẽ hướng tới giải quyết vấn đề cụ thể gì, khắc phục hạn chế gì, phát triển phần mềm \textbf{có các chức năng chính gì}, tạo nên đột phá gì, v.v.

Trong phần này, sinh viên lưu ý chỉ trình bày tổng quan, không đi vào chi tiết của vấn đề hoặc giải pháp. Nội dung chi tiết sẽ được trình bày trong các chương tiếp theo, đặc biệt là trong Chương 5.


### **A. Tổng quan các ứng dụng hiện tại**
**Bảng so sánh ngắn gọn:**

| Tiêu chí | GrabFood | ShopeeFood | Nhận xét |
|----------|----------|------------|----------|
| **Điểm mạnh** | Hệ sinh thái toàn diện, GrabRewards, GrabUnlimited | Voucher nhiều, tích hợp Shopee, marketing tốt | Cả hai đều mạnh về khuyến mãi |
| **Group Order** | Có (chủ nhóm trả toàn bộ) | Có (chủ nhóm trả toàn bộ) | Chưa giải quyết vấn đề thanh toán |
| **Đối tượng** | 35+ tuổi, ưu tiên tiện lợi | 16-24 tuổi, ưu thích săn sale | Phân khúc khác nhau |

### **B. Phân tích hạn chế hiện tại**
1. **Tính năng Group Order chưa hoàn thiện**:
   - Quy trình thanh toán tập trung vào một người
   - Thiếu tính công bằng và minh bạch
   - Gây khó khăn cho việc chia bill

2. **Chưa tối ưu cho nhu cầu đặt nhóm của sinh viên/nhân viên văn phòng**:
   - Thời gian giờ nghỉ trưa ngắn
   - Cần quy trình nhanh, ít ma sát
   - Mong muốn tự chủ về món ăn và thanh toán

### **C. Xác định mục tiêu cụ thể của YummyZoom**
**Mục tiêu chính**: Xây dựng ứng dụng giao đồ ăn với trọng tâm là giải quyết vấn đề đặt hàng nhóm

**Các chức năng cốt lõi cần phát triển**:
1. **Chức năng cơ bản** (đáp ứng tiêu chuẩn ngành):
   - Hệ thống quản lý tài khoản (Customer, Restaurant, Admin)
   - Tìm kiếm và khám phá nhà hàng, menu
   - Đặt hàng cá nhân với tùy chỉnh món ăn
   - Quản lý giỏ hàng, áp dụng coupon
   - Thanh toán (mô phỏng với Stripe test mode)
   - Theo dõi trạng thái đơn hàng
   - Đánh giá và review

2. **Chức năng đột phá** (lợi thế cạnh tranh):
   - **TeamCart (Giỏ hàng nhóm)** với quy trình thanh toán phân tán
   - Real-time collaboration và updates
   - Quản lý thanh toán cá nhân trong nhóm

3. **Chức năng quản lý cho Restaurant**:
   - Đăng ký và quản lý hồ sơ nhà hàng
   - Quản lý menu, coupon
   - Xử lý đơn hàng real-time
   - Xem đánh giá từ khách hàng

4. **Chức năng quản trị**:
   - Dashboard tổng quan
   - Quản lý và phê duyệt nhà hàng
   - Giám sát nội dung (reviews, coupons)

### **D. Phạm vi và giới hạn dự án**
**Trong phạm vi (In-scope)**:
- Ba vai trò: Customer, Restaurant, Admin
- Luồng đặt hàng hoàn chỉnh (cá nhân và nhóm)
- Tính năng TeamCart với thanh toán phân tán
- Quản lý menu, coupon, đơn hàng
- Thanh toán mô phỏng (Stripe test mode)

**Ngoài phạm vi (Out-of-scope)**:
- Module ứng dụng cho tài xế (Shipper)
- GPS tracking real-time (mô phỏng qua trạng thái)
- AI recommendation (dùng danh mục tĩnh)
- Loyalty/Reward programs
- Thanh toán thực tế với VNPay/MoMo

**⚠️ LƯU Ý**: Giải thích rõ lý do các quyết định giới hạn phạm vi (tập trung nguồn lực, tính khả thi trong đồ án)

---

## **1.3 ĐỊNH HƯỚNG GIẢI PHÁP** (1-1.5 trang)

Từ việc xác định rõ nhiệm vụ cần giải quyết ở phần \ref{section:1.2}, sinh viên đề xuất định hướng giải pháp của mình theo trình tự sau: (i) Sinh viên trước tiên trình bày sẽ giải quyết vấn đề theo định hướng, phương pháp, thuật toán, kỹ thuật, hay công nghệ nào; Tiếp theo, (ii) sinh viên mô tả ngắn gọn giải pháp của mình là gì (khi đi theo định hướng/phương pháp nêu trên); và sau cùng, (iii) sinh viên trình bày đóng góp chính của đồ án là gì, kết quả đạt được là gì.

### **A. Định hướng công nghệ**
**1. Clean Architecture**
- **Lý do**: Tách biệt business logic, dễ maintain và test
- **Giải pháp ngắn gọn**: Phân lớp rõ ràng (Domain, Application, Infrastructure, Web)

**2. Domain-Driven Design (DDD)**
- **Lý do**: Domain giao đồ ăn có business rules phức tạp
- **Giải pháp ngắn gọn**: Sử dụng Aggregates (User, Restaurant, Order, Menu, TeamCart)

**3. .NET 8 và C#**
- **Lý do**: Performance cao, async/await cho real-time, ecosystem phong phú
- **Giải pháp ngắn gọn**: Backend API với ASP.NET Core

**4. Entity Framework Core**
- **Lý do**: Code-first approach phù hợp DDD, migration tự động
- **Giải pháp ngắn gọn**: ORM cho persistence layer

**5. SignalR**
- **Lý do**: Cần real-time updates cho TeamCart
- **Giải pháp ngắn gọn**: WebSocket cho bidirectional communication

**⚠️ LƯU Ý**: Chỉ giới thiệu tên và lý do chọn ngắn gọn. KHÔNG giải thích chi tiết công nghệ!

### **B. Mô tả tổng quan giải pháp**
**Kiến trúc hệ thống**:
- Backend: .NET 8 API với Clean Architecture
- Database: PostgreSQL với EF Core
- Real-time: SignalR cho TeamCart updates
- Authentication: ASP.NET Identity với JWT

**Luồng hoạt động chính**:
1. Customer duyệt nhà hàng → chọn món → tạo đơn hoặc TeamCart
2. TeamCart: Members tham gia → thêm món → thanh toán riêng → chuyển thành Order
3. Restaurant nhận đơn → xử lý → cập nhật trạng thái
4. Admin giám sát và quản trị toàn hệ thống

### **C. Đóng góp chính của đồ án**
1. **Áp dụng Clean Architecture + DDD** vào domain phức tạp (food delivery)
2. **Giải pháp TeamCart đột phá**: Thanh toán phân tán giải quyết "nỗi đau" thực tế
3. **Real-time collaboration**: Trải nghiệm tương tác nhóm mượt mà
4. **Thiết kế hệ thống có tính mở rộng**: Module hóa, separation of concerns

**Kết quả đạt được** (nêu sơ bộ):
- Ứng dụng MVP hoàn chỉnh với các tính năng cốt lõi
- Tính năng TeamCart working với real-time updates
- Kiến trúc rõ ràng, dễ maintain và test
- Performance đáp ứng yêu cầu (< 2s response time)

---

## **1.4 BỐ CỤC ĐỒ ÁN** (0.5-1 trang)

**Mẫu viết** (dạng đoạn văn, KHÔNG bullet points):

"Chương 2 trình bày về khảo sát và phân tích yêu cầu hệ thống. Trong chương này, em thực hiện phân tích chi tiết các ứng dụng giao đồ ăn hàng đầu tại thị trường Việt Nam (GrabFood, ShopeeFood) để xác định các tiêu chuẩn ngành và tìm ra cơ hội khác biệt hóa. Tiếp đến, em xác định các yêu cầu chức năng chi tiết cho ba vai trò chính (Customer, Restaurant, Admin), đặc biệt tập trung vào đặc tả của tính năng TeamCart. Cuối chương, em trình bày các yêu cầu phi chức năng về performance, usability và scalability."

"Trong Chương 3, em giới thiệu về các công nghệ được sử dụng trong dự án, bao gồm Clean Architecture và Domain-Driven Design làm nền tảng kiến trúc, .NET 8 cho backend development, Entity Framework Core cho data access, và SignalR cho real-time communication. Mỗi công nghệ được phân tích về lý do lựa chọn, các lựa chọn thay thế và cách áp dụng vào YummyZoom."

"Chương 4 trình bày chi tiết về thiết kế và triển khai hệ thống. Đầu tiên, em mô tả kiến trúc tổng thể với các layers và dependencies giữa chúng. Tiếp theo, em trình bày thiết kế chi tiết cho các domain aggregates chính (User, Restaurant, Order, Menu, TeamCart), database schema, và các API endpoints. Phần cuối chương trình bày về quá trình xây dựng ứng dụng, các công cụ sử dụng và kết quả kiểm thử."

"Chương 5 tập trung vào các giải pháp và đóng góp nổi bật của đồ án. Em phân tích cách áp dụng Clean Architecture vào domain phức tạp của food delivery, giải pháp xử lý real-time notifications với SignalR, và đặc biệt là thiết kế chi tiết của tính năng TeamCart với cơ chế thanh toán phân tán. Mỗi giải pháp được so sánh với cách tiếp cận của các ứng dụng hiện có để làm rõ tính đột phá."

"Chương 6 tổng kết lại các kết quả đạt được, đánh giá những hạn chế còn tồn tại và đề xuất hướng phát triển trong tương lai như triển khai mobile app native, tích hợp payment gateways thực tế, phát triển loyalty programs và áp dụng machine learning cho personalized recommendations."

---

## **📝 CÁC LƯU Ý QUAN TRỌNG KHI VIẾT**

### **1. Về nội dung**:
- ✅ Dẫn chứng bằng số liệu cụ thể (GMV, thị phần, tăng trưởng)
- ✅ Tham chiếu nguồn đáng tin cậy (Momentum Works, báo cáo thị trường)
- ✅ Làm nổi bật "nỗi đau" và cơ hội thị trường ngách
- ✅ Giải thích rõ quyết định phạm vi (in-scope/out-of-scope)
- ❌ KHÔNG trình bày giải pháp trong phần Đặt vấn đề
- ❌ KHÔNG đi sâu vào chi tiết công nghệ ở Chương 1

### **2. Về văn phong**:
- ✅ Viết thành đoạn văn khoa học, có chủ - vị đầy đủ
- ✅ Liên kết câu, liên kết đoạn mạch lạc
- ✅ Khách quan, không cảm tính
- ❌ KHÔNG dùng bullet points (trừ bảng so sánh)
- ❌ KHÔNG dùng từ "tuyệt vời", "cực kỳ hay", v.v.

### **3. Về cấu trúc**:
- Mỗi mục có đoạn mở đầu giới thiệu
- Nội dung logic, theo thứ tự từ tổng quan → cụ thể
- Liên kết giữa các mục rõ ràng
- Độ dài cân đối giữa các phần

### **4. Liên kết với tài liệu khảo sát**:
- Sử dụng số liệu từ file "khảo sát sơ bộ"
- Tham khảo ma trận so sánh tính năng
- Dựa vào phân tích về GrabFood/ShopeeFood
- Áp dụng các yêu cầu chức năng đã định nghĩa

---
