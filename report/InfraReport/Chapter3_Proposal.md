# Đề xuất Nội dung Chi tiết Chương 3: Hạ tầng dưới dạng mã (Infrastructure as Code)

Tài liệu này đề xuất cấu trúc và nội dung chi tiết cho Chương 3 của báo cáo `InfraReport.tex`, dựa trên tài liệu kiến trúc `Aspire_Azure_Infrastructure.md`.

## Mục tiêu
Giải thích cách hệ thống sử dụng .NET Aspire, Azure Developer CLI (azd) và Bicep để định nghĩa, cấp phát và quản lý hạ tầng Azure một cách tự động, nhất quán và bảo mật.

## Cấu trúc Chương 3

### 3.1 Vai trò của .NET Aspire AppHost trong định nghĩa hạ tầng
*   **Concept**: AppHost (`src/AppHost/Program.cs`) là "Source of Truth" cho kiến trúc logic.
*   **Cơ chế**: Mô tả cách các tài nguyên (`redis`, `postgres`, `web`) được định nghĩa bằng C# Code thay vì YAML/JSON tĩnh.
*   **Mapping**: Giải thích sự tương ứng giữa:
    *   `AddRedis("redis")` (Dev) -> Container Redis cục bộ.
    *   `AddConnectionString("redis")` (Prod) -> Azure Container App chạy Redis image.
    *   `AddAzurePostgresFlexibleServer("postgres")` -> Azure Database for PostgreSQL.
*   **Lợi ích**: Type-safety, Compile-time check, và khả năng mô phỏng kiến trúc Production ngay trên môi trường Dev local.

### 3.2 Kiến trúc Bicep Modules và Tài nguyên Azure
Phân tích các file Bicep trong `src/AppHost/infra/` để làm rõ các tài nguyên vật lý được tạo ra.

#### 3.2.1 Tài nguyên nền tảng (Shared Resources)
*   **ACA Environment (`resources.bicep`)**: Môi trường quản lý container serverless, profile Consumption.
*   **Log Analytics Workspace**: Nơi tập trung logs và metrics.
*   **Azure Container Registry (ACR)**: Kho lưu trữ Docker Images private.
*   **User Assigned Managed Identity**: Danh tính dùng chung cho việc pull image và truy cập Key Vault.

#### 3.2.2 Tài nguyên dữ liệu (Data Resources)
*   **PostgreSQL Flexible Server (`postgres.module.bicep`)**:
    *   Cấu hình SKU `Standard_B1ms` (Burstable) để tối ưu chi phí.
    *   Extension `postgis` được bật tự động cho tính năng bản đồ.
    *   Database `YummyZoomDb` được khởi tạo sẵn.
*   **Redis Component (`redis-containerapp.module.bicep`)**:
    *   Chạy dưới dạng một Container App riêng biệt trong cùng Environment.
    *   Cấu hình nội bộ (Internal Ingress), không expose ra Internet.

#### 3.2.3 Aspire Dashboard
*   Được triển khai như một `.NET Component` trong ACA Environment.
*   Tiếp nhận OTel Signals (Logs, Traces, Metrics) từ Web API để hiển thị trực quan.

### 3.3 Quy trình cấp phát với Azure Developer CLI (azd)
*   **Workflow**: Giải thích ý nghĩa của `azure.yaml` và các lệnh `azd up`, `azd provision`, `azd deploy`.
*   **State Management**: Cách `azd` quản lý trạng thái môi trường trong thư mục `.azure/` và file `.env` (chứa Resource ID và các biến môi trường nhạy cảm).
*   **Bicep Parameter Binding**: Cách `azd` tự động map biến môi trường (ví dụ `AZURE_LOCATION`) vào tham số của file Bicep.

### 3.4 Chiến lược Quản lý Bảo mật và Cấu hình
*   **Zero Trust với Managed Identity**:
    *   Không hardcode password hay connection string trong code.
    *   Web API dùng Managed Identity để xác thực với ACR và Key Vault.
*   **Key Vault Integration**:
    *   `keyvault.module.bicep`: Tạo Key Vault và gán quyền RBAC.
    *   `keyvault-secrets.module.bicep`: Lưu Connection String của Postgres và Redis vào Key Vault.
    *   Ứng dụng đọc cấu hình từ Key Vault khi khởi động.

## Điểm nhấn cần làm nổi bật
1.  **Sự chuyển dịch từ "Imperative" sang "Declarative"**: Ta mô tả *hệ thống cần có gì* (Dùng C# AppHost và Bicep), thay vì *làm thế nào để tạo nó* (Script).
2.  **Dev/Prod Parity**: Sự tương đồng giữa môi trường Dev (Local container) và Prod (Azure Resources) nhờ sự trừu tượng hóa của Aspire.
3.  **Cost Optimization**: Nhấn mạnh việc chọn SKU hợp lý (B1ms, Consumption Profile, Redis container thay vì Redis Managed Service đắt đỏ) cho giai đoạn MVP.

## Hình minh họa đề xuất
*   Sơ đồ ánh xạ từ C# AppHost Object -> Bicep Module -> Azure Resource.
*   Bảng so sánh cấu hình Local vs Production cho các thành phần (Redis, Postgres).
