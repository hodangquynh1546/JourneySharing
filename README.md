# Journey Sharing

Nền tảng ghép hành trình dùng chung đường đi. Đây là **bản nền tảng đang phát triển**, chưa phải MVP có thể sử dụng.

## Trạng thái

- Đã tạo solution .NET 10 gồm Domain, Application, Infrastructure và API.
- Đã thiết kế các thực thể nền: người dùng, phương tiện, hành trình, nhu cầu di chuyển và kết nối; cấu hình EF Core với PostgreSQL/PostGIS.
- Đã tạo migration `InitialCreate`. Một hành trình có thể không có phương tiện; bài đăng có thời điểm hết hạn; khoản đóng góp có thể bằng 0.
- Đã có cấu hình Docker cho API và PostGIS, cùng endpoint `/health` kiểm tra kết nối cơ sở dữ liệu.
- Đã có API đăng ký, đăng nhập và xem hồ sơ hiện tại với mật khẩu băm, JWT ngắn hạn và giới hạn số lần gọi endpoint xác thực.
- Đã có API tạo/xem phương tiện, đăng hành trình và đăng nhu cầu di chuyển. Danh sách công khai chỉ hiển thị khu vực, không trả tọa độ hoặc địa chỉ chính xác. Có lọc theo khu vực đi/đến và ngày theo giờ Việt Nam.
- Có thể lọc bài theo bán kính 1–50 km quanh điểm đi/đến bằng PostGIS. Đây là khoảng cách địa lý giữa hai điểm, chưa phải kiểm tra tuyến đường giao thông thực tế.
- Chủ bài có thể xem bài của mình, gồm bài hết hạn, và hủy bài khi chưa có kết nối được chấp nhận. Các lời mời còn chờ được đóng trong cùng giao dịch.
- Đã có API gửi/xem/chấp nhận/từ chối/hủy yêu cầu kết nối khi còn chờ; chỉ người tham gia được thao tác. Chấp nhận ghế dùng giao dịch PostgreSQL mức `Serializable`, kiểm tra số ghế đã nhận và từ chối khi hết chỗ.
- Lời mời mới được kiểm tra khoảng cách điểm đón và điểm trả với ngưỡng 50 km, tránh ghép hai bài ngược hướng hoặc ở quá xa. Đây là kiểm tra địa lý sơ bộ; chưa tính quãng đường lái xe hay điểm đón dọc tuyến.
- Đã có giao diện web responsive cho đăng nhập, tìm bài, đăng bài với bản đồ, thêm phương tiện, gửi lời mời từ hai phía, quản lý bài và yêu cầu kết nối. Trạng thái API trả dạng chuỗi để giao diện hiển thị ổn định. Web và API cho phép kết nối qua origin cấu hình.
- 12 kiểm thử đơn vị cho quyền chuyển trạng thái và sức chứa đã chạy đạt. Toàn bộ solution build 0 lỗi.
- Đã tạo dự án Android/iOS dùng chung giao diện web và tích hợp quyền lấy vị trí. Mã web đã đồng bộ sang hai dự án; chưa build và thử bản cài đặt trên thiết bị.
- Chưa có xác thực email/số điện thoại, refresh token, API sửa bài, quy trình hủy kết nối đã chấp nhận, kiểm tra tuyến đường thực tế, Angular PWA, chat, kiểm thử tích hợp PostgreSQL hoặc triển khai. Không đưa bản này vào sử dụng thực tế.

## Chạy bản nền tảng

Sao chép `.env.example` thành `.env`, thay mật khẩu local, sau đó chạy `docker compose up -d --build`. API lắng nghe ở cổng 8080. Chạy migration bằng công cụ `dotnet-ef` trong môi trường có thể kết nối cơ sở dữ liệu. Cấu hình Docker hiện chưa tự chạy migration và chưa được kiểm tra do môi trường làm việc không có Docker.

## Quyết định nghiệp vụ đã chốt

- Một chuyến có thể ghép nhiều người nếu còn chỗ.
- Người đăng hành trình có thể không có phương tiện; người có phương tiện cùng hướng có thể đón.
- Chi phí tự thỏa thuận, có thể miễn phí; hệ thống chỉ gợi ý.
- Bài đăng công khai, tự hết hạn theo thời điểm đã cài; hết hạn không xóa lịch sử.
- Phát triển web và ứng dụng điện thoại song song.
