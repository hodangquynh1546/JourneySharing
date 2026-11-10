# Journey Sharing

Nền tảng ghép hành trình dùng chung đường đi. Đây là **bản nền tảng đang phát triển**, chưa phải MVP có thể sử dụng.

## Trạng thái

- Đã tạo solution .NET 10 gồm Domain, Application, Infrastructure và API.
- Đã thiết kế các thực thể nền: người dùng, phương tiện, hành trình, nhu cầu di chuyển và kết nối; cấu hình EF Core với PostgreSQL/PostGIS.
- Đã tạo migration `InitialCreate`. Một hành trình có thể không có phương tiện; bài đăng có thời điểm hết hạn; khoản đóng góp có thể bằng 0.
- Đã có cấu hình Docker cho API và PostGIS, cùng endpoint `/health` kiểm tra kết nối cơ sở dữ liệu.
- Chưa có xác thực, các API nghiệp vụ, ghép tuyến, Angular PWA, ứng dụng điện thoại, chat, kiểm thử tích hợp hoặc triển khai. Không đưa bản này vào sử dụng thực tế.

## Chạy bản nền tảng

Sao chép `.env.example` thành `.env`, thay mật khẩu local, sau đó chạy `docker compose up -d --build`. API lắng nghe ở cổng 8080. Chạy migration bằng công cụ `dotnet-ef` trong môi trường có thể kết nối cơ sở dữ liệu. Cấu hình Docker hiện chưa tự chạy migration và chưa được kiểm tra do môi trường làm việc không có Docker.

## Quyết định nghiệp vụ đã chốt

- Một chuyến có thể ghép nhiều người nếu còn chỗ.
- Người đăng hành trình có thể không có phương tiện; người có phương tiện cùng hướng có thể đón.
- Chi phí tự thỏa thuận, có thể miễn phí; hệ thống chỉ gợi ý.
- Bài đăng công khai, tự hết hạn theo thời điểm đã cài; hết hạn không xóa lịch sử.
- Phát triển web và ứng dụng điện thoại song song.
