# Chuẩn bị chạy thử web, Android và iOS

Dự án chưa được kiểm thử tích hợp với PostgreSQL/PostGIS hoặc trên thiết bị thật. Các bước dưới đây tạo **bản chạy thử**, chưa phải quy trình phát hành công khai.

## Triển khai bản thử trên Render

Kho đã có [render.yaml](render.yaml) để tạo một API, một trang web tĩnh và một PostgreSQL 17. Trong Render Dashboard, kết nối kho Git của dự án rồi tạo Blueprint từ file này. Render cấp URL HTTPS cho API và web; Blueprint chuyển các URL đó qua biến môi trường để trình duyệt gọi đúng API và API chấp nhận origin của web. Bản web nạp `runtime-config.js` khi mở trang. Migration của API chạy lúc phiên bản API khởi động; nếu migration lỗi, API không bắt đầu phục vụ.

Blueprint dùng các gói **Free chỉ để thử**. Theo [tài liệu Render](https://render.com/docs/free), PostgreSQL Free hết hạn sau 30 ngày, không có sao lưu và không dùng cho dữ liệu thật. Để vận hành lâu dài, chọn gói cơ sở dữ liệu có sao lưu, kiểm thử migration trên bản sao dữ liệu và chuyển migration sang bước trước triển khai của gói API trả phí. Render [hỗ trợ PostGIS](https://render.com/docs/postgresql-extensions); migration đầu tiên sẽ bật extension này. Kiểm tra trạng thái API ở `/health` và thử đăng ký/đăng bài trên web sau khi Blueprint báo triển khai thành công.

Khi chuẩn bị Android/iOS, dùng URL HTTPS của API mà Render cấp trong bước cấu hình API ở phần web dưới đây. Blueprint chỉ triển khai web và API, không tạo gói cài đặt Android/iOS.

## 1. API và cơ sở dữ liệu

1. Sao chép `.env.example` thành `.env`. Đặt mật khẩu cơ sở dữ liệu riêng, `JWT_SECRET` ngẫu nhiên dài ít nhất 32 byte, và `FRONTEND_URL` là origin HTTPS của web (không có đường dẫn). Không đưa `.env` vào Git hoặc gửi khóa qua chat.
2. Chạy `docker compose up -d --build db`, sau đó `docker compose build api`.
3. Áp dụng các migration bằng `docker compose run --rm api --migrate`. Lệnh này chạy một lần rồi thoát; sao lưu cơ sở dữ liệu trước khi nâng cấp bản đang có dữ liệu.
4. Chạy `docker compose up -d api`, kiểm tra `GET /health` qua địa chỉ API. Chỉ mở API ra Internet sau khi đặt reverse proxy HTTPS và cấu hình bảo mật phù hợp.

`FRONTEND_URL` trong `.env` được chuyển vào API qua Compose. Nếu web có nhiều origin, ngăn cách bằng dấu phẩy. Android/iOS dùng origin nội bộ `https://localhost` hoặc `capacitor://localhost`, đã được API cho phép. `DATABASE_URL` được Compose tạo từ mật khẩu trong `.env`.

## 2. Web

Trong `frontend/journey-sharing-ui`, cài thư viện bằng `bun install`. Dùng `node scripts/api-config.mjs set https://api.example` với **origin API HTTPS của bạn** thay cho địa chỉ ví dụ; script lưu origin công khai trong `public/runtime-config.js`. Không đặt khóa bí mật vào file này. Chạy `bun run build`, rồi phục vụ thư mục `dist/journey-sharing-ui/browser` bằng máy chủ web HTTPS. Kiểm tra đăng ký, đăng nhập, bản đồ và gửi/nhận chat từ trình duyệt điện thoại.

Địa chỉ API phải truy cập được từ thiết bị. `localhost` trong điện thoại trỏ về chính điện thoại, không trỏ về máy chủ của bạn. Chứng chỉ HTTPS cần được thiết bị tin cậy.

## 3. Android

1. Cài Android Studio và Android SDK theo [hướng dẫn chính thức](https://capacitorjs.com/docs/getting-started/environment-setup). Đã có dự án `frontend/journey-sharing-ui/android`; không cần tạo nền tảng mới.
2. Cấu hình API như bước 2, rồi chạy `bun run mobile:sync` trong `frontend/journey-sharing-ui`. Bước đồng bộ sẽ từ chối URL trống hoặc không phải HTTPS.
3. Mở thư mục `android` trong Android Studio, chọn điện thoại hoặc máy ảo, bấm **Run**. Cho phép quyền vị trí khi ứng dụng yêu cầu; thử đăng nhập, đăng bài, bản đồ và chat.
4. Trước khi phát hành, đổi `appId` trong `capacitor.config.ts` và `applicationId`/`namespace` trong dự án Android sang định danh do bạn sở hữu; chuẩn bị tên, biểu tượng, chính sách riêng tư và khóa ký phát hành trong tài khoản của bạn. Khóa ký riêng phải được giữ kín, không đưa vào repo.

Xem thêm [hướng dẫn chạy Android](https://capacitorjs.com/docs/android).

## 4. iOS

1. Cần máy Mac với Xcode tương thích và tài khoản Apple Developer để ký/phát hành. Xem [môi trường cần có](https://capacitorjs.com/docs/getting-started/environment-setup).
2. Cấu hình API và chạy `bun run mobile:sync` như bước 2. Mở `frontend/journey-sharing-ui/ios/App/App.xcworkspace` trong Xcode.
3. Chọn Team ký ứng dụng, đặt Bundle Identifier thuộc quyền của bạn, chọn iPhone hoặc simulator, bấm **Run**. Cấp quyền vị trí khi được hỏi và kiểm tra các luồng chính.
4. Trước khi phát hành, hoàn thiện tên/biểu tượng, chính sách riêng tư, chứng chỉ và hồ sơ ký trong tài khoản Apple của bạn.

Xem thêm [hướng dẫn chạy iOS](https://capacitorjs.com/docs/ios). Chưa có bản cài đặt được ký hoặc kiểm thử trên thiết bị trong repo.

## 5. Khóa triển khai và việc còn thiếu

Repo không chứa deploy key hay khóa ký ứng dụng. Deploy key cho kho mã cần được tạo trong tài khoản lưu trữ/CI của bạn, gắn đúng kho và chỉ cấp quyền cần thiết; khóa riêng không gửi qua chat hoặc commit. Khóa ký Android và chứng chỉ iOS là tài sản phát hành riêng, không dùng chung với deploy key. Chưa có máy chủ hoặc tài khoản phát hành được chỉ định nên chưa thể cung cấp một deploy key có hiệu lực.

Trước khi dùng thật vẫn cần: kiểm thử truy vấn với PostgreSQL/PostGIS, ghép theo tuyến giao thông thay vì chỉ bán kính địa lý, xác thực liên hệ, kiểm thử Android/iOS trên thiết bị, kiểm thử tải và phương án sao lưu/khôi phục.
