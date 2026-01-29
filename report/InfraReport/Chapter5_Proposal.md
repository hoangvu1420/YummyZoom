# Đề cương Chương 5: Vận hành, Quan sát hệ thống và Mở rộng

## Mục tiêu chương
Chứng minh khả năng quản lý và giám sát sức khỏe hệ thống (Health Monitoring) sau khi triển khai.
Chương này sẽ làm nổi bật sức mạnh của **.NET Aspire Dashboard** và **Azure Log Analytics** trong việc cung cấp cái nhìn sâu (Insight) về logs, metrics, và traces distributed.

## Cấu trúc nội dung đề xuất

### 5.1. Kiến trúc Quan sát (Observability Architecture)
*   **Tiêu chuẩn OpenTelemetry (OTel)**:
    *   Giải thích ngắn gọn việc hệ thống sử dụng chuẩn OTel để thu thập tín hiệu (Signals) thay vì dùng SDK riêng của từng hãng.
    *   3 Trụ cột: Logs (Nhật ký), Metrics (Chỉ số), Traces (Truy vết phân tán).
*   **Luồng dữ liệu**:
    *   Web API -> OTLP Exporter -> Aspire Dashboard (Real-time).
    *   Container Apps -> Diagnostic Settings -> Log Analytics Workspace (Lưu trữ dài hạn).

### 5.2. Giám sát thời gian thực với Aspire Dashboard
*   **Vai trò**: Công cụ mạnh mẽ nhất để Dev/Ops nhìn thấy "mạch đập" của hệ thống ngay lập tức.
*   **Các màn hình chính** (Cần chụp Screenshot):
    *   **Resources View**: Danh sách các container (Web, Redis, Postgres) và trạng thái (Running/Exited).
    *   **Trace View**: Minh họa một request đi từ API -> Database -> Redis. Dễ dàng thấy nút thắt cổ chai (latency) ở đâu.
    *   **Structured Logs**: Xem log chi tiết có cấu trúc, lọc theo mức độ lỗi (Error/Warning).
*   **Điểm mạnh**: Không cần cấu hình phức tạp, có sẵn ngay khi chạy dự án (cả Local và Prod).

### 5.3. Phân tích dài hạn trên Azure (Log Analytics)
*   **Tại sao cần?**: Aspire Dashboard tốt cho live view, nhưng Log Analytics cần cho lịch sử và query phức tạp (KQL).
*   **Ứng dụng**:
    *   Truy vấn tổng hợp lỗi trong 24h qua.
    *   Xem biểu đồ tài nguyên (CPU/RAM Usage) của Container Apps.

### 5.4. Chiến lược Mở rộng (Scaling) và Tối ưu chi phí
*   **Cấu hình Scaling**:
    *   Sử dụng KEDA scalers (HTTP, TCP) mặc định của Container Apps.
    *   Cấu hình `minReplicas` và `maxReplicas`.
*   **Bài toán Cold Start vs. Cost**:
    *   Quyết định để `minReplicas = 0` cho cả Redis và Web API (chấp nhận cold start để tiết kiệm chi phí). Do Web API có các background jobs chạy mỗi 5 phút nên nếu đặt `minReplicas = 1` thì nó sẽ luôn chạy và tiêu tốn thêm chi phí ngay cả khi không có request đến.
    *   Giải thích sự đánh đổi (Trade-off) phù hợp với giai đoạn MVP.

## Hình ảnh minh họa dự kiến (Cần anh chụp)
1.  **Dashboard - Resources**: Chụp màn hình chính hiển thị danh sách các service đang chạy.
2.  **Dashboard - Traces**: Chụp chi tiết một Trace (timeline bar chart) của một API call.
3.  **Azure Portal - Log Analytics**: (Tùy chọn) Chụp giao diện query log trên Azure.

ĐÃ thêm các ảnh vào report\res\diagrams, các ảnh bắt đầu bằng Dashboard-*.png