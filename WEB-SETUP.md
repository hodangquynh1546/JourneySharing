# Dùng Journey Sharing trên điện thoại

Hiện đã có **website responsive** dùng bằng trình duyệt điện thoại và mã nguồn dự án Android/iOS dùng chung giao diện. Chưa có gói cài đặt để bàn giao.

## Chạy web

Trong `frontend/journey-sharing-ui`, cài thư viện bằng `bun install` và khởi động môi trường phát triển bằng `bun run start`. Mở địa chỉ web của frontend trên điện thoại; backend và frontend cần cùng truy cập được qua mạng. Khi triển khai, đặt `window.JOURNEY_API_URL` trong `public/runtime-config.js` thành URL HTTPS công khai của backend và cấu hình `FRONTEND_URL` trên backend cho đúng origin của website. Nếu có nhiều origin, ngăn cách bằng dấu phẩy. Môi trường phát triển mặc định cho phép `localhost:4200` và `127.0.0.1:4200`.

## Kiểm tra trên điện thoại

- Mở trang bằng Chrome trên Android hoặc Safari trên iPhone.
- Kiểm tra thanh điều hướng dưới màn hình, đăng nhập, tìm bài, đăng bài, chọn hai điểm trên bản đồ, gửi lời mời và hủy bài.
- Nút lấy vị trí hiện tại chỉ hoạt động khi người dùng đồng ý và website chạy qua HTTPS (hoặc localhost khi phát triển).
- Thử ở chiều rộng 320, 375, 390, 430 và 768 px; kiểm tra không có thanh cuộn ngang, các nút bấm đủ rộng, form không bị che bởi thanh điều hướng.

Bản đồ hiện dùng tile OpenStreetMap có ghi nguồn. Trước khi phục vụ lượng truy cập lớn cần chọn dịch vụ bản đồ có điều khoản và năng lực phù hợp. Chưa có định tuyến đường giao thông hoặc ghép tuyến thực tế.

## Dự án Android/iOS

Hai dự án nằm trong `frontend/journey-sharing-ui/android` và `frontend/journey-sharing-ui/ios`. Chúng dùng cùng mã Angular với website. Trước khi thử trên điện thoại, đặt `window.JOURNEY_API_URL` trong `public/runtime-config.js` thành địa chỉ HTTPS của API mà điện thoại truy cập được, rồi chạy `bun run mobile:sync` trong `frontend/journey-sharing-ui`. API đã cho phép origin mặc định của ứng dụng (`https://localhost` và `capacitor://localhost`); `FRONTEND_URL` dành cho origin của website.

Mở dự án Android bằng Android Studio, dự án iOS bằng Xcode trên máy Mac. Cần SDK và thiết bị hoặc máy ảo tương ứng để build, cấp quyền vị trí và thử toàn bộ luồng. Mã nguồn ở đây mới được tạo và đồng bộ; chưa kiểm thử bản cài đặt trên thiết bị, chưa ký ứng dụng hoặc phát hành.
