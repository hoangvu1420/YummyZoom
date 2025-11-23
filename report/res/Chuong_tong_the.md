
## II. HƯỚNG DẪN CHI TIẾT TỪNG CHƯƠNG

### CHƯƠNG 1: GIỚI THIỆU ĐỀ TÀI (3-6 trang)

#### 1.1 Đặt vấn đề
**Nội dung cần trình bày:**
- **Bối cảnh thực tế**: Xu hướng tăng trưởng của ngành giao đồ ăn online tại Việt Nam
- **Vấn đề hiện tại**: 
  - Các ứng dụng hiện có (Grab, ShopeeFood, Baemin) có những hạn chế gì?
  - Nhu cầu của nhà hàng vừa và nhỏ chưa được đáp ứng đầy đủ
  - Trải nghiệm người dùng còn có thể cải thiện
- **Tầm quan trọng**: Tác động đến kinh tế số, hỗ trợ doanh nghiệp nhỏ, tạo việc làm

**Lưu ý viết:**
- Không trình bày giải pháp trong phần này
- Làm nổi bật mức độ cấp thiết của bài toán
- Dẫn chứng bằng số liệu thống kê (nếu có)

#### 1.2 Mục tiêu và phạm vi đề tài
**Nội dung cần trình bày:**
- **Tổng quan nghiên cứu hiện tại**: Các ứng dụng giao đồ ăn phổ biến và tính năng chính
- **So sánh đánh giá**: Ưu nhược điểm của từng nền tảng
- **Xác định hạn chế**: Những gì chưa được giải quyết tốt
- **Mục tiêu cụ thể**: Xây dựng ứng dụng giao đồ ăn với các tính năng:
  - Quản lý nhà hàng và menu thông minh
  - Hệ thống đặt hàng và thanh toán linh hoạt
  - Quản lý coupon và khuyến mãi
  - Theo dõi đơn hàng real-time
  - Hệ thống đánh giá và phản hồi

#### 1.3 Định hướng giải pháp
**Nội dung cần trình bày:**
- **Định hướng công nghệ**: 
  - Sử dụng Clean Architecture để đảm bảo tính mở rộng và bảo trì
  - Áp dụng Domain-Driven Design cho business logic phức tạp
  - .NET 8 và C# cho backend performance cao
- **Giải pháp tổng quan**: Xây dựng platform tích hợp đầy đủ tính năng
- **Đóng góp chính**: 
  - Ứng dụng Clean Architecture vào domain giao đồ ăn
  - Thiết kế hệ thống real-time notifications
  - Xử lý trạng thái đơn hàng phức tạp

#### 1.4 Bố cục đồ án
**Mẫu viết:**
```
Chương 2 trình bày về khảo sát và phân tích yêu cầu hệ thống, bao gồm nghiên cứu 
các ứng dụng tương tự hiện có, phân tích yêu cầu chức năng và phi chức năng, 
thiết kế use case và quy trình nghiệp vụ chính.

Trong Chương 3, tôi giới thiệu về các công nghệ được sử dụng trong dự án, 
bao gồm Clean Architecture, Domain-Driven Design, .NET 8, Entity Framework Core 
và các công nghệ liên quan khác, đồng thời phân tích lý do lựa chọn từng công nghệ.
```

### CHƯƠNG 2: KHẢO SÁT VÀ PHÂN TÍCH YÊU CẦU (9-11 trang)

#### 2.1 Khảo sát hiện trạng
**Nội dung cần trình bày:**
- **Phân tích các ứng dụng hiện có**:
  - GrabFood: Ưu điểm (phủ sóng rộng, thanh toán đa dạng), nhược điểm (phí cao, UX phức tạp)
  - ShopeeFood: Ưu điểm (tích hợp ecommerce, voucher nhiều), nhược điểm (chất lượng dịch vụ)
- **Bảng so sánh tính năng**: Lập bảng so sánh các tính năng chính
- **Khảo sát người dùng**: Kết quả khảo sát nhu cầu (nếu có thực hiện)

#### 2.2 Tổng quan chức năng

##### 2.2.1 Biểu đồ use case tổng quát
**Actors chính:**
- **Khách hàng (Customer)**: Người dùng cuối đặt đồ ăn
- **Nhà hàng (Restaurant)**: Chủ nhà hàng hoặc nhân viên quản lý
- **Quản trị viên (Admin)**: Quản lý hệ thống tổng thể

**Use cases chính:**
- Quản lý tài khoản người dùng
- Quản lý nhà hàng và menu
- Xử lý đơn hàng
- Quản lý thanh toán
- Quản lý coupon và khuyến mãi

##### 2.2.2 Biểu đồ use case phân rã - Quản lý đơn hàng
**Use cases con:**
- Tạo đơn hàng
- Xác nhận đơn hàng
- Chuẩn bị đơn hàng
- Giao hàng
- Hoàn thành đơn hàng
- Hủy đơn hàng

##### 2.2.3 Quy trình nghiệp vụ
**Quy trình đặt hàng chính:**
1. Khách hàng duyệt nhà hàng và menu
2. Thêm món ăn vào giỏ hàng
3. Áp dụng coupon (nếu có)
4. Thanh toán
5. Nhà hàng nhận và xác nhận đơn hàng
6. Chuẩn bị và giao hàng
7. Hoàn thành và đánh giá

#### 2.3 Đặc tả chức năng (5-7 use cases quan trọng)

##### 2.3.1 Đặc tả use case: Tạo đơn hàng
**Tên:** Tạo đơn hàng (Create Order)
**Tác nhân:** Khách hàng
**Tiền điều kiện:** 
- Khách hàng đã đăng nhập
- Giỏ hàng có ít nhất một món ăn
**Luồng sự kiện chính:**
1. Khách hàng xem lại giỏ hàng
2. Chọn địa chỉ giao hàng
3. Chọn phương thức thanh toán
4. Áp dụng coupon (nếu có)
5. Xác nhận đặt hàng
6. Hệ thống tạo đơn hàng và gửi thông báo
**Luồng thay thế:**
- A1: Coupon không hợp lệ - hiển thị thông báo lỗi
- A2: Thanh toán thất bại - quay lại bước chọn phương thức
**Hậu điều kiện:** Đơn hàng được tạo với trạng thái "Chờ xác nhận"

#### 2.4 Yêu cầu phi chức năng
- **Hiệu năng**: Thời gian phản hồi < 2 giây cho 95% request
- **Khả năng mở rộng**: Hỗ trợ đồng thời 10,000 người dùng
- **Bảo mật**: Mã hóa dữ liệu nhạy cảm, xác thực 2FA
- **Tính sẵn sàng**: Uptime 99.9%
- **Tương thích**: Hỗ trợ web browsers chính, mobile responsive

### CHƯƠNG 3: CÔNG NGHỆ SỬ DỤNG (không quá 10 trang)

#### 3.1 Clean Architecture
**Mô tả:**
- Kiến trúc phân lớp với sự phụ thuộc một chiều
- Tách biệt business logic khỏi infrastructure
**Lý do lựa chọn:**
- Tính bảo trì cao, dễ kiểm thử
- Độc lập với framework và database
- Hỗ trợ development song song
**Các lựa chọn khác:** Layered Architecture, Hexagonal Architecture
**So sánh:** Clean Architecture cung cấp cân bằng tốt nhất giữa tính đơn giản và linh hoạt

#### 3.2 Domain-Driven Design (DDD)
**Mô tả:**
- Phương pháp thiết kế tập trung vào business domain
- Sử dụng Aggregates, Entities, Value Objects
**Lý do lựa chọn:**
- Domain giao đồ ăn có business rules phức tạp
- Cần đảm bảo tính nhất quán dữ liệu
- Hỗ trợ collaboration với domain experts

#### 3.3 .NET 8 và C#
**Mô tả:** Platform phát triển hiện đại từ Microsoft
**Lý do lựa chọn:**
- Performance cao, cross-platform
- Ecosystem phong phú
- Hỗ trợ async/await cho real-time features
**Các lựa chọn khác:** Java Spring Boot, Node.js, Python Django

#### 3.4 Entity Framework Core
**Mô tả:** ORM cho .NET
**Lý do lựa chọn:**
- Code-first approach phù hợp với DDD
- Migration tự động
- LINQ queries type-safe

#### 3.5 SignalR
**Mô tả:** Thư viện real-time communication
**Lý do lựa chọn:** Cần real-time notifications cho trạng thái đơn hàng

### CHƯƠNG 4: THIẾT KẾ, TRIỂN KHAI VÀ ĐÁNH GIÁ HỆ THỐNG

#### 4.1 Thiết kế kiến trúc

##### 4.1.1 Lựa chọn kiến trúc phần mềm
- **Clean Architecture** được chọn làm kiến trúc tổng thể
- **Áp dụng cụ thể**: 
  - Domain layer: User, Restaurant, Order, Menu aggregates
  - Application layer: Commands, Queries, DTOs, Validators
  - Infrastructure layer: EF Core, Identity, SignalR
  - Web layer: API Controllers, Endpoints

##### 4.1.2 Thiết kế tổng quan
**Biểu đồ package dependency:**
- Web → Application → Domain
- Infrastructure → Application → Domain
- SharedKernel ← tất cả layers

##### 4.1.3 Thiết kế chi tiết gói
**Domain Package:**
- User Aggregate: User, Address, PaymentMethod entities
- Restaurant Aggregate: Restaurant, Menu, MenuItem entities  
- Order Aggregate: Order, OrderItem entities
- Shared: Events, Specifications, Services

#### 4.2 Thiết kế chi tiết

##### 4.2.1 Thiết kế giao diện
- **Target devices**: Desktop browsers, mobile responsive
- **Design system**: Material Design principles
- **Color scheme**: Warm colors (orange, red) cho food theme
- **Key screens**: Restaurant listing, menu browsing, order tracking

##### 4.2.2 Thiết kế lớp
**Lớp User Aggregate:**
```csharp
public class User : AggregateRoot<UserId>
{
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public List<Address> Addresses { get; private set; }
    // Methods: AddAddress(), SetDefaultPayment(), etc.
}
```

**Sequence diagram**: Cho use case "Tạo đơn hàng"
- User → OrderController → CreateOrderHandler → OrderAggregate → Repository

##### 4.2.3 Thiết kế cơ sở dữ liệu
**E-R Diagram chính:**
- Users (1) → (n) Addresses
- Users (1) → (n) Orders  
- Restaurants (1) → (n) MenuItems
- Orders (n) → (n) MenuItems (OrderItems)
- Users (1) → (n) Reviews

**Database schema**: PostgreSQL với EF Core migrations

#### 4.3 Xây dựng ứng dụng

##### 4.3.1 Thư viện và công cụ sử dụng
| Mục đích | Công cụ | Phiên bản | URL |
|----------|---------|-----------|-----|
| IDE | Visual Studio 2022 | 17.8 | https://visualstudio.microsoft.com/ |
| Framework | .NET | 8.0 | https://dotnet.microsoft.com/ |
| Database | PostgreSQL | 15.0 | https://www.postgresql.org/ |
| ORM | Entity Framework Core | 8.0 | https://docs.microsoft.com/ef/ |
| Testing | xUnit | 2.4.2 | https://xunit.net/ |

##### 4.3.2 Cài đặt và triển khai
**Development environment:**
- Docker containers cho dependencies
- Local development với dotnet watch
- Seed data cho testing

**Production deployment:**
- Azure App Service
- Azure Database for PostgreSQL
- Azure SignalR Service

#### 4.4 Đánh giá hệ thống

##### 4.4.1 Kiểm thử
**Unit Tests:**
- Domain entities và business rules: 85% coverage
- Application handlers: 90% coverage

**Integration Tests:**
- API endpoints: Tất cả endpoints chính
- Database operations: Repository layer

**Functional Tests:**
- End-to-end scenarios: Đặt hàng hoàn chỉnh
- User acceptance testing

##### 4.4.2 Đánh giá hiệu năng
**Load testing results:**
- Response time: Trung bình 800ms
- Throughput: 1000 requests/second
- Concurrent users: 5000 users đồng thời

### CHƯƠNG 5: CÁC GIẢI PHÁP VÀ ĐÓNG GÓP NỔI BẬT

#### 5.1 Áp dụng Clean Architecture cho domain phức tạp
**Vấn đề:** Domain giao đồ ăn có nhiều business rules phức tạp
**Giải pháp:** 
- Tách biệt business logic vào Domain layer
- Sử dụng Domain Events cho loose coupling
- Aggregate boundaries rõ ràng

#### 5.2 Xử lý real-time notifications
**Vấn đề:** Cần cập nhật trạng thái đơn hàng real-time
**Giải pháp:**
- SignalR cho bidirectional communication
- Event-driven architecture với Domain Events
- Connection management cho mobile users

#### 5.3 Quản lý trạng thái đơn hàng phức tạp
**Vấn đề:** Đơn hàng có nhiều trạng thái và transitions phức tạp
**Giải pháp:**
- State Machine pattern trong Order Aggregate
- Business rules validation cho state transitions
- Compensation actions cho failed operations

#### 5.4 Đánh giá và so sánh
**So với các ứng dụng hiện có:**
- Kiến trúc rõ ràng hơn, dễ maintain
- Performance tốt với async processing
- User experience được cải thiện

### CHƯƠNG 6: KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN (2-3 trang)

#### 6.1 Tổng kết kết quả đạt được
- Xây dựng thành công ứng dụng giao đồ ăn đầy đủ tính năng
- Áp dụng thành công Clean Architecture và DDD
- Đạt được các yêu cầu phi chức năng đề ra
- Kiến thức thu được về software architecture

#### 6.2 Hạn chế của đề tài
- Chưa triển khai mobile app native
- Chưa có machine learning cho recommendations
- Scale testing chưa được thực hiện ở quy mô lớn

#### 6.3 Hướng phát triển
**Ngắn hạn:**
- Phát triển mobile applications (iOS, Android)
- Tích hợp payment gateways trong nước
- Thêm tính năng chat giữa khách hàng và nhà hàng

**Dài hạn:**
- Machine learning cho personalized recommendations
- Microservices architecture để scale
- AI chatbot cho customer support
- Blockchain cho loyalty programs
