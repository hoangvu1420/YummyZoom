CHƯƠNG 3. CÔNG NGHỆ SỬ DỤNG

3.1 Fullstack
Next.js là một framework phổ biến trong việc phát triển ứng dụng web dựa trên
React và được phát triển bởi Vercel. Được giới thiệu lần đầu tiên vào năm 2016,
Next.js cung cấp một nền tảng mạnh mẽ và linh hoạt để phát triển các ứng dụng
React tối ưu hóa hiệu suất và thân thiện với công cụ tìm kiếm (SEO). Nhờ vào khả
năng kết hợp tính năng server-side rendering (SSR) và static site generation (SSG),
Next.js đã trở thành một lựa chọn hàng đầu của các lập trình viên khi xây dựng các
ứng dụng web hiện đại.
Next.js có sẵn những tính năng và tự động cấu hình các công cụ cần thiết cho
React như bundling, compiling, ... giúp lập trình viên tận dụng và tập trung vào
việc phát triển ứng dụng một cách nhanh chóng và tối ưu thay vì mất thời gian vào
việc cấu hình.
3.1.1 Lý do lựa chọn NextJS
• Kết hợp Frontend và Backend trong một nền tảng duy nhất (Full-stack):
– Next.js cho phép xây dựng cả giao diện người dùng và các API backend
trong cùng một dự án, giúp đồng bộ phát triển và quản lý logic hệ thống
dễ dàng hơn.
– Các API có thể được xây dựng trực tiếp thông qua các route trong thư mục
app/api hoặc pages/api
• Hỗ trợ nhiều cơ chế render hiện đại:
– Next.js hỗ trợ các phương pháp hiển thị như SSR (Server-side rendering),
SSG (Static site generation) và CSR (Client-side rendering), giúp tối ưu
hiệu suất tải trang và cải thiện trải nghiệm người dùng.
– Việc chọn SSR/SSG còn hỗ trợ tốt cho SEO – phù hợp với các hệ thống
cần hiển thị thông tin công khai.
• Tổ chức kiến trúc linh hoạt, hiện đại:
– Kiến trúc của Next.js dựa trên thành phần (component-based), kết hợp với
routing tự động, giúp phát triển giao diện nhanh chóng và dễ tái sử dụng.
– Ngoài ra, nhờ khả năng tích hợp các thư viện như Tailwind CSS, Zustand,
hoặc Prisma, dự án có thể mở rộng dễ dàng theo nhu cầu thực tế.
• Hiệu suất cao và tối ưu SEO : Do có khả năng render trên server, Next.js giúp
các công cụ tìm kiếm dễ dàng thu thập nội dung, từ đó tối ưu hóa khả năng
SEO, rất cần thiết cho các hệ thống thương mại hoặc giới thiệu sản phẩm.
• Cộng đồng mạnh và được hỗ trợ lâu dài : Next.js được phát triển và duy trì
bởi Vercel – một công ty lớn trong lĩnh vực công nghệ, có cộng đồng sử dụng
rộng rãi, nhiều tài liệu học tập, nên rất thuận tiện trong quá trình phát triển và
xử lý lỗi.
3.1.2 So sánh Nextjs với các Framework khác
Angular
– Điểm mạnh: Angular là một framework toàn diện, cung cấp sẵn nhiều tính
năng như dependency injection, two-way data binding và routing. Phù hợp
với các ứng dụng lớn, có cấu trúc rõ ràng và yêu cầu bảo trì dài hạn.
– Hạn chế: Angular có đường cong học tập cao, cấu trúc phức tạp và đòi hỏi
kiến thức nền tảng vững về TypeScript và hệ thống công cụ.
Vue.js
– Điểm mạnh: Vue.js nổi bật với cú pháp đơn giản, dễ tiếp cận và linh hoạt.
Rất thích hợp cho những dự án nhỏ đến trung bình hoặc đội ngũ phát triển
ít kinh nghiệm.
– Hạn chế: Hệ sinh thái nhỏ hơn và thiếu sự hỗ trợ chính thức từ các công
ty lớn so với React hoặc Next.js.
3.1.3 Nền tảng bổ trợ
React và hệ sinh thái React
Vì Next.js xây dựng dựa trên React, nên có thể sử dụng toàn bộ thư viện trong
hệ sinh thái React như:
– Zustand / Redux / Recoil (quản lý trạng thái)
– React Hook Form / Formik (quản lý form)
– React Query / SWR (quản lý dữ liệu bất đồng bộ)
NextJS được chọn cho đồ án nhờ những đặc điểm vượt trội trong hiệu năng,
được hỗ trợ bởi hệ sinh thái hiện đại và phong phú, giúp việc phát triển ứng dụng
web trở nên nhanh chóng, tối ưu và có khả năng mở rộng cao. Việc tận dụng các
nền tảng bổ trợ giúp tăng hiệu quả làm việc, giảm thời gian phát triển và dễ bảo trì
về sau.
3.1.4 Vai trò của NextJS trong Đồ Án
• Xây dựng giao diện: Tạo các components UI để mô hình cho các chức năng
của dự án.
• Quản lý dữ liệu: Kết nối cơ sở dữ liệu Cloud Firestore để lưu trữ và truy xuất
dữ liêu người dùng một cách hiệu quả. Kết nối tới server Liveblocks để lưu trữ
dữ liệu ghi chú real-time.
• Sử dụng middleware và Clerk để xác thực người dùng và phân quyền truy cập.
• Tích hợp API routes/server actions cho các chức năng chính của dự án.
3.1.5 Bảo mật trong NextJS
Trong đồ án, bảo mật được đặt lên hàng đầu để đảm bảo dữ liệu nhạy cảm không
bị rò rỉ. Một số biện pháp bảo mật được áp dụng:
• Bảo mật route và API: Có thể kiểm tra xác thực người dùng trong middleware
hoặc tại mỗi API route (/api/) bằng cách: xác thực JWT, kiểm tra session từ
NextAuth, hạn chế quyền truy cập theo vai trò (role-based access).
• Middleware kiểm soát truy cập và HTTP Headers an toàn (Có thể tùy chỉnh
headers trong next.config.js để bổ sung các header bảo mật).
• Mã hóa và lưu trữ an toàn: Không lưu thông tin nhạy cảm (mật khẩu, token) ở
phía client hoặc localStorage. Mã hóa thông tin khi truyền tải (qua HTTPS),
và sử dụng dotenv để bảo vệ biến môi trường (.env.local).
3.2 Cơ sở dữ liệu
Firestore là một dịch vụ cơ sở dữ liệu NoSQL dạng tài liệu, được phát triển
bởi Google trong hệ sinh thái Firebase. Firestore cung cấp khả năng lưu trữ dữ liệu
dưới dạng collection (tập hợp) và document (tài liệu), với khả năng đồng bộ hóa
theo thời gian thực giữa các client và backend. Hệ thống được xây dựng trên nền
tảng đám mây của Google (Google Cloud Platform), cho phép mở rộng linh hoạt
và tích hợp nhanh chóng vào các ứng dụng web, mobile hoặc server.
3.2.1 Lý do lựa chọn Firestore
• Tích hợp dễ dàng với hệ thống web sử dụng Next.js và các dịch vụ Firebase
khác như Firebase Authentication.
• Quản lý dữ liệu linh hoạt: Firestore không yêu cầu thiết kế sơ đồ cơ sở dữ liệu
cứng nhắc, phù hợp với dự án có cấu trúc dữ liệu thay đổi theo thời gian.
• Hỗ trợ thời gian thực: Dữ liệu được đồng bộ hóa tức thì giữa client và server,
giúp cải thiện trải nghiệm người dùng khi thao tác trực tiếp với dữ liệu.
• Không cần tự triển khai server riêng: Firestore là dịch vụ không máy chủ
(serverless), giúp giảm thiểu chi phí vận hành và đơn giản hóa quá trình phát
triển backend.
3.2.2 So sánh Cloud Firestore với các giải pháp khác
Cloud Firestore và MySQL
• Ưu điểm của Cloud Firestore: Linh hoạt trong việc lưu trữ dữ liệu không có
cấu trúc cố định, phù hợp với mô hình NoSQL hiện đại. Hỗ trợ đồng bộ thời
gian thực và tích hợp tốt với ứng dụng frontend như Next.js, React.
• Hạn chế: Không phù hợp với các ứng dụng cần xử lý truy vấn phức tạp, có
quan hệ rõ ràng giữa các bảng như hệ thống kế toán, quản lý nhân sự truyền
thống.
Cloud Firestore và PostgreSQL
• Ưu điểm của Cloud Firestore: Dễ dàng mở rộng, không cần quản lý cơ sở hạ
tầng. Phù hợp với ứng dụng có dữ liệu thay đổi nhanh, cấu trúc linh hoạt, và
cần cập nhật thời gian thực.
• Hạn chế: PostgreSQL mạnh hơn về việc đảm bảo tính toàn vẹn dữ liệu (ACID),
các truy vấn liên kết (JOIN), và tính năng phân tích dữ liệu phức tạp.
Cloud Firestore và MongoDB
• Ưu điểm của Cloud Firestore: Tích hợp sâu với Firebase, hỗ trợ các tính năng
như phân quyền truy cập theo người dùng, đồng bộ thời gian thực, và sử dụng
dễ dàng trong môi trường serverless.
• Hạn chế: MongoDB mạnh hơn trong khả năng xử lý lượng dữ liệu lớn, hỗ trợ
truy vấn phức tạp, và tùy chỉnh sâu khi triển khai hệ thống backend riêng.
3.2.3 Tính năng nổi bật của Firestore
• Lưu trữ dữ liệu dạng document: Các tài liệu JSON có thể lưu trữ dữ liệu dạng
lồng nhau, linh hoạt trong thiết kế.
• Đồng bộ thời gian thực: Khi có thay đổi dữ liệu, client được cập nhật ngay lập
tức mà không cần gửi lại yêu cầu (polling).
• Truy vấn nâng cao: Hỗ trợ lọc (where), sắp xếp (orderBy), phân trang (limit,
startAfter) trực tiếp trên server.
• Hỗ trợ offline: Ứng dụng vẫn có thể hoạt động khi mất kết nối, dữ liệu được
đồng bộ lại khi có mạng.
• Khả năng mở rộng toàn cầu: Dữ liệu được sao lưu và phân phối trên hệ thống
Google Cloud, hỗ trợ nhiều vùng địa lý.
3.2.4 Bảo mật trong Firesore
Cloud Firestore hỗ trợ hệ thống bảo mật mạnh mẽ thông qua Firebase Security
Rules, cho phép xác định quyền truy cập đến từng document hoặc collection dựa
trên điều kiện logic như:
• Kiểm tra người dùng đã đăng nhập (request.auth != null).
• So sánh ID người dùng với dữ liệu lưu trong document.
• Giới hạn phương thức truy cập (read, write, update, delete).
Ngoài ra, toàn bộ kết nối giữa client và Firestore đều sử dụng giao thức HTTPS
có mã hóa, giúp đảm bảo an toàn khi truyền dữ liệu.
3.2.5 Tích hợp Firestore với NextJS
Cloud Firestore tích hợp tốt với Next.js nhờ SDK của Firebase. Có thể sử dụng:
• Firebase Client SDK để tương tác với dữ liệu từ phía trình duyệt, phù hợp với
các chức năng như form, bảng dữ liệu, hiển thị danh sách.
• Firebase Admin SDK để xử lý phía server (API route, middleware, SSR), giúp
đảm bảo bảo mật dữ liệu nhạy cảm.
Firestore là một giải pháp cơ sở dữ liệu hiện đại, linh hoạt và mạnh mẽ, đáp ứng
tốt các yêu cầu lưu trữ và quản lý dữ liệu trong đồ án. Khả năng tích hợp dễ dàng
với NextJS và các tính năng nổi bật, dễ dàng mở rộng đã chứng minh rằng Firestore
là lựa chọn phù hợp cho các hệ thống yêu cầu hiệu năng cao và khả năng mở rộng.
3.3 Cộng tác
Liveblocks là một nền tảng cung cấp các API và công cụ hỗ trợ cộng tác thời
gian thực (real-time collaboration) trên các ứng dụng web. Với Liveblocks, lập trình
viên có thể dễ dàng tích hợp các tính năng như chỉnh sửa đồng thời, con trỏ người
dùng, trạng thái online/offline, và chia sẻ trạng thái trong ứng dụng mà không cần
tự xây dựng hạ tầng phức tạp.
3.3.1 Chức năng nổi bật của Liveblocks
• Real-time Presence (Hiển thị trạng thái người dùng theo thời gian thực)
– Giúp theo dõi trạng thái online/offline, vị trí con trỏ, thao tác người dùng
đang làm.
– Hữu ích cho các ứng dụng đa người dùng như soạn thảo văn bản, board,
form,. . .
• Live Storage (Lưu trữ đồng bộ dữ liệu thời gian thực)
– Cho phép nhiều người dùng chỉnh sửa cùng một dữ liệu và được đồng bộ
ngay lập tức.
– Hỗ trợ conflict resolution (giải quyết xung đột) và rollback khi cần.
• Broadcast Events (Sự kiện phát sóng theo thời gian thực)
– Truyền thông tin tức thời giữa các client mà không cần lưu trữ.
– Phù hợp để gửi thông báo, trạng thái nhanh như "User A vừa xoá hình",
"User B đang vẽ".
• Tính năng bảo mật
– Cho phép phân quyền truy cập theo session hoặc user.
– Hỗ trợ xác thực thông qua JWT và dễ dàng tích hợp với hệ thống xác thực
riêng như Firebase Auth, Auth0.
3.3.2 Vai trò của Liveblocks trong đồ án
• Lưu trữ dữ liệu của các bảng ghi chú và trang dữ liệu
• Hiển thị trạng thái người dùng theo thời gian thực dưới dạng con trỏ chuột
3.4 Xác thực và phân quyền
Clerk là một nền tảng cung cấp giải pháp xác thực và quản lý người dùng dành
cho các ứng dụng web hiện đại. Với Clerk, lập trình viên có thể dễ dàng tích hợp
các chức năng như đăng nhập, đăng ký, quản lý hồ sơ người dùng, bảo vệ route,
và xác thực bằng OTP/email/social login mà không cần xây dựng hệ thống auth từ
đầu.
3.4.1 Chức năng nổi bật của Clerk
• Authentication (Xác thực người dùng)
– Hỗ trợ nhiều phương thức xác thực như email/password, OTP, Google,
Facebook, GitHub, v.v.
– Tích hợp dễ dàng với các framework phổ biến như Next.js, React, Vue.
• User Management (Quản lý người dùng)
– Cung cấp giao diện UI sẵn có cho đăng ký, đăng nhập, và quản lý hồ sơ
cá nhân.
– Cho phép tuỳ chỉnh giao diện để phù hợp với thương hiệu riêng.
• Session Management (Quản lý phiên đăng nhập)
– Theo dõi và kiểm soát các phiên hoạt động của người dùng trên nhiều
thiết bị.
– Cho phép đăng xuất từ xa và giới hạn số lượng thiết bị đang hoạt động.
• Role-Based Access Control (Kiểm soát truy cập theo vai trò)
– Phân quyền truy cập tuỳ theo vai trò người dùng hoặc quyền hạn cụ thể.
– Hữu ích trong các ứng dụng có nhiều tầng quyền như admin, editor,
viewer.
• Tính năng bảo mật
– Hỗ trợ xác thực hai yếu tố (2FA) để tăng cường bảo vệ tài khoản.
– Bảo vệ route ở cả phía client và server, tránh truy cập trái phép.
– Mã hoá dữ liệu nhạy cảm và tuân thủ các tiêu chuẩn như GDPR, SOC 2.
– Hỗ trợ Webhooks để theo dõi sự kiện đăng nhập bất thường hoặc cập nhật
người dùng.
3.4.2 Vai trò của Clerk trong đồ án
• Đảm nhận chức năng xác thực và phân quyền người dùng trong hệ thống
• Bảo vệ các route nhạy cảm và quản lý phiên hoạt động của từng người dùng
• Đảm bảo dữ liệu người dùng được bảo mật và tuân thủ tiêu chuẩn bảo vệ thông
tin