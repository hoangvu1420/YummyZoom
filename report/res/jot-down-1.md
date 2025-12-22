# Dàn ý và lưu ý cho Chương 3 - Công nghệ sử dụng

## 1. Mục tiêu chương
- Giới thiệu các công nghệ, nền tảng và nguyên lý đã sử dụng trong hệ thống YummyZoom.
- Liên kết từng công nghệ với vấn đề/yêu cầu cụ thể ở Chương 2.
- Nêu các lựa chọn thay thế khả dĩ và giải thích lý do chọn công nghệ hiện tại.
- Dẫn nguồn tài liệu tham khảo cho từng công nghệ.

## 2. Cấu trúc đề xuất

### 3.1. Tổng quan chương
- Liên kết từ Chương 2: nhu cầu nghiệp vụ và yêu cầu phi chức năng.
- Tóm tắt nhóm công nghệ chính: kiến trúc/thiết kế, backend, dữ liệu, realtime, bảo mật, tích hợp dịch vụ, quan sát hệ thống.

### 3.2. Kiến trúc và nguyên lý thiết kế
- Clean Architecture: lý do phù hợp với yêu cầu bảo trì, kiểm thử, mở rộng.
  - Vấn đề liên quan: hệ thống nhiều vai trò, nghiệp vụ phức tạp.
  - Lựa chọn thay thế: Layered Architecture truyền thống, Microservices ngay từ đầu.
  - Lý do chọn: cân bằng giữa tính mô-đun và phạm vi MVP.
- Domain-Driven Design (DDD) + CQRS (nếu đã dùng trong code và Chương 1/2).
  - Vấn đề liên quan: mô hình hóa nghiệp vụ TeamCart và order.
  - Lựa chọn thay thế: Active Record, mô hình domain đơn giản.
  - Lý do chọn: giảm rủi ro sai lệch nghiệp vụ, dễ kiểm thử.

### 3.3. Nền tảng và framework backend
- .NET 9 + ASP.NET Core Web API.
  - Vấn đề liên quan: hiệu năng, async, RESTful API.
  - Lựa chọn thay thế: Node.js/Express, Java Spring Boot.
  - Lý do chọn: hiệu năng, hệ sinh thái, phù hợp DDD/Clean Architecture.
- API documentation và versioning: NSwag + Asp.Versioning.
  - Vấn đề liên quan: nhiều client (mobile/web/admin) và vòng đời API.
  - Lựa chọn thay thế: Swashbuckle, OAS thủ công.

### 3.4. Tầng dữ liệu và truy cập dữ liệu
- Entity Framework Core + PostgreSQL (Npgsql).
  - Vấn đề liên quan: persistence cho đơn hàng, người dùng, nhà hàng.
  - Lựa chọn thay thế: MySQL, SQL Server.
  - Lý do chọn: PostgreSQL ổn định, hỗ trợ GIS, cộng đồng mạnh.
- Dapper cho các truy vấn đọc tối ưu (nếu có áp dụng).
  - Vấn đề liên quan: hiệu năng ở các màn hình danh sách lớn.
  - Lựa chọn thay thế: EF Core thuần, stored procedures.

### 3.5. Xác thực, phân quyền và bảo mật
- ASP.NET Identity + JWT.
  - Vấn đề liên quan: 3 vai trò (khách hàng/nhà hàng/admin).
  - Lựa chọn thay thế: OAuth2 providers, IdentityServer.
  - Lý do chọn: phù hợp hệ thống nội bộ, dễ tích hợp.
- Quản lý cấu hình/bí mật: Azure Key Vault (nếu dùng ở backend).
  - Vấn đề liên quan: quản lý secret an toàn.

### 3.6. Tính năng realtime
- SignalR cho TeamCart và cập nhật trạng thái đơn.
  - Vấn đề liên quan: đồng bộ hóa giỏ hàng nhóm.
  - Lựa chọn thay thế: WebSocket thuần, MQTT, Firebase Realtime.
  - Lý do chọn: tích hợp tốt với ASP.NET Core.

### 3.7. Cache và hiệu năng
- Redis (StackExchange.Redis) + Memory Cache.
  - Vấn đề liên quan: tăng tốc đọc dữ liệu nóng, giảm tải DB.
  - Lựa chọn thay thế: Memcached, chỉ dùng memory cache.

### 3.8. Tích hợp dịch vụ bên thứ ba
- Stripe (thanh toán mô phỏng).
  - Vấn đề liên quan: xử lý thanh toán nhóm.
  - Lựa chọn thay thế: VNPay/MoMo (ngoài phạm vi).
- Cloudinary (lưu trữ ảnh món ăn/nhà hàng).
  - Lựa chọn thay thế: S3, Azure Blob.
- Firebase Admin/FCM (thông báo).
  - Lựa chọn thay thế: OneSignal, SNS.

### 3.9. Quan sát hệ thống và DevOps
- OpenTelemetry + .NET Aspire (service defaults, AppHost).
  - Vấn đề liên quan: giám sát và cấu hình dịch vụ, dễ debug.
  - Lựa chọn thay thế: Prometheus + Grafana thủ công.
- Ghi chú: chỉ đưa vào nếu thực sự có trong repo backend.

### 3.10. Kết chương
- Tóm tắt vai trò của các nhóm công nghệ.
- Dẫn dắt sang chương thiết kế/triển khai.

## 3. Những lưu ý khi viết
- Mỗi công nghệ phải gắn với vấn đề/yêu cầu cụ thể từ Chương 2.
- Luôn nêu lựa chọn thay thế và lý do chọn hiện tại.
- Tránh mô tả dài dòng; 1-2 đoạn cho mỗi công nghệ.
- Dẫn nguồn tài liệu cho các công nghệ chính (sách, tài liệu chính thức).
- Giữ nhất quán với Chương 1 và 2 (không thêm công nghệ chưa dùng).

## 4. Danh sách công nghệ cần xác thực trước khi chốt
- Mapbox/Vercel/Docker/CI-CD: chỉ đưa vào nếu thực sự dùng trong repo hoặc tài liệu triển khai.
- Dapper: chỉ nêu nếu có thực sự dùng cho truy vấn.
- Identity/JWT: cần đối chiếu implementation hiện có.

# Dan y cong nghe frontend (Flutter/Dart) - YummyZoom

Muc tieu: tong hop cong nghe thuc su su dung trong app khach hang, lien he yeu cau chuong 2 va ly do lua chon.

## 1. Nen tang va kien truc tong the
- Flutter (Dart)
  - Mo ta: SDK cross-platform cho UI, build cho Android/iOS/Web/Desktop.
  - Giai quyet yeu cau: da nen tang, UI nhat quan, hieu nang tot cho ung dung dat do an.
  - Thay the: React Native, native Android/iOS, Kotlin Multiplatform.
  - Ly do chon: mot codebase, he sinh thai Flutter day du, thoi gian trien khai nhanh.
- To chuc module theo feature + MVVM/Repository
  - Mo ta: cau truc thu muc `lib/features/*/data|presentation|viewmodels` ket hop repository + viewmodel.
  - Giai quyet yeu cau: tach bien giao dien/du lieu, de mo rong tinh nang (TeamCart, order tracking, search).
  - Thay the: BLoC, Redux, Clean Architecture day du theo layer.
  - Ly do chon: can bang giua don gian va kha nang mo rong.

## 2. UI/UX va responsive
- Flutter Material + cupertino_icons
  - Mo ta: bo widget co san cho giao dien.
  - Giai quyet yeu cau: UX don gian, giong he sinh thai mobile quen thuoc.
  - Thay the: custom render UI, toolkit khac.
  - Ly do chon: nhanh, on dinh, de tuong thich.
- Flutter ScreenUtil
  - Mo ta: scale kich thuoc theo man hinh.
  - Giai quyet yeu cau: responsive tren nhieu thiet bi.
  - Thay the: LayoutBuilder, MediaQuery thu cong.
  - Ly do chon: giam cong can chinh kich thuoc.
- He thong theme tu tuy chinh
  - Mo ta: `core/theme` gom app_theme, text_styles, app_colors.
  - Giai quyet yeu cau: dong bo nhan dien, de doi theme.
  - Thay the: theme mac dinh.
  - Ly do chon: can kiem soat mau sac, font, spacing.
- line_awesome_flutter
  - Mo ta: bo icon mo rong.
  - Giai quyet yeu cau: icon phu hop nganh hang/do an.
  - Thay the: Material Icons, FontAwesome.
  - Ly do chon: thu vien icon da dang.
- cached_network_image + flutter_cache_manager + image
  - Mo ta: cache anh mang, xu ly anh.
  - Giai quyet yeu cau: toi uu hieu nang danh sach mon/an.
  - Thay the: Image.network + cache tu viet.
  - Ly do chon: giam tai lai, tang toc do cuon.

## 3. State management
- provider + ChangeNotifier
  - Mo ta: state management nhe, de hoc, phu hop MVVM.
  - Giai quyet yeu cau: cap nhat UI realtime, gian don cho nhieu man hinh.
  - Thay the: BLoC, Riverpod, MobX.
  - Ly do chon: it boilerplate, phu hop quy mo hien tai.

## 4. Dependency Injection
- get_it + injectable (+ build_runner)
  - Mo ta: container DI, tao dependency tu dong bang annotation.
  - Giai quyet yeu cau: quan ly service/repository (API, session, notifications).
  - Thay the: Provider DI, Riverpod, manual wiring.
  - Ly do chon: de test, de thay the thanh phan.

## 5. Routing va navigation
- go_router
  - Mo ta: declarative routing, ho tro deep link.
  - Giai quyet yeu cau: luong man hinh lon (home, cart, teamcart, order tracking).
  - Thay the: Navigator 1.0, auto_route.
  - Ly do chon: ro rang, de quan ly URL/deep link.

## 6. Networking va data layer
- Dio + ApiClient + Interceptors
  - Mo ta: HTTP client, co auth/error/logging interceptors.
  - Giai quyet yeu cau: goi API on dinh, tu dong gan token, xu ly loi.
  - Thay the: http package, chopper, retrofit.
  - Ly do chon: linh hoat, ho tro interceptors manh.
- dartz (Either) + equatable
  - Mo ta: kieu Either cho ket qua thanh cong/loi, model so sanh du lieu.
  - Giai quyet yeu cau: luu trinh xu ly loi ro rang, tranh re-render khong can.
  - Thay the: Result custom, freezed.
  - Ly do chon: don gian, tot cho repository layer.
- uuid
  - Mo ta: tao Idempotency-Key cho thanh toan/TeamCart.
  - Giai quyet yeu cau: tranh lap don hang khi retry.
  - Thay the: idempotency client tu viet.
  - Ly do chon: nhanh, tin cay.

## 7. Luu tru cuc bo va cau hinh
- Hive / hive_flutter
  - Mo ta: local NoSQL DB nhe.
  - Giai quyet yeu cau: luu session, cart, cache, luu tru khi offline ngan.
  - Thay the: SharedPreferences, SQLite.
  - Ly do chon: doc/ghi nhanh, it boilerplate.
- flutter_dotenv + Env wrapper
  - Mo ta: doc bien moi truong tu .env.
  - Giai quyet yeu cau: tach config (API base URL, Mapbox, Stripe).
  - Thay the: --dart-define, flavors.
  - Ly do chon: don gian cho giai doan phat trien.

## 8. Realtime va thong bao
- Polling + ETag (TeamCartTrackingService, OrderTrackingService)
  - Mo ta: Timer + StreamController, conditional request theo ETag.
  - Giai quyet yeu cau: cap nhat realtime TeamCart, tracking don hang.
  - Thay the: WebSocket/SSE.
  - Ly do chon: giam chi phi server, de trien khai voi API hien co.
- Firebase Messaging + flutter_local_notifications
  - Mo ta: push notification + local notification.
  - Giai quyet yeu cau: thong bao trang thai don hang, teamcart.
  - Thay the: OneSignal, Pusher Beams.
  - Ly do chon: FCM pho bien, tich hop Flutter tot.
- device_info_plus
  - Mo ta: lay thong tin thiet bi cho dang ky token.
  - Giai quyet yeu cau: quan ly thiet bi nhan thong bao.
  - Thay the: platform channel tu viet.
  - Ly do chon: da nen tang, tin cay.

## 9. Ban do va dinh vi
- mapbox_maps_flutter + Mapbox REST (directions/geocoding)
  - Mo ta: hien thi ban do + goi API Mapbox qua Dio.
  - Giai quyet yeu cau: chon dia chi, tim nha hang gan.
  - Thay the: Google Maps, Here Maps.
  - Ly do chon: tinh nang map linh hoat, gia phu hop.
- geolocator
  - Mo ta: xin quyen va lay vi tri.
  - Giai quyet yeu cau: goi y dia chi, dinh vi giao hang.
  - Thay the: location, native geolocation.
  - Ly do chon: on dinh, ho tro nhieu nen tang.

## 10. Thanh toan va chia se
- flutter_stripe
  - Mo ta: tich hop Stripe PaymentSheet.
  - Giai quyet yeu cau: thanh toan online.
  - Thay the: VNPay/MoMo SDK, Braintree.
  - Ly do chon: Stripe pho bien, SDK Flutter san co.
- share_plus
  - Mo ta: chia se link TeamCart/restaurant.
  - Giai quyet yeu cau: moi nguoi dung tham gia TeamCart.
  - Thay the: share system channel tu viet.
  - Ly do chon: API don gian.

## 11. Build/Release tooling
- flutter_launcher_icons + flutter_native_splash + rename_app
  - Mo ta: tao icon app, splash screen, doi ten app.
  - Giai quyet yeu cau: nhan dien thuong hieu YummyZoom.
  - Thay the: tao tay tren tung nen tang.
  - Ly do chon: tu dong hoa, giam sai sot.
- Cau hinh da nen tang (android/ios/web/desktop)
  - Mo ta: project Flutter tao san cac target.
  - Giai quyet yeu cau: mo rong sang nhieu nen tang khi can.
  - Thay the: chi tap trung mobile.
  - Ly do chon: du phong phat trien mo rong.

## 12. Testing
- flutter_test + mocktail
  - Mo ta: unit/widget test, mock dependency.
  - Giai quyet yeu cau: giam loi nghiem trong khi refactor.
  - Thay the: Mockito.
  - Ly do chon: mocktail de dung, null-safety than thien.

# Dàn ý công nghệ frontend (YummyZoom Portal)

## 1. Framework & nền tảng chính
- Angular 21 + TypeScript
  - Mô tả: SPA framework dùng Angular CLI, kiến trúc component, DI, router, build pipeline chuẩn.
  - Yêu cầu giải quyết: giao diện quản trị nhiều vai trò, module hóa rõ ràng, dễ mở rộng.
  - Lựa chọn thay thế: React, Vue, Svelte.
  - Lý do chọn: hệ sinh thái Angular đầy đủ (router, DI, HttpClient, CLI), phù hợp dự án quản trị quy mô vừa/lớn.

- RxJS
  - Mô tả: thư viện reactive stream cho xử lý bất đồng bộ.
  - Yêu cầu giải quyết: xử lý API call, pipeline login -> lấy profile, xử lý interceptor.
  - Lựa chọn thay thế: Promise/async-await thuần, NgRx effect.
  - Lý do chọn: đi kèm Angular, tích hợp tốt với HttpClient, chuẩn hóa xử lý async.

## 2. UI/UX & hệ thống thiết kế
- PrimeNG + PrimeIcons + PrimeUIX Themes
  - Mô tả: bộ UI component cho Angular; có theme preset và icon.
  - Yêu cầu giải quyết: UI dashboard/biểu mẫu/bảng dữ liệu nhanh, thống nhất giao diện.
  - Lựa chọn thay thế: Angular Material, Ant Design, Tailwind UI.
  - Lý do chọn: component phong phú cho admin (table, dialog, toast, form), cấu hình theme linh hoạt.

- Tailwind CSS + tailwindcss-primeui
  - Mô tả: utility-first CSS, kết hợp lớp của PrimeNG.
  - Yêu cầu giải quyết: tùy biến layout/spacing nhanh, thống nhất token màu theo theme.
  - Lựa chọn thay thế: SCSS/BEM thuần, CSS-in-JS.
  - Lý do chọn: rút ngắn thời gian styling, đồng bộ với theme PrimeNG.

- Phosphor Icons
  - Mô tả: bộ icon vector dùng trực tiếp trong UI.
  - Yêu cầu giải quyết: icon nhất quán cho sidebar, status, trang đăng ký.
  - Lựa chọn thay thế: Material Icons, Font Awesome.
  - Lý do chọn: bộ icon đa dạng, nhẹ, dễ dùng.

## 3. State management & kiến trúc dữ liệu
- Angular Signals
  - Mô tả: reactive state tích hợp sẵn trong Angular (signal, computed, effect).
  - Yêu cầu giải quyết: state cục bộ cho dashboard, live orders, thông báo, form.
  - Lựa chọn thay thế: NgRx, Akita, BehaviorSubject.
  - Lý do chọn: nhẹ, dễ dùng, đủ cho scope quản trị hiện tại.

## 4. Routing & phân quyền
- Angular Router + lazy loading
  - Mô tả: định tuyến theo module/feature, loadComponent/loadChildren.
  - Yêu cầu giải quyết: chia vùng admin/restaurant/onboarding, tối ưu tải trang.
  - Lựa chọn thay thế: router tùy chỉnh, micro-frontend.
  - Lý do chọn: chuẩn Angular, hỗ trợ guard, lazy load.

- Auth Guard + Role Guard
  - Mô tả: guard kiểm tra đăng nhập và vai trò người dùng.
  - Yêu cầu giải quyết: phân quyền admin/restaurant owner/staff, hạn chế truy cập.
  - Lựa chọn thay thế: kiểm tra phân quyền ở từng component.
  - Lý do chọn: tập trung kiểm soát truy cập tại routing, dễ bảo trì.

## 5. Networking & tích hợp backend
- Angular HttpClient + Interceptors
  - Mô tả: gọi REST API, interceptor gắn Bearer token và xử lý lỗi.
  - Yêu cầu giải quyết: gọi API nhất quán, thông báo lỗi chuẩn hóa, gắn auth header.
  - Lựa chọn thay thế: fetch/axios.
  - Lý do chọn: chuẩn Angular, tích hợp RxJS và DI.

- Environment config
  - Mô tả: tách `environment.ts`/`environment.development.ts` để cấu hình API base URL.
  - Yêu cầu giải quyết: chuyển đổi môi trường dev/prod linh hoạt.
  - Lựa chọn thay thế: runtime config hoặc build-time env khác.
  - Lý do chọn: đơn giản, phù hợp Angular CLI.

## 6. Realtime
- Microsoft SignalR
  - Mô tả: kết nối realtime qua WebSocket/transport fallback.
  - Yêu cầu giải quyết: live orders theo thời gian thực cho restaurant.
  - Lựa chọn thay thế: Socket.IO, WebSocket thuần.
  - Lý do chọn: đồng bộ với backend .NET/SignalR, hỗ trợ reconnect/subscription.

## 7. Authentication & bảo mật
- JWT + Refresh Token lưu localStorage
  - Mô tả: đăng nhập lấy access/refresh token, gọi `/me` để lấy profile & role.
  - Yêu cầu giải quyết: đa vai trò, phiên đăng nhập bền, tái xác thực.
  - Lựa chọn thay thế: session cookie, OAuth/OIDC SSO.
  - Lý do chọn: phù hợp kiến trúc API hiện tại, đơn giản cho SPA.

## 8. Build/Deploy
- Angular CLI build
  - Mô tả: build SPA, tối ưu production, output `dist/`.
  - Yêu cầu giải quyết: tối ưu bundle và caching.
  - Lựa chọn thay thế: Vite, custom webpack.
  - Lý do chọn: chuẩn Angular, ít cấu hình.

- Vercel + GitHub Actions (theo kế hoạch triển khai)
  - Mô tả: CI/CD tự động build/deploy frontend.
  - Yêu cầu giải quyết: triển khai nhanh, rollback dễ, tích hợp Git.
  - Lựa chọn thay thế: Netlify, AWS Amplify, self-hosted.
  - Lý do chọn: đơn giản, phù hợp SPA, tích hợp GitHub tốt.

## 9. Testing
- Unit test với Vitest (có cấu hình CLI)
  - Mô tả: chạy unit test thông qua `ng test`.
  - Yêu cầu giải quyết: kiểm thử cơ bản cho component/service quan trọng.
  - Lựa chọn thay thế: Jest/Karma.
  - Lý do chọn: cấu hình sẵn trong repo, phù hợp tốc độ chạy nhanh.

- E2E testing
  - Mô tả: chưa triển khai.
  - Yêu cầu giải quyết: nếu cần kiểm thử hành trình quan trọng (login, live orders).
  - Lựa chọn thay thế: Cypress, Playwright.
  - Lý do hiện tại: ưu tiên phát triển tính năng, chưa tập trung testing.

## 10. Liên hệ Chương 2 (nhu cầu hệ thống)
- UX quản trị nhanh: PrimeNG + Tailwind + routing rõ ràng.
- Realtime live orders: SignalR + service layer + signals.
- Auth đa vai trò: JWT + role guard + profile `/me`.
- Hiệu năng: lazy loading routes + Angular build production.
- Khả năng mở rộng: cấu trúc feature module + DI chuẩn Angular.
