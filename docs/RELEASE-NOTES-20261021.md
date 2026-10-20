# SMART Bus Go — bản cải thiện ngày 21/10/2026

Nhánh bàn giao: `codex/smartbus-safety-sync-20261021`.
Nền develop đã đối chiếu: `ba98ee9`. Không merge vào develop hoặc deploy.

## Những phần chính đã cải thiện

1. **Đăng nhập và phân quyền**: bỏ mật khẩu/tài khoản mẫu và Google đăng
   nhập không xác minh; tài xế không được cấp Admin ngầm. Role/driver lấy
   từ DB, cookie cũ được đối chiếu lại. Chặn sai role, xem vé người khác,
   giả userId/sessionId và tài xế truy cập chuyến không được giao.
2. **Đặt vé, giữ ghế và giá**: SQL transaction + khóa theo TripId phối hợp
   nhiều process; kiểm tra ghế đúng layout, ghế ISSUED/USED, hết hạn và tối
   đa6 ghế. Retry không thêm giữ trùng hoặc gia hạn ngầm; tổng lấy từ ticket
   phía server. Giá khi thiếu distance dùng cùng nguồn với Search.
3. **Vé và thanh toán**: Detail/Payment/PaymentResult/status đọc dữ liệu
   thật; refresh không tạo vé/Payment/chuyến hoặc kích hoạt lại vé hủy.
   Không xuất boarding QR chỉ dựa URL/cookie hoặc tiền do client gửi.
4. **Soát vé**: tách InspectAsync chỉ đọc khỏi check-in; mã vé chính xác,
   kiểm tra staff/ca/payment. Xem lại không tiêu thụ vé; check-in là lệnh
   riêng và tiếp tục chặn quét trùng tuần tự.
5. **Frontend đã review**: tách409 khỏi lỗi400/401/403/503/nonJSON, bỏ chọn
   thành công giả khi mất mạng, giữ selection khi nhả thất bại, chặn submit
   lặp, sửa polling hết hạn/in-flight, guard QR từ Razor và tách lỗi Search
   khỏi danh sách rỗng. Giữ thiết kế hiện hữu.
6. **Kiểm thử tái lập**: đưa console harness và fixture SQL cô lập vào repo;
   test source/kết quả không đi vào web publish.

Backend commits: `048f8e5`, `e200229`, `59b841f`.
Frontend được bàn giao/review: `d05579c`, `bfa54ae`, tích hợp nguyên commit
thành `b705a36`, `a25eb9d`. Không sửa tay file thuộc Antigravity.
Backend004: `a6ee9f9`. Đã nhận bổ sung chuỗi FE `f833927` tới `f87c488`
thành `ae15b63`, `cc1bfb3`, `93bb9ff`, `cfad8e8`, `270ce05`, `f157845`.
FE06 `4ebb417`/`d60b4d4` được tích hợp thành `aa271f6`/`f9fc94d`.
Các cập nhật sau bản push02:09 hiện chỉ ở local, chờ lịch push07:00.

## Kiểm chứng

- Build cây tích hợp đạt, 0 lỗi; còn2 warning nullable legacy.
- **82/82 kiểm tra HTTP/SQL đạt** trên cây tích hợp: Kestrel/cookie/
  antiforgery thật, LocalDB test riêng, hai OS worker trả200/409 và một
  phân bổ ghế. Bổ sung9 ca đọc quote, chống giả actor, tổng không khớp,
  nhiều đơn mơ hồ, release/reload/TTL và không ghi khi GET. DB tạm đã dọn
  theo marker/token. Kết quả này thuộc bản local bổ sung AGREEMENT-004.
- **15/15 kỳ vọng workflow JS đạt** trên bản tích hợp bằng Node vm/mock
  DOM/fetch; baseline S1 trước đó11/11. Tách khỏi82 test backend;
  chưa phải browser/mobile E2E.
- Backend004 bổ sung `heldBookingCode`, `heldTotalPrice`, `remainingSeconds`
  vào GET trip-seat-status. Chỉ trả quote cho một đơn Pending hợp lệ của
  người đăng nhập; không tạo/gia hạn giữ chỗ hoặc sửa tổng tiền khi đọc.
- Anti sửa race của `c1d5d07` tại `f87c488`; các ca bao gồm poll trước và
  trong lúc hold, storage price, TTL0, release/recovery, lỗi poll và payment503.
  Chạy lại bằng `node tests/BackendValidation/review-antigravity.cjs HEAD`.
  Ca15 bật hàm sync thật trong VM và xác nhận GET tự động ngay sau release
  khôi phục quote;14 ca trước điều khiển polling để tái hiện thứ tự response.
  Không suy kết quả thành kiểm thử toàn bộ chu kỳ polling trên trình duyệt.
  JS-only FE06 đã kiểm tra cú pháp và15ca trên source tích hợp; không chạy
  lại82ca SQL vì backend không đổi từ lần xác minh trước.
- Evidence JSON ở `docs/verification/`; hướng dẫn chạy tại
  `tests/BackendValidation/README.md`.
- **7/7 ca trình duyệt thật đạt** bằng Edge headless đã cài, profile/phiên
  khách A/B riêng, thao tác form và click thật. Bao phủ giữ/nhả ghế, khách B
  thấy ghế A bị khóa, reload ở390px khôi phục cùng booking, giá khớp server
  và Payment503 giữ trang/mở retry. Ảnh và JSON tại
  `TestResults/browser-smoke/1792527283406/` trên máy kiểm thử.
  Chạy lại với fixture đang READY:
  `node tests/BackendValidation/review-antigravity.cjs HEAD --browser-smoke`.
  Fixture tạo bằng `dotnet tests/BackendValidation/bin/Debug/net9.0/BackendValidation.dll browser-fixture 20`.
  Mode browser dùng Development để nạp static assets runtime của bản Debug;
  API regression giữ Testing. Không thay môi trường/config của sản phẩm.
- **9/9 ca trên bản publish ở Production đạt**: cùng7ca trên và thêm mất
  mạng khóa checkout, khôi phục mạng lấy lại cùng đơn bằng polling. JS/CSS
  trả200, nội dung khác rỗng và MIME đúng. Kết quả tại
  `TestResults/browser-smoke/1792538175209/result.json`; fixture đã dừng và
  DB tạm đã dọn. Publish không chứa test/tooling artifacts.
  Sau publish vào `../publish-release-check`, tạo fixture bằng
  `dotnet tests/BackendValidation/bin/Debug/net9.0/BackendValidation.dll browser-fixture 10 Production`,
  rồi chạy cùng lệnh `--browser-smoke` phía trên. Đây là kiểm thử local bản
  publish, chưa deploy lên Somee.
- Đã review bằng chứng Chrome bổ sung của Anti: tài xế đăng nhập vào đúng
  Dashboard, tài xế/khách bị chặn Admin403, Admin vào được Dashboard,
  truy cập vé khác chủ404, vé chưa trả tiền khôngQR, viewport375px có ảnh.
  Không cộng các ca trùng với Edge thành số test độc lập; bản báo cáo cũ
  nhầm login thất bại là pass đã bị loại và thay bằng bằng chứng HTTP rõ ràng.
- **5/5 ca hết hạn theo thời gian thực đạt** trên bản publish Production:
  TTL ban đầu còn577giây, browser chờ579giây, sau đó server trả code/giá null,
  TTL0, giao diện bỏ chọn và khóa checkout. Không có POST giữ ghế nền trong
  thời gian chờ; khách B giữ được ghế vừa hết hạn bằng đơn mới. Không sửa
  đồng hồ hoặc tuổi booking để rút ngắn test. DB tạm đã dọn.
  Kết quả: `TestResults/browser-smoke/1792538364405/result.json`.
  Chạy trên fixture Production mới20phút bằng
  `node tests/BackendValidation/review-antigravity.cjs HEAD --expiry-watch`;
  phép thử cần gần10phút thực và không được chạy trùng/thao tác vào fixture.

## Phần chưa hoàn tất / giới hạn bản bàn giao

- Chưa có provider verifier. ConfirmPayment GET trả405, POST/verify-payment
  trả503; không báo đã thu tiền hay tự issue. Google OAuth chưa xác minh
  trả503. Đây là chặn hành vi sai, không phải checkout tiền thật hoàn chỉnh.
- ConfirmReschedule trả409 để không tự xác nhận vé/giá/ghế theo client khi
  chưa có workflow đổi vé an toàn. Đổi/hủy/hoàn tiền đầy đủ chưa nghiệm thu.
- Frontend S2/S3 đã tích hợp qua cổng logic; S4 browser đã xác minh một phần
  trên desktop1440px/mobile-width390px. FE06 đã sửa fresh snapshot sau mutation:
  chỉ gọi sau khi generation/in-flight đã ổn định để không tự loại GET mới.
  Playwright driver1.57.0 của Anti tải404; đã dùng Edge/WebSocket native để
  kiểm thử7ca trên, không cần tải driver. Các phiên fixture trước đã dọn;
  phiên dùng cho Anti và phiên Production9ca đều đã dừng/dọn DB tạm.
  Còn thiếu thiết bị di động vật lý, font/icon ngoài và offline kéo dài.
  TTL10phút thực, không gia hạn nền và khách khác nhận ghế sau expiry đã đạt.
  Hai lượt browser đầu gặp asset200rỗng trong Testing không được tính pass.
- Chưa kiểm tra Somee, SMTP/OTP delivery, thiết bị mobile thật, check-in đồng thời
  và migration dữ liệu cũ. BookingTime giữ convention server-local hiện hữu;
  inspect timestamp legacy unknown chưa phải event check-in bền vững.
- Giữ nguyên schema/dữ liệu Somee, appsettings/User Secrets và nhánh nhóm.
  Nhánh quyền `68c4db9` có thay đổi riêng chưa được tích hợp vào bản này.

Không coi bản bàn giao là đã sẵn sàng nhận khách hoặc tiền thật.
