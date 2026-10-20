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
- **73/73 kiểm tra HTTP/SQL đạt** trên cây tích hợp: Kestrel/cookie/
  antiforgery thật, LocalDB test riêng, hai OS worker trả200/409 và một
  phân bổ ghế. DB tạm đã dọn theo marker/token.
- **11/11 kỳ vọng JS đạt** bằng Node vm/mock DOM/fetch, tách khỏi73 test
  backend; chưa phải browser/mobile E2E.
- Evidence JSON ở `docs/verification/`; hướng dẫn chạy tại
  `tests/BackendValidation/README.md`.

## Phần chưa hoàn tất / giới hạn bản bàn giao

- Chưa có provider verifier. ConfirmPayment GET trả405, POST/verify-payment
  trả503; không báo đã thu tiền hay tự issue. Google OAuth chưa xác minh
  trả503. Đây là chặn hành vi sai, không phải checkout tiền thật hoàn chỉnh.
- ConfirmReschedule trả409 để không tự xác nhận vé/giá/ghế theo client khi
  chưa có workflow đổi vé an toàn. Đổi/hủy/hoàn tiền đầy đủ chưa nghiệm thu.
- Frontend S2/S3/S4 `f833927`, `daf58db` chưa tích hợp: review còn vấn đề
  quote/tổng cũ sau nhả bớt ghế và khôi phục booking sau reload. Luồng nối
  checkout theo booking thật phải chốt các ca đó trước khi nhận.
- Chưa kiểm tra Somee, SMTP/OTP delivery, browser/mobile, check-in đồng thời
  và migration dữ liệu cũ. BookingTime giữ convention server-local hiện hữu;
  inspect timestamp legacy unknown chưa phải event check-in bền vững.
- Giữ nguyên schema/dữ liệu Somee, appsettings/User Secrets và nhánh nhóm.
  Nhánh quyền `68c4db9` có thay đổi riêng chưa được tích hợp vào bản này.

Không coi bản bàn giao là đã sẵn sàng nhận khách hoặc tiền thật.
