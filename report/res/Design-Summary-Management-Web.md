# Tóm tắt thông số thiết kế & màn hình mục tiêu

## Mục tiêu chung
Tài liệu này tổng hợp các thông số thiết kế và danh sách màn hình mục tiêu để thực hiện mockup. Nội dung hướng tới nhiều bên liên quan, không đi sâu vào chi tiết kỹ thuật triển khai.

## 1) Thông số thiết kế
- **Thiết bị ưu tiên**:
  - Desktop chính: 1440x900 (mật độ thông tin cao).
  - Tablet ngang: 1280x800 và 1024x768.
  - Mobile bổ sung: 390x844 (chỉ khuyên dùng khi cho màn theo dõi đơn hàng, các màn khác cũng hỗ trợ mobile nhưng không tối ưu).
- **Phong cách tổng thể**:
  - Gọn gàng, trực quan, ưu tiên khả năng quét nhanh dữ liệu.
  - Giao diện làm việc: thông tin dày, ít trang trí, tập trung hiệu suất thao tác.
  - Có chế độ nền tối; tương phản chữ/nền đảo chiều để dễ đọc.
- **Color scheme & theme (màu chính trong app)**:
  - **Primary 50**: #FDF4F2 (nền nhạt, highlight).
  - **Primary 100**: #FBE6E2.
  - **Primary 200**: #F7D0C9.
  - **Primary 300**: #F0AD9F (nhấn phụ, biểu đồ).
  - **Primary 400**: #E8816D.
  - **Primary 500**: #DD5A3C (màu thương hiệu chính, CTA).
  - **Primary 600**: #CC4226.
  - **Primary 700**: #AA331D.
  - **Primary 800**: #8D2D1D.
  - **Primary 900**: #75291D.
  - **Primary 950**: #40120A (chữ đậm/nền tối sâu).
  - **Neutral nền sáng**: #FFFFFF.
  - **Neutral viền nhạt**: #E5E7EB.
  - **Neutral chữ phụ**: #6B7280.
- **Typography**:
  - Cấp chữ: 12px (metadata/badge), 14px (label/nav), 16px (body), 18px (nhấn nhẹ), 24px (heading), 30px (page title).
  - Độ đậm: 400 (secondary), 500 (nav/button), 600 (nhấn vừa/badge), 700 (heading).
- **Button**:
  - Primary: nền màu thương hiệu, chữ trắng, dùng cho hành động chính.
  - Secondary: viền mảnh, nền trong suốt.
  - Text/Ghost: không nền, dùng cho hành động phụ.
  - Trạng thái: hover sáng hơn, active nhấn nhẹ, focus có viền nổi 2px.
- **Input & form**:
  - Ô nhập có viền trung tính, bo góc vừa phải; label đặt trên hoặc trái theo không gian.
  - Focus dùng màu thương hiệu; lỗi hiển thị ngay dưới trường nhập với màu cảnh báo.
- **Khoảng cách & bố cục**:
  - Lưới khoảng cách theo bước 4px/8px; ưu tiên các khoảng 8/12/16/24px.
  - Card nội dung padding ~16px; section lớn cách nhau ~24px.
  - Bảng dữ liệu dùng hàng gọn, phù hợp mật độ cao.
- **Feedback**:
  - Thông báo nhanh xuất hiện ở góc trên phải (chung) và góc dưới phải (theo luồng).
  - Trạng thái trống có thông điệp hướng dẫn; dữ liệu đang tải có placeholder trực quan.

## 2) Màn hình mục tiêu (chọn 4 màn hình tiêu biểu)
2. **Restaurant Dashboard**: Tổng quan vận hành với số liệu và biểu đồ, đại diện cho bố cục mật độ cao. Ảnh đã xuất tại report\Hinhve\restaurant-dashboard-desktop.png
3. **Live Orders**: Luồng quan trọng nhất, thể hiện xử lý real-time và khả năng quét nhanh trạng thái. Ảnh đã xuất tại report\Hinhve\restaurant-live-orders-desktop.png
4. **Quản lý thực đơn (danh sách + form)**: Đại diện cho luồng CRUD và form phức tạp. Ảnh đã xuất tại report\Hinhve\restaurant-menu-management-desktop.png
5. **Duyệt đăng ký nhà hàng**: Đại diện cho nghiệp vụ quản trị và quy trình duyệt. Ảnh đã xuất tại report\Hinhve\admin-restaurant-approval-desktop.png

## 3) Quy ước đầu ra
- Mockup trình bày theo phong cách đen trắng để tập trung bố cục và nội dung.
- Desktop là bản chính; Mobile chỉ làm thêm cho đăng nhập và luồng cần theo dõi nhanh.
