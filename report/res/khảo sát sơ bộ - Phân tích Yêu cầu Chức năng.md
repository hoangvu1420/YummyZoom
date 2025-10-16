Khảo sát và Phân tích Yêu cầu Chức năng cho Ứng dụng Giao đồ ăn YummyZoom
Họ và tên sinh viên thực hiện: Hoàng Nguyên Vũ
Mã số sinh viên: 20215171
Mục Lục
1.	Chương 1: Mở đầu	2
1.1.	Giới thiệu dự án YummyZoom	2
1.2.	Mục đích và mục tiêu của tài liệu khảo sát	2
1.3.	Phạm vi khảo sát	2
1.4.	Phương pháp khảo sát	3
2.	Chương 2: Phân tích thị trường và các ứng dụng thực tế	3
2.1.	Tổng quan thị trường giao đồ ăn tại Việt Nam	3
2.2.	Giới thiệu các ứng dụng chính: GrabFood và ShopeeFood	4
2.3.	Bảng ma trận so sánh tính năng tổng quan	4
2.4.	Phân tích sâu và bài học kinh nghiệm cho YummyZoom	6
3.	Chương 3: Xác định yêu cầu chức năng chi tiết cho YummyZoom	7
3.1.	Nguyên tắc xác định yêu cầu và phạm vi dự án	7
3.2.	Yêu cầu chức năng dành cho Khách hàng (Customer)	8
3.3.	Yêu cầu chức năng dành cho Đối tác Nhà hàng (Restaurant)	11
3.4.	Yêu cầu chức năng dành cho Quản trị viên (Admin)	14
4.	Chương 4: Yêu cầu phi chức năng	15
4.1.	Yêu cầu về hiệu năng (Performance)	16
4.2.	Yêu cầu về tính dễ sử dụng (Usability - UI/UX)	16
4.3.	Yêu cầu về độ tin cậy và bảo mật (Reliability & Security)	17
5.	Chương 5: Kết luận	17
5.1.	Tóm tắt kết quả khảo sát	17
5.2.	Chốt lại phạm vi chức năng cuối cùng của dự án YummyZoom	17
5.3.	Hướng phát triển trong tương lai	18

1.	Chương 1: Mở đầu
1.1.	Giới thiệu dự án YummyZoom
Trong bối cảnh cuộc cách mạng công nghiệp 4.0, lĩnh vực công nghệ thực phẩm (FoodTech), đặc biệt là các nền tảng giao đồ ăn trực tuyến, đã và đang phát triển một cách bùng nổ. Sự tiện lợi, đa dạng và nhanh chóng đã đưa các ứng dụng này trở thành một phần không thể thiếu trong đời sống hiện đại. Tuy nhiên, thị trường cũng chứng kiến sự cạnh tranh khốc liệt, đòi hỏi các sản phẩm mới phải có sự đầu tư kỹ lưỡng về trải nghiệm người dùng và các tính năng độc đáo.
Dự án YummyZoom ra đời với mục tiêu xây dựng một nền tảng ứng dụng giao đồ ăn nhanh, tập trung vào việc mang lại trải nghiệm mượt mà và tiện lợi, đặc biệt hướng đến đối tượng khách hàng mục tiêu là sinh viên đại học và nhân viên văn phòng. Đây là nhóm người dùng có nhu cầu đặt hàng theo nhóm cao, đặc biệt trong các giờ nghỉ trưa ngắn, và luôn tìm kiếm một giải pháp không chỉ nhanh chóng mà còn đơn giản hóa việc thanh toán. Dự án được thực hiện trong khuôn khổ của một đồ án tốt nghiệp, mô phỏng một hệ thống hoàn chỉnh kết nối ba đối tượng chính: 
	Khách hàng (Customer): Người dùng cuối có nhu cầu đặt món ăn.
	Đối tác Nhà hàng (Restaurant): Các cơ sở kinh doanh cung cấp món ăn.
	Quản trị viên (Admin): Người quản lý và vận hành toàn bộ hệ thống.
Để đáp ứng nhu cầu đặc thù của nhóm khách hàng mục tiêu, điểm nhấn khác biệt lớn nhất của YummyZoom là việc phát triển tính năng TeamCart (Giỏ hàng nhóm). Tính năng này được thiết kế để giải quyết trực tiếp "nỗi đau" khi đặt hàng chung mà các nhóm bạn bè, đồng nghiệp thường xuyên gặp phải. Với TeamCart, YummyZoom không chỉ mang đến sự tiện lợi về quy trình mà còn thúc đẩy sự gắn kết, biến mỗi bữa ăn chung trở thành một trải nghiệm xã hội vui vẻ, hiệu quả và không còn gánh nặng về tài chính.
1.2.	Mục đích và mục tiêu của tài liệu khảo sát
Tài liệu này được biên soạn nhằm mục đích xây dựng một nền tảng vững chắc cho quá trình phát triển dự án YummyZoom, thông qua việc khảo sát và phân tích các yêu cầu chức năng một cách chi tiết và có hệ thống.
Để đạt được mục đích trên, tài liệu sẽ tập trung vào các mục tiêu cụ thể sau:
	Phân tích các ứng dụng giao đồ ăn hàng đầu trên thị trường Việt Nam để hiểu rõ các tiêu chuẩn ngành, các mô hình thành công và xu hướng phát triển.
	Xác định các tính năng cốt lõi mà một ứng dụng hiện đại cần có để đáp ứng kỳ vọng cơ bản của người dùng.
	Tìm kiếm cơ hội để tạo ra sự khác biệt và lợi thế cạnh tranh, đặc biệt là làm rõ giá trị của tính năng TeamCart so với các giải pháp đặt nhóm hiện có.
	Định hình và chốt lại phạm vi chức năng cuối cùng cho dự án YummyZoom, đảm bảo tính khả thi trong khuôn khổ một đồ án tốt nghiệp nhưng vẫn đủ sức cạnh tranh về mặt ý tưởng.
1.3.	Phạm vi khảo sát
Để đảm bảo tính tập trung và hiệu quả, quá trình khảo sát sẽ được giới hạn trong phạm vi sau:
Phạm vi chức năng: Khảo sát tập trung vào các tính năng dành cho ba nhóm đối tượng chính: Khách hàng, Nhà hàng và Quản trị viên. Các quy trình nghiệp vụ cốt lõi như tìm kiếm, đặt hàng, quản lý thực đơn, xử lý đơn hàng và quản lý khuyến mãi sẽ được ưu tiên phân tích.
Phạm vi thị trường: Khảo sát các ứng dụng phổ biến và có thị phần lớn tại thị trường Việt Nam như GrabFood, ShopeeFood để đảm bảo tính phù hợp với bối cảnh và thói quen người dùng trong nước.
Giới hạn khảo sát: Tài liệu sẽ không đi sâu vào việc phân tích và thiết kế hệ thống dành cho tài xế (shipper), bao gồm các nghiệp vụ như đăng ký tài xế, thuật toán chỉ định đơn hàng, hay quản lý thu nhập của tài xế. Đây là một quyết định có chủ đích nhằm tập trung nguồn lực giới hạn của đồ án vào việc hoàn thiện trải nghiệm cốt lõi cho khách hàng và nhà hàng. Quá trình giao hàng trong dự án sẽ được mô phỏng (simulated).
1.4.	Phương pháp khảo sát
Để thu thập thông tin một cách toàn diện và khách quan, tài liệu áp dụng kết hợp các phương pháp nghiên cứu sau:
Phân tích tài liệu ban đầu: Nghiên cứu tài liệu Phác thảo chức năng để nắm vững các ý tưởng và chức năng mục tiêu đã được phác thảo cho YummyZoom.
Nghiên cứu và trải nghiệm thực tế (Primary Research): Tải và sử dụng trực tiếp các ứng dụng của các nền tảng thực tế (GrabFood, ShopeeFood) để có cái nhìn trực quan về luồng hoạt động, giao diện người dùng (UI), và trải nghiệm người dùng (UX).
Nghiên cứu thứ cấp (Secondary Research): Thu thập thông tin từ các nguồn công khai như các bài viết phân tích thị trường, báo cáo ngành, các bài đánh giá của người dùng trên App Store/Google Play và các blog công nghệ để hiểu rõ điểm mạnh, điểm yếu của từng nền tảng.
Tổng hợp và Phân tích: So sánh, tổng hợp dữ liệu thu thập được từ các phương pháp trên để xây dựng ma trận tính năng, từ đó rút ra các yêu cầu chức năng phù hợp và khả thi nhất cho dự án YummyZoom.
2.	Chương 2: Phân tích thị trường và các ứng dụng thực tế
2.1.	Tổng quan thị trường giao đồ ăn tại Việt Nam
Thị trường giao đồ ăn trực tuyến tại Việt Nam đang trải qua giai đoạn phát triển vô cùng sôi động và đầy tiềm năng. Theo báo cáo từ Momentum Works, thị trường giao đồ ăn Việt Nam đã chứng kiến mức tăng trưởng cao nhất khu vực Đông Nam Á, đạt 26% trong năm qua, với tổng giá trị giao dịch (GMV) mở rộng từ 1,4 tỷ USD vào năm 2023 lên 1,8 tỷ USD vào năm 2024. Sự tăng trưởng ấn tượng này được thúc đẩy bởi sự phổ biến của điện thoại thông minh, lối sống bận rộn của người dân đô thị và nhu cầu ngày càng cao về sự tiện lợi trong ăn uống.
Hiện tại, thị trường đang bị thống trị bởi hai "gã khổng lồ" là GrabFood và ShopeeFood, tạo nên một thế song cực khi chiếm giữ gần như toàn bộ thị phần. Cụ thể, vào năm 2024, GrabFood chiếm 48% thị phần, trong khi ShopeeFood bám sát ngay sau với 47%. Sự cạnh tranh khốc liệt này chủ yếu xoay quanh các cuộc chiến về giá, các chương trình khuyến mãi sâu và nỗ lực mở rộng hệ sinh thái dịch vụ để giữ chân người dùng. Thói quen đặt đồ ăn qua ứng dụng đã trở nên phổ biến, đặc biệt là cho bữa trưa, khi có đến 30% người được khảo sát lựa chọn hình thức này. Điều này cho thấy tiềm năng to lớn nhưng cũng đặt ra thách thức không nhỏ cho bất kỳ nền tảng mới nào muốn gia nhập thị trường.
2.2.	Giới thiệu các ứng dụng chính: GrabFood và ShopeeFood
Để xây dựng chiến lược phù hợp cho YummyZoom, việc phân tích sâu hai nền tảng đang dẫn đầu thị trường là điều kiện tiên quyết.
	GrabFood: Sức mạnh từ hệ sinh thái "Siêu ứng dụng"
GrabFood không chỉ là một ứng dụng giao đồ ăn đơn thuần mà là một phần quan trọng trong "siêu ứng dụng" Grab. Lợi thế cạnh tranh cốt lõi của GrabFood đến từ hệ sinh thái toàn diện bao gồm di chuyển (GrabBike, GrabCar), giao hàng (GrabExpress) và thanh toán (ví điện tử Moca). Sức mạnh này cho phép Grab tận dụng một tệp khách hàng khổng lồ có sẵn, đội ngũ tài xế đông đảo, và dữ liệu người dùng phong phú để tối ưu hóa dịch vụ. Chiến lược giữ chân người dùng của Grab rất hiệu quả, tập trung vào các chương trình khách hàng thân thiết như hệ thống tích điểm GrabRewards và gói hội viên GrabUnlimited mang lại các ưu đãi độc quyền, đặc biệt là miễn phí giao hàng. GrabFood thu hút nhóm người dùng từ 35 tuổi trở lên, những người ưu tiên sự tiện lợi, tin cậy và các bữa ăn đầy đủ.

	ShopeeFood: Thống lĩnh bằng văn hóa "săn sale"
ShopeeFood, với tiền thân là Now.vn, có lợi thế là một trong những nền tảng tiên phong trên thị trường. Sau khi được mua lại bởi SEA Group, ShopeeFood đã được tích hợp sâu vào hệ sinh thái thương mại điện tử Shopee và ví điện tử ShopeePay. Điểm mạnh lớn nhất của ShopeeFood là khả năng tung ra vô số các chương trình khuyến mãi, voucher giảm giá và freeship, đánh trúng vào tâm lý yêu thích "săn sale" của người tiêu dùng Việt. Nền tảng này tận dụng rất tốt các chiến dịch mua sắm lớn của Shopee (như 11/11, 12/12) để thu hút người dùng. Đối tượng khách hàng mục tiêu của ShopeeFood trẻ hơn, chủ yếu từ 16-24 tuổi, và thường ưu tiên các món ăn vặt, trà sữa, đồ ăn nhanh.
2.3.	Bảng ma trận so sánh tính năng tổng quan
Dưới đây là bảng phân tích và so sánh các tính năng chính giữa YummyZoom (dựa trên tài liệu thiết kế) với hai nền tảng hàng đầu là GrabFood và ShopeeFood.
Nhóm tính năng	Tính năng chi tiết	YummyZoom (Kế hoạch)	GrabFood	ShopeeFood	Ghi chú/Phân tích
1. Tài khoản & Xác thực	Đăng ký/Đăng nhập (SĐT, Email, MXH)	Có	Có	Có	Tính năng cơ bản, bắt buộc phải có để đảm bảo tính quen thuộc.
	Quản lý hồ sơ, địa chỉ, thanh toán	Có	Có	Có	Tương tự, là yêu cầu tối thiểu cho trải nghiệm người dùng.
2. Khám phá & Tìm kiếm	Thanh tìm kiếm (theo món/quán)	Có	Có	Có	Chức năng cốt lõi.
	Bộ lọc (giá, rating, khuyến mãi)	Có	Có	Có	Giúp người dùng ra quyết định nhanh hơn.
	Gợi ý theo danh mục, bộ sưu tập	Cơ bản (không ứng dụng AI)	Rất mạnh	Rất mạnh	Các nền tảng thực tế đầu tư mạnh vào AI để cá nhân hóa gợi ý. YummyZoom sẽ bắt đầu với các danh mục tĩnh.
3. Nhà hàng & Thực đơn	Xem thông tin chi tiết nhà hàng	Có	Có	Có	Quan trọng để xây dựng lòng tin.
	Xem menu, giá, hình ảnh món ăn	Có	Có	Có	Yêu cầu cơ bản.
	Tùy chọn món ăn (size, topping...)	Có	Có	Có	Cần thiết cho việc cá nhân hóa đơn hàng.
4. Giỏ hàng & Thanh toán	Quản lý giỏ hàng	Có	Có	Có	Cốt lõi.
	Áp dụng mã khuyến mãi/coupon	Có	Có	Có	Tính năng không thể thiếu trong thị trường cạnh tranh bằng giá.
	Đa dạng phương thức thanh toán	Tích hợp Stripe cơ bản	Có tích hợp nhiều phương thức 	Có tích hợp nhiều phương thức 	YummyZoom sẽ mô phỏng hoặc tích hợp các cổng thanh toán phổ biến.
5. Theo dõi & Quản lý đơn	Cập nhật trạng thái đơn hàng	Cập nhật các bước	Cập nhật các bước	Cập nhật các bước	Yêu cầu tối thiểu để người dùng biết tình trạng đơn hàng.
	Theo dõi tài xế real-time trên bản đồ	Chỉ mô phỏng	Rất mạnh	Rất mạnh	Thực hiện module cho tài xế sẽ rất phức tạp. Việc mô phỏng là hợp lý cho phạm vi đồ án.
	Lịch sử đơn hàng & Đặt lại	Có	Có	Có	Tăng tính tiện lợi cho khách hàng cũ.
6. Tương tác & Giữ chân	Đánh giá và xếp hạng (Rating & Review)	Có	Có	Có	Yếu tố quan trọng ảnh hưởng đến quyết định của người dùng khác.
	Chương trình khách hàng thân thiết	Không	Có (Grab Rewards)	Có voucher	Grab mạnh về hệ thống điểm thưởng. Đây là hướng phát triển tương lai cho YummyZoom.
	Gói thành viên/đăng ký tháng	Không	Có (Grab Unlimited)	Không	Mô hình kinh doanh hiệu quả để tăng lòng trung thành, nhưng phức tạp để triển khai ban đầu.
7. Tính năng đặc biệt	Đặt hàng nhóm (Group Order)	Có (TeamCart)	Có	Có	Cả 3 đều có. Tuy nhiên, điểm khác biệt nằm ở quy trình thực hiện.
	Quy trình thanh toán nhóm	Từng thành viên tự trả	Chủ nhóm trả toàn bộ	Chủ nhóm trả toàn bộ	Đây là lợi thế cạnh tranh cốt lõi của YummyZoom.
2.4.	Phân tích sâu và bài học kinh nghiệm cho YummyZoom
Từ việc phân tích thị trường và hai nền tảng lớn, chúng ta có thể rút ra những bài học chiến lược quan trọng để định hình cho dự án YummyZoom, đặc biệt khi xác định đối tượng khách hàng mục tiêu là sinh viên đại học và nhân viên văn phòng. Đây là nhóm người dùng có nhu cầu đặt đồ ăn thường xuyên, đặc biệt là vào giờ nghỉ trưa ngắn, cần một giải pháp tiện lợi, nhanh chóng và có khả năng kết nối tập thể.
1.	Phải đáp ứng các tiêu chuẩn cơ bản: Phân tích ma trận tính năng cho thấy, các chức năng từ quản lý tài khoản, tìm kiếm, đặt hàng, thanh toán đến xem lại lịch sử là những yêu cầu tối thiểu. Người dùng đã quen với các luồng thao tác này và YummyZoom bắt buộc phải cung cấp một trải nghiệm mượt mà, quen thuộc ở các tính năng cốt lõi này để không tạo ra rào cản khi sử dụng.
2.	Không thể cạnh tranh trực diện về giá và hệ sinh thái: YummyZoom, với quy mô của một đồ án, không có ngân sách khổng lồ để "đốt tiền" vào khuyến mãi như ShopeeFood hay một hệ sinh thái đa dịch vụ vững chắc như Grab. Do đó, việc sao chép mô hình của họ và cạnh tranh trực diện là bất khả thi và không phải là một chiến lược khôn ngoan.
3.	Tập trung vào thị trường ngách và giải quyết tốt một vấn đề cụ thể: Đây chính là con đường phù hợp nhất cho YummyZoom. Phân tích cho thấy dù các ứng dụng có tính năng "Đặt nhóm", quy trình của họ vẫn còn một "nỗi đau" lớn: một người (chủ nhóm) phải đứng ra đặt hàng và thanh toán cho tất cả, sau đó phải mất công thu lại tiền từ mỗi thành viên. Tính năng TeamCart của YummyZoom được thiết kế để giải quyết chính xác vấn đề này bằng cách cho phép mỗi thành viên tự thêm món và tự thanh toán phần của mình. Đây không chỉ là một cải tiến nhỏ mà là một sự thay đổi quy trình mang lại giá trị thực tế, tạo ra một lợi thế cạnh tranh rõ ràng trong một phân khúc người dùng cụ thể (nhân viên văn phòng, nhóm bạn bè).
4.	Sự hợp lý của việc tinh giản các tính năng phức tạp: Hệ thống quản lý và theo dõi tài xế theo thời gian thực trên bản đồ là một tính năng cực kỳ phức tạp, đòi hỏi đầu tư lớn về công nghệ và hạ tầng. Việc YummyZoom quyết định mô phỏng quá trình giao hàng thay vì xây dựng thật là một lựa chọn chiến lược thông minh. Điều này cho phép dự án tập trung toàn bộ nguồn lực vào việc hoàn thiện trải nghiệm đặt hàng, đặc biệt là tính năng TeamCart, thay vì dàn trải cho một module quá sức với phạm vi của một đồ án tốt nghiệp.
3.	Chương 3: Xác định yêu cầu chức năng chi tiết cho YummyZoom
Từ những phân tích sâu sắc về thị trường và các ứng dụng thực tế ở chương trước, chương này sẽ đi vào việc xác định bộ yêu cầu chức năng chi tiết cho ứng dụng YummyZoom. Quá trình này không chỉ đơn thuần là liệt kê các tính năng, mà được xây dựng dựa trên một bộ nguyên tắc và một phạm vi dự án rõ ràng, nhằm đảm bảo tính khả thi, sự tập trung và khả năng tạo ra giá trị khác biệt.
3.1.	Nguyên tắc xác định yêu cầu và phạm vi dự án
Để định hình một sản phẩm có tính cạnh tranh trong khuôn khổ một đồ án tốt nghiệp, YummyZoom sẽ được phát triển dựa trên hai nguyên tắc cốt lõi sau:
  1.  Tập trung vào Sản phẩm khả dụng tối thiểu (Minimum Viable Product - MVP): Phân tích ở Chương 2 đã chỉ ra một loạt các tính năng được coi là "tiêu chuẩn ngành". Do đó, YummyZoom sẽ tập trung nguồn lực để xây dựng hoàn chỉnh một luồng nghiệp vụ cốt lõi, đảm bảo người dùng có thể thực hiện một vòng lặp trải nghiệm hoàn chỉnh: từ tìm kiếm nhà hàng, tùy chọn món ăn, đặt hàng, thanh toán (mô phỏng) cho đến khi nhận được thông báo đơn hàng hoàn tất. Việc hoàn thiện MVP giúp đảm bảo sản phẩm có nền tảng vững chắc và mang lại trải nghiệm quen thuộc, không gây khó khăn cho người dùng mới.
  2.  Tạo sự khác biệt thông qua giải quyết vấn đề ở thị trường ngách (Niche Market Focus): Thay vì cạnh tranh trực diện và dàn trải trên mọi mặt trận, YummyZoom xác định chiến lược cạnh tranh bằng cách tập trung giải quyết một vấn đề cụ thể một cách vượt trội. Như đã phân tích, quy trình đặt hàng nhóm hiện tại trên thị trường vẫn còn tồn tại "nỗi đau" về khâu thanh toán. Do đó, dự án sẽ dồn tâm huyết vào việc thiết kế và hoàn thiện tính năng TeamCart, biến nó thành một lợi thế cạnh tranh độc đáo và là điểm nhấn chính của sản phẩm.
Dựa trên các nguyên tắc trên, phạm vi của dự án YummyZoom được xác định rõ ràng như sau để đảm bảo tính thực tế và tập trung:
A. Các hạng mục trong phạm vi phát triển (In-scope):
	Hệ thống xác thực và quản lý tài khoản: Xây dựng đầy đủ chức năng đăng ký, đăng nhập, quản lý thông tin cá nhân cho cả ba vai trò: Khách hàng, Đối tác Nhà hàng và Quản trị viên.
	Luồng đặt hàng hoàn chỉnh cho khách hàng: Bao gồm các chức năng tìm kiếm, xem menu, tùy chỉnh món ăn, quản lý giỏ hàng, áp dụng coupon và thực hiện thanh toán mô phỏng.
	Tính năng đặt hàng nhóm (TeamCart): Phát triển hoàn chỉnh luồng nghiệp vụ cho TeamCart, từ việc tạo nhóm, mời thành viên, cho đến việc mỗi thành viên tự thanh toán phần của mình.
	Hệ thống quản lý cho Đối tác Nhà hàng: Cung cấp các công cụ cần thiết để nhà hàng có thể quản lý thông tin, thực đơn, coupon và xử lý các đơn hàng nhận được từ nền tảng.
	Bảng điều khiển cơ bản cho Quản trị viên: Xây dựng giao diện cho phép quản trị viên theo dõi các hoạt động chính trên hệ thống, quản lý tài khoản người dùng và nhà hàng.
B. Các hạng mục nằm ngoài phạm vi phát triển (Out-of-scope):
Để đảm bảo dự án được hoàn thành đúng tiến độ và chất lượng trong giới hạn nguồn lực, YummyZoom sẽ chủ đích không triển khai các hạng mục phức tạp sau:
	Module ứng dụng dành cho tài xế (Driver App): Toàn bộ hệ thống liên quan đến việc đăng ký, quản lý và điều phối tài xế sẽ không được xây dựng.
	Hệ thống theo dõi vị trí tài xế theo thời gian thực (Real-time GPS Tracking): Như đã phân tích, đây là tính năng cực kỳ phức tạp. Thay vào đó, quá trình giao hàng sẽ được mô phỏng thông qua việc cập nhật các trạng thái đơn hàng (Đang chuẩn bị -> Sẵn sàng -> Đang giao).
	Các thuật toán gợi ý bằng Trí tuệ nhân tạo (AI-powered Recommendation): Hệ thống sẽ không tích hợp các thuật toán máy học phức tạp để gợi ý món ăn/nhà hàng. Các gợi ý sẽ dựa trên các danh mục được quản lý thủ công.
	Chương trình khách hàng thân thiết (Loyalty/Reward Programs): Các hệ thống tích điểm, đổi quà sẽ được xem xét như một hướng phát triển trong tương lai, không thuộc phạm vi của đồ án này.
	Tích hợp cổng thanh toán thực tế: Quá trình thanh toán sẽ được mô phỏng sử dụng cổng thanh toán test của Stripe để hoàn thiện luồng nghiệp vụ mà không cần xử lý các giao dịch tài chính thực.
3.2.	Yêu cầu chức năng dành cho Khách hàng (Customer)
Các yêu cầu chức năng dành cho Khách hàng được thiết kế nhằm mang lại một trải nghiệm đặt hàng liền mạch, tiện lợi và cạnh tranh. Các chức năng này được xác định dựa trên việc phân tích các tiêu chuẩn ngành từ GrabFood, ShopeeFood và chiến lược tạo sự khác biệt của YummyZoom.
ID chức năng: CUS-01
Tên chức năng: Quản lý Tài khoản Cá nhân
Vai trò người dùng: Khách hàng
Mô tả:
	Cho phép người dùng mới đăng ký tài khoản bằng số điện thoại hoặc email.
	Cho phép người dùng đã có tài khoản đăng nhập vào hệ thống.
	Cung cấp khu vực quản lý hồ sơ, nơi người dùng có thể xem và chỉnh sửa thông tin cá nhân như tên, ảnh đại diện.
	Cho phép người dùng lưu và quản lý nhiều địa chỉ giao hàng để thuận tiện cho việc đặt hàng sau này.
Nguồn gốc: Tiêu chuẩn ngành.
Luận giải: Đây là nhóm chức năng cơ bản và bắt buộc phải có ở bất kỳ ứng dụng thương mại điện tử nào. Phân tích tại Mục 2.3 cho thấy tất cả các ứng dụng thực tế đều cung cấp các tính năng này một cách hoàn thiện. Việc triển khai đầy đủ chức năng quản lý tài khoản là yêu cầu tối thiểu để đảm bảo tính quen thuộc và tiện lợi cho người dùng.
ID chức năng: CUS-02
Tên chức năng: Tìm kiếm và Khám phá Nhà hàng
Vai trò người dùng: Khách hàng
Mô tả:
	Cung cấp thanh tìm kiếm thông minh, cho phép người dùng tìm kiếm theo tên nhà hàng hoặc tên món ăn.
	Hiển thị danh sách các nhà hàng dựa trên các danh mục được định sẵn (ví dụ: Cơm văn phòng, Trà sữa, Món Hàn).
	Cung cấp bộ lọc chi tiết giúp người dùng thu hẹp kết quả tìm kiếm theo các tiêu chí như: xếp hạng (rating), mức giá, chương trình khuyến mãi.
Nguồn gốc: Tiêu chuẩn ngành.
Luận giải: Chức năng tìm kiếm và lọc là công cụ cốt lõi giúp người dùng khám phá và ra quyết định. Như đã phân tích ở Mục 2.4, YummyZoom không cạnh tranh bằng thuật toán gợi ý AI phức tạp, do đó việc cung cấp một công cụ tìm kiếm và lọc mạnh mẽ, trực quan là điều kiện tiên quyết để đảm bảo trải nghiệm người dùng ở mức MVP.
ID chức năng: CUS-03
Tên chức năng: Xem chi tiết Nhà hàng và Thực đơn
Vai trò người dùng: Khách hàng
Mô tả:
	Hiển thị trang thông tin chi tiết của nhà hàng, bao gồm: tên, logo, địa chỉ, giờ hoạt động, mô tả và đánh giá từ những người dùng khác.
	Hiển thị thực đơn (menu) một cách rõ ràng, có hình ảnh, tên món, mô tả chi tiết và giá bán.
Nguồn gốc: Cốt lõi nghiệp vụ.
Luận giải: Việc cung cấp thông tin đầy đủ và minh bạch là yếu tố sống còn để xây dựng lòng tin và thúc đẩy người dùng đặt hàng. Đây là chức năng không thể thiếu trong luồng nghiệp vụ chính của ứng dụng.
ID chức năng: CUS-04
Tên chức năng: Đặt hàng và Tùy chỉnh Món ăn
Vai trò người dùng: Khách hàng
Mô tả:
	Cho phép người dùng chọn một món ăn từ thực đơn và thêm vào giỏ hàng.
	Cung cấp các tùy chọn để cá nhân hóa món ăn (ví dụ: chọn kích cỡ L/M/S, mức độ cay, thêm topping, chọn mức đường/đá).
	Hiển thị trạng thái món ăn nếu nhà hàng cập nhật "hết hàng".
Nguồn gốc: Cốt lõi nghiệp vụ & Tiêu chuẩn ngành.
Luận giải: Đây là hành động trung tâm của toàn bộ ứng dụng. Khả năng tùy chỉnh món ăn là một tiêu chuẩn mà người dùng mong đợi từ các nền tảng hiện đại, giúp đáp ứng nhu cầu cá nhân hóa của họ.
ID chức năng: CUS-05
Tên chức năng: Quản lý Giỏ hàng và Thanh toán
Vai trò người dùng: Khách hàng
Mô tả:
	Cho phép người dùng xem lại toàn bộ các món ăn đã chọn trong giỏ hàng, điều chỉnh số lượng hoặc xóa món.
	Cung cấp ô nhập mã khuyến mãi/coupon và hiển thị số tiền được giảm ngay lập tức.
	Người dùng chọn địa chỉ giao hàng và phương thức thanh toán (mô phỏng).
	Xác nhận đơn hàng để gửi đến nhà hàng.
Nguồn gốc: Cốt lõi nghiệp vụ.
Luận giải: Chức năng này hoàn tất vòng lặp đặt hàng. Việc tích hợp tính năng áp dụng coupon là bắt buộc, như đã phân tích về sự cạnh tranh khốc liệt bằng khuyến mãi ở Chương 2. Quá trình thanh toán sẽ được mô phỏng để phù hợp với phạm vi dự án đã nêu ở Mục 3.1.
ID chức năng: CUS-06
Tên chức năng: Theo dõi Đơn hàng (Mô phỏng)
Vai trò người dùng: Khách hàng
Mô tả:
	Sau khi đặt hàng thành công, người dùng có thể theo dõi trạng thái đơn hàng của mình theo thời gian thực.
	Các trạng thái được cập nhật tuần tự: Đã đặt → Đã chấp nhận → Đang chuẩn bị → Sẵn sàng giao → Đã giao hàng.
Nguồn gốc: Phù hợp phạm vi dự án.
Luận giải: Phân tích ở Mục 2.3 cho thấy theo dõi tài xế trên bản đồ là một tính năng tiêu chuẩn nhưng cực kỳ phức tạp. Để đảm bảo tính khả thi cho đồ án, YummyZoom chọn phương án mô phỏng quá trình này thông qua việc cập nhật trạng thái. Điều này vừa cung cấp thông tin cần thiết cho người dùng, vừa nằm trong phạm vi nguồn lực cho phép đã được xác định tại Mục 3.1.
ID chức năng: CUS-07
Tên chức năng: Lịch sử Đơn hàng và Đặt lại
Vai trò người dùng: Khách hàng
Mô tả:
	Cho phép người dùng xem lại danh sách tất cả các đơn hàng đã đặt trong quá khứ.
	Cung cấp tính năng "Đặt lại" (Re-order) chỉ với một cú nhấp chuột, giúp người dùng nhanh chóng đặt lại đơn hàng yêu thích mà không cần tìm kiếm và chọn lại từ đầu.
Nguồn gốc: Tiêu chuẩn ngành.
Luận giải: Đây là một tính năng quan trọng giúp tăng tính tiện lợi và giữ chân người dùng. Cả GrabFood và ShopeeFood đều làm rất tốt điều này, do đó YummyZoom cần phải có để không bị thua kém về trải nghiệm cơ bản.
ID chức năng: CUS-08
Tên chức năng: Đánh giá và Xếp hạng Nhà hàng
Vai trò người dùng: Khách hàng
Mô tả:
	Sau khi đơn hàng được hoàn thành, người dùng có thể xếp hạng nhà hàng theo thang điểm từ 1 đến 5 sao.
	Người dùng có thể để lại bình luận chi tiết về trải nghiệm của mình.
Nguồn gốc: Tiêu chuẩn ngành.
Luận giải: Tính năng này tạo ra "bằng chứng xã hội" (social proof), một yếu tố cực kỳ quan trọng ảnh hưởng đến quyết định của những khách hàng sau. Nó giúp xây dựng một cộng đồng người dùng và tăng tính minh bạch, tin cậy cho nền tảng.
ID chức năng: CUS-09
Tên chức năng: Đặt hàng nhóm (TeamCart)
Vai trò người dùng: Khách hàng (trong vai trò Host và Member)
Mô tả:
	Cho phép một người dùng (Host) tạo một giỏ hàng chung và mời người khác tham gia qua một liên kết.
	Các thành viên (Members) có thể tham gia và tự thêm các món ăn mình muốn vào giỏ hàng chung.
	Host có quyền khóa giỏ hàng để chốt đơn.
Điểm cốt lõi: Mỗi thành viên sẽ tự thực hiện thanh toán cho phần món ăn của riêng mình.
Khi tất cả đã thanh toán, Host sẽ xác nhận để gửi đơn hàng cuối cùng đến nhà hàng.
Nguồn gốc: Lợi thế cạnh tranh & Giải quyết vấn đề thị trường ngách.
Luận giải: Đây là tính năng chiến lược của YummyZoom. Dựa trên phân tích ở Mục 2.4, tính năng này được thiết kế để cung cấp một giải pháp vượt trội so với tính năng "Group Order" hiện có của các nền tảng, bằng cách giải quyết triệt để "nỗi đau" về việc một người phải trả tiền cho tất cả. TeamCart sẽ là điểm nhấn khác biệt, tạo ra giá trị thực tiễn và là luận điểm chính bảo vệ cho tính sáng tạo của dự án.

3.3.	Yêu cầu chức năng dành cho Đối tác Nhà hàng (Restaurant)
Đối tác Nhà hàng là một trụ cột của hệ sinh thái YummyZoom. Do đó, việc cung cấp một bộ công cụ quản lý mạnh mẽ, trực quan và hiệu quả là yếu tố sống còn để thu hút và giữ chân họ. Các yêu cầu chức năng dưới đây được xác định nhằm đáp ứng các nhu cầu vận hành thiết yếu của nhà hàng trên nền tảng số.
ID chức năng: RES-01
Tên chức năng: Đăng ký Đối tác Nhà hàng mới
Vai trò người dùng: Đối tác Nhà hàng (Chủ nhà hàng)
Mô tả:
	Cung cấp một biểu mẫu đăng ký trực tuyến cho các chủ nhà hàng muốn đưa quán ăn của mình lên nền tảng YummyZoom.
	Biểu mẫu yêu cầu các thông tin xác thực cần thiết như: tên nhà hàng, địa chỉ, thông tin liên hệ, loại hình ẩm thực, và cho phép tải lên các giấy tờ liên quan (ví dụ: giấy phép kinh doanh, chứng nhận an toàn thực phẩm).
	Sau khi nộp đơn, tài khoản nhà hàng sẽ ở trạng thái "Chờ duyệt" và được chuyển đến cho Quản trị viên xem xét.
	Hồ sơ nhà hàng sẽ không được hiển thị công khai trên ứng dụng cho đến khi được Quản trị viên phê duyệt.
Nguồn gốc: Cốt lõi nghiệp vụ & Quy trình vận hành nền tảng.
Luận giải: Đây là chức năng "cổng vào" (gatekeeping) mang tính sống còn, đảm bảo chất lượng và tính hợp pháp của các đối tác trên YummyZoom. Chức năng này cho phép nền tảng kiểm soát và xác minh các nhà hàng trước khi họ tiếp cận khách hàng, là yếu tố then chốt để xây dựng lòng tin và bảo vệ người dùng. Nó chính thức hóa quy trình onboarding cho đối tác mới và liên kết trực tiếp với nghiệp vụ quản lý của vai trò Quản trị viên.
ID chức năng: RES-02
Tên chức năng: Quản lý Hồ sơ Nhà hàng
Vai trò người dùng: Đối tác Nhà hàng
Mô tả:
	Cho phép nhà hàng thiết lập và cập nhật các thông tin cơ bản: tên thương hiệu, logo, địa chỉ, số điện thoại liên hệ.
	Cung cấp công cụ để cài đặt giờ mở cửa, đóng cửa cho các ngày trong tuần.
	Cho phép nhà hàng viết một đoạn mô tả ngắn và định nghĩa loại hình ẩm thực (ví dụ: Món Việt, Món Hàn, Đồ ăn nhanh).
Nguồn gốc: Cốt lõi nghiệp vụ.
Luận giải: Đây là chức năng nền tảng, được ví như "mặt tiền kỹ thuật số" của nhà hàng trên ứng dụng YummyZoom. Việc cung cấp thông tin chính xác và đầy đủ là bước đầu tiên và bắt buộc để nhà hàng có thể hiện diện và kinh doanh trên nền tảng, cũng như để khách hàng có đủ thông tin để ra quyết định.
ID chức năng: RES-03
Tên chức năng: Quản lý Thực đơn (Menu)
Vai trò người dùng: Đối tác Nhà hàng
Mô tả:
	Cung cấp giao diện để thêm, sửa, xóa các món ăn trong thực đơn.
	Cho phép cập nhật chi tiết cho từng món: tên, hình ảnh, mô tả, giá bán.
	Tích hợp công tắc "Tạm hết hàng" (Out of stock) để nhà hàng có thể chủ động bật/tắt hiển thị của món ăn một cách nhanh chóng.
	Hỗ trợ tạo các tùy chọn cho món ăn (ví dụ: size M/L, thêm topping, mức độ đường/đá) để khách hàng có thể cá nhân hóa đơn hàng.
Nguồn gốc: Cốt lõi nghiệp vụ.
Luận giải: Thực đơn là "trái tim" của một nhà hàng. Cung cấp một công cụ quản lý thực đơn linh hoạt và mạnh mẽ là yêu cầu thiết yếu, giúp nhà hàng phản ánh chính xác các sản phẩm họ cung cấp và chủ động trong hoạt động kinh doanh hàng ngày (ví dụ: khi một nguyên liệu bất ngờ hết).
ID chức năng: RES-04
Tên chức năng: Xử lý và Quản lý Đơn hàng
Vai trò người dùng: Đối tác Nhà hàng
Mô tả:
Hệ thống gửi thông báo theo thời gian thực (real-time notification) đến nhà hàng mỗi khi có đơn hàng mới.
Cho phép nhà hàng xem chi tiết đơn hàng (danh sách món, ghi chú của khách) và quyết định "Chấp nhận" hoặc "Từ chối" đơn hàng.
Cung cấp quy trình cập nhật trạng thái đơn hàng để thông báo cho khách hàng: Đã chấp nhận → Đang chuẩn bị → Sẵn sàng giao.
Cho phép truy cập và lọc lịch sử các đơn hàng đã hoàn thành hoặc đã hủy.
Nguồn gốc: Cốt lõi nghiệp vụ.
Luận giải: Đây là trung tâm vận hành của nhà hàng trên ứng dụng, kết nối trực tiếp yêu cầu từ khách hàng đến quy trình chế biến tại bếp. Việc xử lý đơn hàng một cách hiệu quả và cập nhật trạng thái chính xác là yếu tố quyết định đến sự hài lòng của khách hàng (liên kết trực tiếp đến chức năng CUS-06).
ID chức năng: RES-04
Tên chức năng: Tạo và Quản lý Khuyến mãi (Coupon)
Vai trò người dùng: Đối tác Nhà hàng
Mô tả:
	Cung cấp công cụ để nhà hàng tự tạo và quản lý các chiến dịch khuyến mãi của riêng mình.
	Hỗ trợ nhiều loại hình coupon: giảm giá theo phần trăm, giảm giá theo số tiền cố định.
	Cho phép thiết lập các điều kiện áp dụng: giá trị đơn hàng tối thiểu, giới hạn sử dụng, thời gian hiệu lực.
	Cung cấp thống kê cơ bản về hiệu quả của các chiến dịch coupon đã tạo.
Nguồn gốc: Tiêu chuẩn ngành & Công cụ cạnh tranh.
Luận giải: Như đã phân tích tại Chương 2, thị trường giao đồ ăn có tính cạnh tranh rất cao về giá và khuyến mãi. Việc trang bị cho nhà hàng công cụ để họ tự tạo ra các chương trình marketing là một yêu cầu tất yếu. Chức năng này giúp tăng giá trị cho đối tác nhà hàng, cho phép họ chủ động thu hút khách hàng mà không hoàn toàn phụ thuộc vào các chương trình của nền tảng.
ID chức năng: RES-05
Tên chức năng: Xem Đánh giá và Phản hồi từ Khách hàng
Vai trò người dùng: Đối tác Nhà hàng
Mô tả:
	Cho phép nhà hàng xem tất cả các đánh giá (xếp hạng sao và bình luận chi tiết) mà khách hàng đã để lại cho các đơn hàng đã hoàn thành.
	Hiển thị đánh giá gắn liền với đơn hàng cụ thể để nhà hàng dễ dàng đối soát.
	Ghi chú: Trong phạm vi MVP, chức năng này tập trung vào việc xem phản hồi, chưa bao gồm tính năng cho phép nhà hàng trả lời trực tiếp các bình luận.
Nguồn gốc: Cốt lõi nghiệp vụ & Tiêu chuẩn ngành.
Luận giải: Phản hồi của khách hàng là nguồn thông tin vô giá giúp nhà hàng cải thiện chất lượng sản phẩm và dịch vụ. Việc cung cấp quyền truy cập vào các đánh giá này là một phần quan trọng để xây dựng mối quan hệ đối tác minh bạch và hiệu quả, đồng thời hoàn thiện vòng lặp tương tác được khởi tạo từ chức năng CUS-08 của khách hàng.
3.4.	Yêu cầu chức năng dành cho Quản trị viên (Admin)
Vai trò Quản trị viên là trung tâm điều hành của toàn bộ nền tảng YummyZoom. Admin có quyền truy cập và quản lý tối cao đối với dữ liệu và các quy trình vận hành, đảm bảo hệ thống hoạt động trơn tru, an toàn và hiệu quả. Các yêu cầu chức năng dưới đây được xác định để cung cấp cho Admin bộ công cụ cần thiết để thực hiện vai trò này.
ID chức năng: ADM-01
Tên chức năng: Bảng điều khiển Tổng quan (Admin Dashboard)
Vai trò người dùng: Quản trị viên
Mô tả:
	Cung cấp một trang tổng quan hiển thị các chỉ số quan trọng của hệ thống theo thời gian thực.
	Các chỉ số bao gồm: tổng doanh thu (mô phỏng), tổng số đơn hàng, số lượng người dùng đang hoạt động, số lượng đối tác nhà hàng đã được duyệt.
Nguồn gốc: Cốt lõi vận hành.
Luận giải: Đây là "trung tâm chỉ huy" cho phép quản trị viên nắm bắt nhanh chóng "sức khỏe" của toàn bộ nền tảng. Chức năng này cung cấp cái nhìn tổng thể về hoạt động kinh doanh, là cơ sở để đưa ra các quyết định vận hành và chiến lược.
ID chức năng: ADM-02
Tên chức năng: Quản lý Đối tác Nhà hàng
Vai trò người dùng: Quản trị viên
Mô tả:
	Hiển thị danh sách các nhà hàng đã nộp đơn đăng ký và đang ở trạng thái "Chờ duyệt".
	Cho phép Admin xem chi tiết hồ sơ đăng ký và ra quyết định Phê duyệt (Approve) hoặc Từ chối (Reject).
	Hiển thị danh sách toàn bộ các nhà hàng đối tác (đã duyệt, đang chờ, đã vô hiệu hóa).
	Cung cấp quyền vô hiệu hóa (deactivate) hoặc kích hoạt lại (reactivate) tài khoản của một nhà hàng trong trường hợp có vi phạm hoặc các vấn đề về chất lượng.
Nguồn gốc: Quy trình vận hành & Kiểm soát chất lượng.
Luận giải: Chức năng này liên kết trực tiếp và hoàn thiện luồng nghiệp vụ được bắt đầu từ chức năng RES-06 (Đăng ký Đối tác). Đây là công cụ "gác cổng" quan trọng nhất, giúp Admin đảm bảo chất lượng và tính xác thực của các nhà hàng xuất hiện trên nền tảng, từ đó xây dựng lòng tin và bảo vệ người dùng cuối.
ID chức năng: ADM-03
Tên chức năng: Quản lý Khách hàng
Vai trò người dùng: Quản trị viên
Mô tả:
	Cho phép Admin xem danh sách tất cả các tài khoản khách hàng đã đăng ký trên hệ thống.
	Cung cấp khả năng tìm kiếm và xem thông tin cơ bản của một khách hàng cụ thể.
	Cho phép Admin vô hiệu hóa tài khoản của khách hàng trong các trường hợp cần thiết (ví dụ: phát hiện hành vi gian lận, lạm dụng hệ thống).
Nguồn gốc: Cốt lõi quản trị.
Luận giải: Đây là một chức năng quản trị tiêu chuẩn, cần thiết để duy trì an ninh và trật tự cho nền tảng. Nó cung cấp cho Admin công cụ để xử lý các vấn đề phát sinh từ phía người dùng, đảm bảo một môi trường kinh doanh lành mạnh.
ID chức năng: ADM-04
Tên chức năng: Giám sát Nội dung (Đánh giá và Khuyến mãi)
Vai trò người dùng: Quản trị viên
Mô tả:
	Cho phép Admin xem tất cả các đánh giá và bình luận mà khách hàng đã gửi (CUS-08).
	Cung cấp công cụ để ẩn hoặc xóa các đánh giá có nội dung không phù hợp (ví dụ: spam, ngôn từ xúc phạm, không liên quan).
	Cho phép Admin xem danh sách tất cả các chương trình khuyến mãi đang hoạt động do các nhà hàng tạo ra (RES-04) để đảm bảo không có sự lạm dụng.
Nguồn gốc: Kiểm soát chất lượng & Vận hành.
Luận giải: Nội dung do người dùng tạo ra (User-Generated Content) là một phần quan trọng nhưng cũng tiềm ẩn rủi ro. Chức năng này trao cho Admin quyền kiểm duyệt cần thiết để duy trì một môi trường tương tác văn minh, đáng tin cậy, bảo vệ cả người dùng và các đối tác nhà hàng chân chính.

4.	Chương 4: Yêu cầu phi chức năng
Nếu các yêu cầu chức năng (Chương 3) định nghĩa hệ thống sẽ làm gì, thì các yêu cầu phi chức năng lại định nghĩa hệ thống sẽ làm điều đó như thế nào. Đây là những tiêu chí quan trọng quyết định đến chất lượng, trải nghiệm người dùng và sự thành công bền vững của ứng dụng YummyZoom. Chương này sẽ xác định các yêu cầu phi chức năng cốt lõi cho dự án.
4.1.	Yêu cầu về hiệu năng (Performance)
Hiệu năng là yếu tố tác động trực tiếp đến sự hài lòng của người dùng, đặc biệt với một ứng dụng đặt đồ ăn nơi tốc độ là chìa khóa. Một hệ thống chậm chạp sẽ nhanh chóng khiến người dùng mất kiên nhẫn và từ bỏ.
	Thời gian phản hồi của hệ thống (Response Time):
Hầu hết các thao tác của người dùng như tải danh sách nhà hàng, xem thực đơn, thêm món vào giỏ hàng phải có thời gian phản hồi dưới 2 giây trong điều kiện mạng thông thường. Đảm bảo trải nghiệm người dùng mượt mà, không có độ trễ gây khó chịu.
	Cập nhật thời gian thực (Real-time Updates):
Đối với tính năng TeamCart, các hành động của thành viên (thêm/xóa món) phải được cập nhật và hiển thị trên màn hình của các thành viên khác trong vòng dưới 1 giây. Đây là yêu cầu cốt lõi để đảm bảo tính "live" và tương tác hiệu quả của tính năng TeamCart, vốn là lợi thế cạnh tranh của dự án.
	Tối ưu tài nguyên phía Client (Client-side Optimization):
Ứng dụng trên thiết bị di động phải được tối ưu để không gây hao pin quá mức hoặc chiếm dụng bộ nhớ bất thường trong quá trình sử dụng. Một ứng dụng "nặng" sẽ là rào cản lớn khiến người dùng ngần ngại cài đặt và sử dụng thường xuyên.
4.2.	Yêu cầu về tính dễ sử dụng (Usability - UI/UX)
Giao diện người dùng (UI) và trải nghiệm người dùng (UX) là bộ mặt của sản phẩm. Một giao diện thân thiện, trực quan sẽ giúp người dùng mới dễ dàng tiếp cận và người dùng cũ gắn bó lâu dài.
	Tính nhất quán (Consistency):
Toàn bộ ứng dụng phải tuân thủ một bộ quy tắc thiết kế nhất quán về màu sắc, font chữ, biểu tượng (icons) và cách bố trí các thành phần trên các màn hình khác nhau. Giúp người dùng dễ dàng làm quen và định vị các chức năng, tạo cảm giác chuyên nghiệp và tin cậy.
	Tính trực quan và dễ học (Intuitiveness):
Người dùng lần đầu tiên có thể hoàn thành một luồng đặt hàng cơ bản mà không cần bất kỳ sự hướng dẫn chi tiết nào. Các biểu tượng và nhãn chức năng phải rõ ràng, dễ hiểu. Giảm thiểu rào cản tiếp cận, giúp ứng dụng có thể nhanh chóng được chấp nhận bởi một lượng lớn người dùng.
	Cung cấp phản hồi cho người dùng (User Feedback):
Hệ thống phải cung cấp phản hồi ngay lập tức cho các hành động của người dùng, ví dụ: hiển thị thông báo "Thêm vào giỏ hàng thành công", hiệu ứng khi nhấn nút, hoặc thông báo lỗi rõ ràng khi có sự cố xảy ra. Giúp người dùng biết chắc chắn hành động của họ đã được hệ thống ghi nhận, tránh gây hoang mang hoặc thực hiện lại thao tác một cách không cần thiết.
4.3.	Yêu cầu về độ tin cậy và bảo mật (Reliability & Security)
Dù là một dự án trong phạm vi đồ án, việc thiết kế với tầm nhìn về khả năng mở rộng sẽ thể hiện tư duy phát triển phần mềm chuyên nghiệp và giúp dự án có thể phát triển trong tương lai.
	Kiến trúc Module hóa (Modular Architecture):
Hệ thống backend phải được thiết kế theo kiến trúc module hoặc microservices, nơi các thành phần chức năng (quản lý người dùng, quản lý đơn hàng, quản lý nhà hàng) được tách biệt tương đối với nhau. Giúp việc phát triển, bảo trì và nâng cấp từng phần của hệ thống trở nên dễ dàng hơn trong tương lai mà không làm ảnh hưởng đến các phần còn lại.
	Khả năng mở rộng cơ sở dữ liệu (Database Scalability):
Thiết kế lược đồ cơ sở dữ liệu phải được tối ưu, sử dụng các chỉ mục (indexing) hợp lý để đảm bảo hiệu suất truy vấn không bị suy giảm nghiêm trọng khi lượng dữ liệu người dùng và đơn hàng tăng lên. Đảm bảo hệ thống vẫn duy trì được hiệu năng tốt khi quy mô người dùng và dữ liệu phát triển.
5.	Chương 5: Kết luận
Sau quá trình khảo sát, phân tích thị trường và xác định chi tiết các yêu cầu, tài liệu này đã xây dựng được một bức tranh toàn diện và một nền tảng vững chắc cho việc phát triển ứng dụng giao đồ ăn YummyZoom. Chương cuối cùng này sẽ tóm tắt lại những kết quả chính, chốt lại phạm vi chức năng cuối cùng của dự án và đề ra các hướng phát triển tiềm năng trong tương lai.
5.1.	Tóm tắt kết quả khảo sát
Quá trình khảo sát đã mang lại những kết quả sâu sắc và mang tính định hướng cao. Thứ nhất, phân tích thị trường đã khẳng định rằng lĩnh vực giao đồ ăn tại Việt Nam là một môi trường cực kỳ cạnh tranh, bị thống trị bởi các "siêu ứng dụng" như GrabFood và ShopeeFood với hệ sinh thái và nguồn lực khổng lồ. Thứ hai, phân tích các ứng dụng thực tế đã chỉ ra rằng việc cạnh tranh trực diện bằng mô hình tương tự (dựa vào khuyến mãi lớn hoặc hệ sinh thái đa dịch vụ) là một chiến lược bất khả thi và không bền vững đối với một dự án có quy mô như YummyZoom.
Tuy nhiên, quá trình phân tích cũng đã khám phá ra một cơ hội quan trọng tại thị trường ngách: quy trình đặt hàng nhóm hiện tại trên các nền tảng lớn vẫn còn tồn tại một "nỗi đau" rõ rệt liên quan đến khâu thanh toán tập trung, gây bất tiện cho người dùng. Đây chính là điểm tựa chiến lược, là cơ sở để YummyZoom xác định hướng đi khác biệt và tạo ra giá trị cạnh tranh độc đáo.
5.2.	Chốt lại phạm vi chức năng cuối cùng của dự án YummyZoom
Dựa trên toàn bộ kết quả khảo sát và các nguyên tắc đã đề ra, phạm vi chức năng cuối cùng được lựa chọn để triển khai trong khuôn khổ đồ án tốt nghiệp YummyZoom được xác định như sau:
  1.  Xây dựng nền tảng MVP (Sản phẩm khả dụng tối thiểu) hoàn chỉnh: Dự án sẽ phát triển đầy đủ các module chức năng cốt lõi cho ba vai trò chính:
	Khách hàng: Có thể thực hiện một luồng đặt hàng cá nhân hoàn chỉnh, từ tìm kiếm, chọn món, thanh toán (mô phỏng) đến theo dõi trạng thái đơn hàng và để lại đánh giá.
	Đối tác Nhà hàng: Được cung cấp bộ công cụ cần thiết để đăng ký, quản lý hồ sơ, thực đơn, khuyến mãi và xử lý đơn hàng.
	Quản trị viên: Có bảng điều khiển để giám sát và quản lý các hoạt động cơ bản của nền tảng.
  2.  Tập trung phát triển tính năng chiến lược - TeamCart: Dự án sẽ dành nguồn lực ưu tiên để hoàn thiện tính năng đặt hàng nhóm TeamCart, đặc biệt là cơ chế cho phép mỗi thành viên tự thanh toán phần của mình. Đây được xác định là tính năng tạo ra sự khác biệt lớn nhất và là điểm nhấn quan trọng nhất của dự án.
  3.  Xác nhận các giới hạn phạm vi: Dự án sẽ mô phỏng quá trình giao hàng thông qua việc cập nhật trạng thái, và không xây dựng module dành cho tài xế cũng như các tính năng phức tạp như theo dõi GPS thời gian thực hay chương trình khách hàng thân thiết.
5.3.	Hướng phát triển trong tương lai
Đồ án tốt nghiệp Xây dựng hệ thống giao đồ ăn YummyZoom được xem là bước khởi đầu, tạo ra một nền tảng vững chắc. Nếu có cơ hội tiếp tục phát triển dự án trong tương lai, các hướng đi tiềm năng sau đây sẽ được xem xét ưu tiên:
	Xây dựng hệ thống dành cho Tài xế và Tích hợp theo dõi thời gian thực: Đây là bước nâng cấp quan trọng nhất để chuyển từ mô hình mô phỏng sang một nền tảng vận hành thực tế, mang lại trải nghiệm theo dõi đơn hàng hoàn chỉnh cho người dùng.
	Phát triển Chương trình Khách hàng thân thiết (Loyalty Program): Xây dựng hệ thống tích điểm, đổi thưởng và các cấp độ thành viên để tăng cường khả năng giữ chân người dùng cũ, một chiến lược đã được chứng minh là rất hiệu quả qua phân tích GrabRewards.
	Tích hợp các cổng thanh toán thực tế: Kết nối với các ví điện tử và cổng thanh toán phổ biến tại Việt Nam (như MoMo, ZaloPay, VNPay) để cung cấp trải nghiệm thanh toán liền mạch và an toàn.
	Ứng dụng AI để cá nhân hóa trải nghiệm: Phát triển các thuật toán gợi ý món ăn, nhà hàng dựa trên lịch sử và sở thích của người dùng để nâng cao khả năng khám phá và tăng tỷ lệ chuyển đổi đơn hàng.


