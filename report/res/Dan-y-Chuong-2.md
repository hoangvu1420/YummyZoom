
## DÀN Ý TỔNG QUÁT CHƯƠNG 2: KHẢO SÁT VÀ PHÂN TÍCH YÊU CẦU

### **2.1 Khảo sát hiện trạng** (2-3 trang)

#### 2.1.1 Phân tích các ứng dụng giao đồ ăn hiện có
- **GrabFood**: Ưu điểm (phủ sóng rộng, thanh toán đa dạng), nhược điểm (phí cao, UX phức tạp)
- **ShopeeFood**: Ưu điểm (tích hợp ecommerce, voucher nhiều), nhược điểm (chất lượng dịch vụ)

#### 2.1.2 Bảng so sánh tính năng
- So sánh các tính năng chính: đặt hàng, thanh toán, theo dõi đơn hàng, đánh giá, coupon
- Phân tích điểm mạnh/yếu của từng nền tảng

#### 2.1.3 Xác định khoảng trống thị trường
- Nhu cầu của nhà hàng vừa và nhỏ chưa được đáp ứng đầy đủ
- Thiếu tính năng đặt hàng nhóm (TeamCart)
- Hệ thống quản lý coupon và khuyến mãi chưa tối ưu

### **2.2 Tổng quan chức năng** (3-4 trang)

#### 2.2.1 Biểu đồ use case tổng quát
**Các tác nhân chính:**
- **Khách hàng (Customer)**: Người dùng cuối đặt đồ ăn
- **Nhà hàng (Restaurant)**: Chủ nhà hàng hoặc nhân viên quản lý  
- **Quản trị viên (Admin)**: Quản lý hệ thống tổng thể

**Use cases chính:**
- Quản lý tài khoản người dùng
- Quản lý nhà hàng và menu
- Xử lý đơn hàng (cá nhân và nhóm)
- Quản lý thanh toán
- Quản lý coupon và khuyến mãi
- Hệ thống đánh giá và phản hồi

#### 2.2.2 Biểu đồ use case phân rã - Quản lý đơn hàng
**Use cases con:**
- Tạo đơn hàng cá nhân
- Tạo TeamCart (đặt hàng nhóm)
- Xác nhận đơn hàng
- Chuẩn bị đơn hàng
- Giao hàng
- Hoàn thành đơn hàng
- Hủy đơn hàng

#### 2.2.3 Biểu đồ use case phân rã - Quản lý TeamCart
**Use cases con:**
- Tạo TeamCart
- Thêm thành viên vào TeamCart
- Thêm món ăn vào TeamCart
- Khóa TeamCart để thanh toán
- Áp dụng tip và coupon
- Thanh toán của từng thành viên
- Chuyển đổi TeamCart thành đơn hàng

#### 2.2.4 Quy trình nghiệp vụ chính
**Quy trình đặt hàng cá nhân:**
1. Khách hàng duyệt nhà hàng và menu
2. Thêm món ăn vào giỏ hàng
3. Áp dụng coupon (nếu có)
4. Thanh toán
5. Nhà hàng nhận và xác nhận đơn hàng
6. Chuẩn bị và giao hàng
7. Hoàn thành và đánh giá

**Quy trình đặt hàng nhóm (TeamCart):**
1. Host tạo TeamCart và chia sẻ link
2. Các thành viên tham gia và thêm món ăn
3. Host khóa TeamCart và áp dụng tip/coupon
4. Các thành viên thanh toán phần của mình
5. Host chuyển đổi TeamCart thành đơn hàng
6. Nhà hàng xử lý đơn hàng như bình thường

### **2.3 Đặc tả chức năng** (3-4 trang)

#### 2.3.1 Đặc tả use case: Tạo đơn hàng cá nhân
- **Tên**: Tạo đơn hàng (Create Order)
- **Tác nhân**: Khách hàng
- **Tiền điều kiện**: Khách hàng đã đăng nhập, giỏ hàng có ít nhất một món ăn
- **Luồng sự kiện chính**: Xem giỏ hàng → Chọn địa chỉ → Chọn phương thức thanh toán → Áp dụng coupon → Xác nhận đặt hàng
- **Luồng thay thế**: Coupon không hợp lệ, thanh toán thất bại
- **Hậu điều kiện**: Đơn hàng được tạo với trạng thái "Chờ xác nhận"

#### 2.3.2 Đặc tả use case: Tạo TeamCart
- **Tên**: Tạo TeamCart (Create TeamCart)
- **Tác nhân**: Khách hàng (Host)
- **Tiền điều kiện**: Khách hàng đã đăng nhập, đã chọn nhà hàng
- **Luồng sự kiện chính**: Chọn nhà hàng → Tạo TeamCart → Nhập tên host → Tạo share token → Chia sẻ link
- **Luồng thay thế**: Nhà hàng không hoạt động
- **Hậu điều kiện**: TeamCart được tạo với trạng thái "Open"

#### 2.3.3 Đặc tả use case: Tham gia TeamCart
- **Tên**: Tham gia TeamCart (Join TeamCart)
- **Tác nhân**: Khách hàng (Guest)
- **Tiền điều kiện**: Có share token hợp lệ
- **Luồng sự kiện chính**: Nhập share token → Nhập tên guest → Xác nhận tham gia
- **Luồng thay thế**: Share token không hợp lệ, TeamCart đã đầy
- **Hậu điều kiện**: Guest trở thành thành viên của TeamCart

#### 2.3.4 Đặc tả use case: Khóa TeamCart để thanh toán
- **Tên**: Khóa TeamCart (Lock TeamCart)
- **Tác nhân**: Khách hàng (Host)
- **Tiền điều kiện**: TeamCart ở trạng thái "Open", có ít nhất một món ăn
- **Luồng sự kiện chính**: Host khóa TeamCart → Tính toán số tiền mỗi thành viên → Thông báo cho các thành viên
- **Luồng thay thế**: TeamCart trống
- **Hậu điều kiện**: TeamCart chuyển sang trạng thái "Locked"

#### 2.3.5 Đặc tả use case: Chuyển đổi TeamCart thành đơn hàng
- **Tên**: Chuyển đổi TeamCart (Convert TeamCart)
- **Tác nhân**: Khách hàng (Host)
- **Tiền điều kiện**: Tất cả thành viên đã thanh toán, TeamCart ở trạng thái "ReadyToConfirm"
- **Luồng sự kiện chính**: Xác nhận địa chỉ giao hàng → Chuyển đổi thành đơn hàng → Thông báo cho nhà hàng
- **Luồng thay thế**: Còn thành viên chưa thanh toán
- **Hậu điều kiện**: TeamCart chuyển sang trạng thái "Converted", đơn hàng được tạo

### **2.4 Yêu cầu phi chức năng** (1-2 trang)

#### 2.4.1 Yêu cầu hiệu năng
- Thời gian phản hồi < 2 giây cho 95% request
- Hỗ trợ đồng thời 10,000 người dùng
- Thời gian tải trang < 3 giây

#### 2.4.2 Yêu cầu bảo mật
- Mã hóa dữ liệu nhạy cảm (thông tin thanh toán, thông tin cá nhân)
- Xác thực 2FA cho tài khoản nhà hàng
- Bảo vệ chống SQL injection, XSS
- Tuân thủ PCI DSS cho thanh toán

#### 2.4.3 Yêu cầu tính sẵn sàng
- Uptime 99.9%
- Khả năng phục hồi sau sự cố < 4 giờ
- Backup dữ liệu hàng ngày

#### 2.4.4 Yêu cầu tương thích
- Hỗ trợ web browsers chính (Chrome, Firefox, Safari, Edge)
- Mobile responsive design
- Hỗ trợ các thiết bị từ 320px trở lên

#### 2.4.5 Yêu cầu khả năng mở rộng
- Kiến trúc microservices để dễ dàng mở rộng
- Hỗ trợ horizontal scaling
- Tách biệt database cho từng domain

---


[1 tool called]

## TÓM TẮT DÀN Ý CHƯƠNG 2

Dựa trên phân tích chi tiết các tài liệu và code của dự án YummyZoom, tôi đã tạo ra dàn ý tổng quát cho **Chương 2: Khảo sát và Phân tích Yêu cầu** với các điểm nổi bật sau:

### **Điểm đặc biệt của dự án YummyZoom:**

1. **Tính năng TeamCart độc đáo**: Đây là điểm khác biệt lớn nhất so với các ứng dụng hiện có, cho phép đặt hàng nhóm với quy trình "Lock, Settle, Convert" rất tinh vi.

2. **Kiến trúc Clean Architecture + DDD**: Dự án áp dụng các nguyên tắc thiết kế hiện đại với 15 aggregates được thiết kế cẩn thận.

3. **Hệ thống thanh toán linh hoạt**: Hỗ trợ cả thanh toán online (Stripe) và COD, với xử lý webhook và real-time notifications.

4. **Quản lý coupon phức tạp**: Hệ thống coupon với nhiều loại (percentage, fixed amount, free item) và áp dụng cho TeamCart.

### **Cấu trúc chương 2 (9-11 trang):**

- **2.1 Khảo sát hiện trạng** (2-3 trang): So sánh với GrabFood, ShopeeFood, Baemin
- **2.2 Tổng quan chức năng** (3-4 trang): Use cases tổng quát và phân rã, quy trình nghiệp vụ
- **2.3 Đặc tả chức năng** (3-4 trang): 5 use cases quan trọng nhất
- **2.4 Yêu cầu phi chức năng** (1-2 trang): Hiệu năng, bảo mật, tính sẵn sàng

### **Các use cases được chọn để đặc tả chi tiết:**
1. Tạo đơn hàng cá nhân
2. Tạo TeamCart  
3. Tham gia TeamCart
4. Khóa TeamCart để thanh toán
5. Chuyển đổi TeamCart thành đơn hàng

Dàn ý này tuân thủ hướng dẫn viết báo cáo, tập trung vào các tính năng độc đáo của YummyZoom và đảm bảo tính khoa học, logic trong việc trình bày yêu cầu hệ thống.