# Journey Sharing

Nền tảng ghép hành trình dùng chung đường đi. Đây là **bản nền tảng đang phát triển**, chưa phải MVP có thể sử dụng.

## Trạng thái

- Đã tạo solution .NET 10 gồm Domain, Application, Infrastructure và API.
- Đã thiết kế các thực thể nền: người dùng, phương tiện, hành trình, nhu cầu di chuyển và kết nối; cấu hình EF Core với PostgreSQL/PostGIS.
- Đã có cấu hình Docker cho API và PostGIS, cùng endpoint `/health` kiểm tra kết nối cơ sở dữ liệu.
- Chưa có migration, xác thực, các API nghiệp vụ, ghép tuyến, Angular PWA, chat, kiểm thử tích hợp hoặc triển khai. Không đưa bản này vào sử dụng thực tế.

## Chạy bản nền tảng

Sao chép `.env.example` thành `.env`, thay mật khẩu local, sau đó chạy `docker compose up -d --build`. API lắng nghe ở cổng 8080. Cơ sở dữ liệu chưa có schema cho đến khi migration được triển khai.

Các quyết định về số người trên mỗi chuyến, phương tiện, phí, quyền hiển thị, chuyến định kỳ và nền tảng MVP cần được chủ dự án xác nhận trước khi triển khai luồng ghép chuyến.
