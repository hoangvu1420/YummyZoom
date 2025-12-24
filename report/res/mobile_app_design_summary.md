# Tóm tắt thông số thiết kế & màn hình mục tiêu (Mobile App)

## Mục tiêu chung
Tài liệu này tổng hợp các thông số thiết kế và danh sách màn hình mục tiêu để thực hiện mockup cho app mobile. Nội dung phục vụ báo cáo và các bên liên quan, không đi sâu chi tiết kỹ thuật triển khai.

## 1) Thông số thiết kế
- **Thiết bị ưu tiên**:
  - Mobile chính: 384x854 (tỉ lệ ~9:20). Kích thước này khớp `designSize` trong app.
  - Mobile bổ sung (tuỳ chọn): 360x800 hoặc 390x844 để kiểm tra độ linh hoạt.
- **Phong cách tổng thể**:
  - Đơn giản, rõ ràng, ưu tiên luồng đặt món nhanh.
  - Nội dung xếp lớp theo thứ bậc: tiêu đề -> thông tin chính -> hành động.
  - Tối giản trang trí, tập trung bố cục và nội dung.
- **Color scheme & theme (trong app thực tế)**:
  - Primary: #DD5A3C; Primary Light: #F3A88A; Primary Dark: #B8412C.
  - Secondary: #F1C07B; Secondary Light: #FFE4B5; Secondary Dark: #CF9A50.
  - Accent: #AED581; Accent Dark: #2E982A.
  - Success: #81C784; Error: #E57373; Warning: #FFF176; Info: #64B5F6.
  - Background (light): #FFF8F0; Surface: #FFFFFF.
  - Text primary: #4E342E; Text secondary: #795548.
  - Divider/Outline: #E1CFC2.
- **Typography**:
  - H1 23sp/1.2, H2 19sp/1.2, H3 17sp/1.3, H4 16sp/1.3, H5 15sp/1.3.
  - Body L 14sp/1.5, Body M 13sp/1.5, Body S 11sp/1.3.
  - Button 13sp (letterSpacing 1.25), Caption 10sp.
  - Kích thước chữ dùng `ScreenUtil` (sp) theo `designSize` 384x854.
- **Button**:
  - Theme hiện có cho ElevatedButton: nền Primary, chữ onPrimary, bo góc 8r.
  - Padding mặc định: 16w x 12h; textStyle dùng `labelMedium`.
  - Chưa khai báo theme riêng cho Outlined/TextButton (mặc định Material).
- **Input & form**:
  - Input có `filled: true`, nền Surface, padding 16w x 12h, bo góc 8r.
  - Border mặc định dùng `outline`; Focus dùng Primary (2w); Error dùng màu Error.
  - Label/Hint dùng `bodyMedium` với màu `onSurfaceVariant`.
- **Khoảng cách & bố cục**:
  - Không có spacing system trung tâm; đa số dùng `ScreenUtil` và padding rời trong từng màn hình.
  - CardTheme: margin 8w x 4h, bo góc 12r, viền 1w, elevation 1.
  - Divider: thickness 1, màu `outline`.
- **Feedback**:
  - Thông báo chủ yếu dùng `SnackBar`; một số nơi `floating` (ví dụ Order Tracking).
  - Một số thông báo thành công dùng nền xanh (ví dụ TeamCart).

## 2) Màn hình mục tiêu (chọn 4 màn hình tiêu biểu)
1. **Home/Menu**: Luồng khám phá món/nhà hàng, điều hướng chính. Ảnh đã xuất tại report\Hinhve\mobile-home.png
2. **Restaurant Detail**: Thông tin nhà hàng + danh sách món + CTA đặt món. Ảnh đã xuất tại report\Hinhve\mobile-restaurant_detail.png
3. **TeamCart Lobby**: Trạng thái giỏ nhóm, danh sách thành viên, tổng tiền. Ảnh đã xuất tại report\Hinhve\mobile-teamcart_lobby.png
4. **Checkout/Payment**: Xác nhận địa chỉ, món đã chọn, mã ưu đãi, tổng tiền. Ảnh đã xuất tại report\Hinhve\mobile-checkout_payment.png

## 3) Quy ước đầu ra
- Mockup đen trắng để tập trung vào bố cục và nội dung.
- Mỗi màn hình lưu thành 1 file HTML riêng, đúng kích thước mobile mục tiêu.
- Không dùng ảnh/icon bên ngoài; chỉ dùng khối, text placeholder, đường kẻ.
