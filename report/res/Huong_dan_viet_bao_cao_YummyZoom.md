# HƯỚNG DẪN VIẾT BÁO CÁO ĐỒ ÁN TỐT NGHIỆP - DỰ ÁN YUMMYZOOM

---

## I. GIỚI THIỆU TỔNG QUAN

### Về dự án YummyZoom
YummyZoom là một ứng dụng giao đồ ăn được xây dựng theo kiến trúc Clean Architecture và Domain-Driven Design (DDD). Dự án bao gồm:
- **Hệ thống nhà hàng**: Quản lý hồ sơ, menu, đơn hàng, coupon
- **Hệ thống khách hàng**: Duyệt nhà hàng, đặt hàng, thanh toán, theo dõi đơn hàng
- **Kiến trúc phần mềm**: Domain, Application, Infrastructure, Web layers với SharedKernel

### Cấu trúc báo cáo
- Báo cáo tuân theo chuẩn ISO 7144:1986
- Sử dụng LaTeX với template có sẵn
- Độ dài tổng: 35-50 trang (không tính phụ lục)
- Font 13pt, margin: trái 3.5cm, phải 2.5cm, trên/dưới 2cm

## III. QUY ĐỊNH KỸ THUẬT VÀ ĐỊNH DẠNG

### Quy định chung
- **Tuyệt đối không đạo văn** - ghi rõ nguồn tham khảo
- **Không viết bullet points** - viết thành đoạn văn khoa học
- **Mỗi hình/bảng phải được tham chiếu và giải thích**
- **Văn phong khoa học** - không dùng từ ngữ cảm tính

### Định dạng LaTeX
- Font: 13pt Times New Roman
- Margin: trái 3.5cm, phải 2.5cm, trên/dưới 2cm
- Dòng cách: 1.5 lines
- Tài liệu tham khảo: IEEE style

### Cấu trúc files
```
report/
├── DoAn.tex                 # File chính
├── Bia.tex                  # Trang bìa  
├── Tu_viet_tat.tex          # Từ viết tắt
├── Chuong/
│   ├── 1_Gioi_thieu.tex     # Chương 1
│   ├── 2_Khao_sat.tex       # Chương 2
│   ├── 3_Cong_nghe.tex      # Chương 3
│   ├── 4_Ket_qua_thuc_nghiem.tex # Chương 4
│   ├── 5_Giai_phap_dong_gop.tex  # Chương 5
│   └── 6_Ket_luan.tex       # Chương 6
└── Hinhve/                  # Thư mục hình ảnh
```

## IV. CHECKLIST HOÀN THIỆN

### Trước khi nộp báo cáo:
- [ ] Đọc lại toàn bộ báo cáo để kiểm tra logic và flow
- [ ] Kiểm tra chính tả và ngữ pháp
- [ ] Đảm bảo tất cả hình ảnh có caption và được tham chiếu
- [ ] Kiểm tra format consistency (font, spacing, numbering)
- [ ] Cập nhật mục lục và danh sách hình/bảng
- [ ] Kiểm tra danh sách tài liệu tham khảo đầy đủ
- [ ] Test compile LaTeX không lỗi
- [ ] Export PDF final version

### Các lỗi thường gặp cần tránh:
- Viết theo kiểu bullet points thay vì đoạn văn
- Copy-paste text mà không format lại
- Hình ảnh không rõ nét hoặc không có caption
- Thiếu tham chiếu đến hình/bảng trong nội dung
- Sử dụng từ ngữ không khoa học
- Cấu trúc câu không đầy đủ chủ-vị

### Các lưu ý quan trọng:
- Mỗi chương nên có thêm 1 đoạn mở đầu chương và kết thúc chương, mở đầu giới thiệu những nội dung sẽ trình bày trong chương, kết thúc tổng kết lại các nội dung đã trình bày

---

## ⚠️ **NGUYÊN TẮC QUAN TRỌNG: VIỆT HÓA THUẬT NGỮ**

### 🎯 **Mục tiêu:** Viết báo cáo bằng tiếng Việt khoa học, tránh trộn lẫn tiếng Việt - tiếng Anh

### 📋 **Các nguyên tắc bắt buộc:**

1. **✅ ƯU TIÊN sử dụng thuật ngữ tiếng Việt khi có từ tương đương chính xác**
   - ❌ Sai: "backend development", "business logic", "real-time"
   - ✅ Đúng: "phát triển phần backend", "logic nghiệp vụ", "thời gian thực"

2. **✅ GIỮ thuật ngữ tiếng Anh trong ngoặc () khi cần làm rõ**
   - ✅ Ví dụ: "lớp miền (Domain layer)", "tập hợp (Aggregate)", "thực thể (Entity)"
   - ✅ Ví dụ: "phương pháp ưu tiên mã nguồn (code-first)", "kiểm thử đơn vị (unit tests)"

3. **✅ GIỮ NGUYÊN tên riêng và công nghệ cụ thể**
   - ✅ Giữ nguyên: .NET 8, C#, Entity Framework Core, SignalR, PostgreSQL, JWT
   - ✅ Giữ nguyên: GrabFood, ShopeeFood, iOS, Android, MoMo, VNPay
   - ✅ Giữ nguyên: Clean Architecture, Domain-Driven Design (DDD), CQRS

### 💡 **Ví dụ áp dụng:**

**❌ Không tốt:**
> "Dự án sử dụng Clean Architecture với Domain layer chứa các aggregate như User, Restaurant. Application layer triển khai use cases thông qua Commands và Queries."

**✅ Tốt hơn:**
> "Dự án áp dụng Clean Architecture với lớp miền (Domain layer) chứa các tập hợp (aggregates) như User, Restaurant. Lớp ứng dụng (Application layer) triển khai các ca sử dụng thông qua Commands và Queries."


---

*Tài liệu này được tổng hợp từ các hướng dẫn mẫu và tùy chỉnh cho dự án YummyZoom. Sinh viên cần đọc kỹ và tuân thủ nghiêm ngặt các hướng dẫn để đảm bảo chất lượng báo cáo.*