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

## Kiểm chứng

- Build cây tích hợp đạt, 0 lỗi; còn2 warning nullable legacy.
- **82/82 kiểm tra HTTP/SQL đạt** trên cây tích hợp: Kestrel/cookie/
  antiforgery thật, LocalDB test riêng, hai OS worker trả200/409 và một
  phân bổ ghế. Bổ sung9 ca đọc quote, chống giả actor, tổng không khớp,
  nhiều đơn mơ hồ, release/reload/TTL và không ghi khi GET. DB tạm đã dọn
  theo marker/token. Kết quả này thuộc bản local bổ sung AGREEMENT-004.
- **11/11 kỳ vọng JS đạt** bằng Node vm/mock DOM/fetch, tách khỏi73 test
  backend; chưa phải browser/mobile E2E.
- Backend004 bổ sung `heldBookingCode`, `heldTotalPrice`, `remainingSeconds`
  vào GET trip-seat-status. Chỉ trả quote cho một đơn Pending hợp lệ của
  người đăng nhập; không tạo/gia hạn giữ chỗ hoặc sửa tổng tiền khi đọc.
- Review ứng viên FE `c1d5d07`: **13/14** ca Node vm đạt; còn race khi poll
  bắt đầu trong lúc hold đang chạy rồi trả snapshot cũ sau hold success.
  Chưa tích hợp ứng viên. Chạy lại bằng
  `node tests/BackendValidation/review-antigravity.cjs c1d5d07 --workflow`.
- Evidence JSON ở `docs/verification/`; hướng dẫn chạy tại
  `tests/BackendValidation/README.md`.

## Phần chưa hoàn tất / giới hạn bản bàn giao

- Chưa có provider verifier. ConfirmPayment GET trả405, POST/verify-payment
  trả503; không báo đã thu tiền hay tự issue. Google OAuth chưa xác minh
  trả503. Đây là chặn hành vi sai, không phải checkout tiền thật hoàn chỉnh.
- ConfirmReschedule trả409 để không tự xác nhận vé/giá/ghế theo client khi
  chưa có workflow đổi vé an toàn. Đổi/hủy/hoàn tiền đầy đủ chưa nghiệm thu.
- Frontend S2/S3/S4 từ `f833927` tới `c1d5d07` chưa tích hợp: Anti đã sửa
  storage price/TTL/payment503 và phục hồi quote theo004; còn ca race nêu
  trên trước khi nhận luồng nối checkout theo booking thật.
- Chưa kiểm tra Somee, SMTP/OTP delivery, browser/mobile, check-in đồng thời
  và migration dữ liệu cũ. BookingTime giữ convention server-local hiện hữu;
  inspect timestamp legacy unknown chưa phải event check-in bền vững.
- Giữ nguyên schema/dữ liệu Somee, appsettings/User Secrets và nhánh nhóm.
  Nhánh quyền `68c4db9` có thay đổi riêng chưa được tích hợp vào bản này.

Không coi bản bàn giao là đã sẵn sàng nhận khách hoặc tiền thật.
