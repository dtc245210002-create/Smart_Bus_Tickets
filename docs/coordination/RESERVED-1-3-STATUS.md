# Kiểm kê độc lập mục 1–3 — 21/10/2026

## Hiện hành — S1/S2/S3 backend, 73/73 kiểm tra đạt

Gói SMARTBUS-SYNC-01 và AGREEMENT-003 đã được cả hai bên ACK/ACTIVE qua file.
Antigravity nhận trọn S1–S4 trên đúng5 file FE; Codex làm BE/SQL/quyền và tích
hợp tương ứng, tự đọc lại file ở mốc sửa/test/commit/bàn giao. Không có cơ chế
đánh thức agent đã dừng và không dùng MCP để trao đổi.

BE S1 commit048f8e5; S2 e200229 sửa fare khi route không lưu distance, cùng
nguồn tính với Search. S2 bổ sung expiry/reallocation/retry TTL/max6/process
restart/không seed lịch; 66/66 HTTP/SQL đạt trước S3.

S3 thêm InspectAsync truy vấn vé chính xác và không tracking, không gọi
SaveChanges/check-in. API inspect gọi đường này, giữ kiểm tra staff/assignment/
payment và mã chính xác. Xem nhiều lần không đổi status; check-in tường minh
vẫn hoạt động và chặn quét lặp tuần tự. DTO thời gian legacy chưa có persisted
check-in timestamp: inspect trả giá trị mặc định unknown, không gán giờ xem
thành giờ check-in; chưa đổi wire field/schema hoặc hoàn thiện audit thời gian.

**73/73 kiểm tra đạt** sau S3, bao gồm66 trước +7 ca inspect/check-in. DB tạm
được dọn đúng marker/token, không reset fixture lâu dài. Chưa test scan đồng
thời, provider/Somee, SMTP/OTP delivery hoặc browser/mobile E2E.

FE d05579c có6 ca đỏ; đã nhận bfa54ae sửa lại và chạy Node vm/mock fetch/DOM
trên chính git blob: **11/11 kỳ vọng đạt**. Không cộng mock11 vào73 HTTP/SQL.
Đọc diff thấy SSR QR guard và polling hết hạn đã sửa. Chưa coi S2/S3 FE hoặc
checkout tiền thật hoàn tất, chưa push/deploy hoặc sửa DB chung.

Evidence hiện tại: `../validation/results/backend.json`,
`../validation/results/bfa54ae-review.json`; cách chạy tại
`../validation/README.md`. Các đoạn trạng thái cũ dưới đây là lịch sử.

## Đợt đầu: triển khai backend và kiểm thử thực tế (lịch sử)

Các mục kiểm kê ban đầu bên dưới là lịch sử. Worktree hiện đã có bản sửa
backend trên nền ba98ee9, không thay đổi các nhánh khác hoặc Somee.

- Xác thực: bỏ password/plaintext/demo fallback và Google giả; driver không
  được cấp Admin; role lấy từ DB, cookie cũ được đối chiếu user/roles/driver
  đang hoạt động mỗi request. Signup Pending, OTP không trả code cho client,
  không dùng mã dự phòng để kích hoạt một email bất kỳ hoặc tài khoản khóa.
- Quyền: chặn admin/driver/API sai role, truy vấn vé theo owner; actor không
  lấy từ userId/sessionId client; driver chỉ soát chuyến được giao. Mã scan
  gần đúng gây nhiều kết quả bị chặn trước khi gọi engine cũ.
- Đọc vé: Detail/Payment/PaymentResult/status dùng dữ liệu thật. GET không
  sinh vé/chuyến/Payment hoặc hồi sinh vé hủy. Detail từ chối boarding QR
  khi chưa có payment đủ tiền và trạng thái vé đã cấp. Không tin query/cookie.
- Inventory: SQL transaction và sp_getapplock theo TripId, validate ghế theo
  layout; ISSUED/USED chiếm ghế; retry/nhả giữ đúng owner và tổng ticket.
  Không tạo bảng/cột/index mới hoặc áp thay đổi vào DB chung.
- Fail closed: chưa có provider verifier nên ConfirmPayment GET405/POST503,
  verify-payment503; không báo thu tiền/issue giả. ConfirmReschedule409 để
  không dùng luồng cũ tự CONFIRMED với giá/ghế client. Không coi đây là đã
  hoàn thiện luồng thanh toán hoặc đổi vé; các tính năng này còn phụ thuộc.

### Bằng chứng mới

Build đạt, không có lỗi; còn hai warning nullable ở đường legacy đổi/hủy và
warning NU1900 do không truy cập được dịch vụ audit NuGet trong lần restore.
Đây không phải kết luận các dependency đã qua kiểm tra vulnerability.

**53/53 kiểm tra đạt**, dùng Kestrel thật, đăng nhập cookie/antiforgery thật,
SQL LocalDB SmartBusG0 và fixture/guard G0 tái sử dụng. Có:

- Anonymous/sai role/chéo owner; khóa user sau login; driver không có Admin;
  lỗi DB + password mẫu không cấp cookie đăng nhập.
- GET lặp không đổi row/status, thiếu đơn không có vé/QR; PaymentResult
  query success không hồi sinh CANCELLED, payment thiếu/lặp không ghi dữ liệu.
- Ghế không tồn tại/ISSUED bị từ chối; userId/sessionId giả không chiếm/nhả
  giữ của người khác; retry không thêm Ticket và tổng bằng các hold còn lại.
- Hai OS worker, barrier SQL, trả đúng 200/409, chỉ một phân bổ A06; process
  web mới đọc thấy hold từ SQL; không dựa dictionary một process.
- Chặn bypass đổi vé, chưa thanh toán không check-in, mã mơ hồ không quét
  chéo chuyến. **Chưa nghiệm thu inspect chỉ đọc hoặc check-in concurrency.**

Evidence bên ngoài repo (không đi vào publish):
`../validation/results/backend.json`; nguồn console harness ở
`../validation/BackendValidation.csproj` và `../validation/Program.cs`.
Harness compile-link TestDatabase/TestDbGuard từ clone G0 đã có. Không chạy
lại G0Infrastructure14/14 trong lượt này và không gộp số đó vào53.
DB tạm được dọn theo đúng marker/token; fixture lâu dài G0 không bị reset.

### Review và việc còn lại

Antigravity bàn giao d05579c đúng5 file FE. Chưa tích hợp: Node vm/mock
fetch/DOM trên git blob đạt5/11 kỳ vọng, tái hiện6 ca đỏ về coi lỗi không phải
409 thành ghế đã bán và submit lặp không preventDefault. Source còn QR img
SSR không guard payload/trạng thái và polling selection hết hạn. Các ca
mock không thay thế kiểm thử browser desktop/mobile.

Đã ghi ANTIGRAVITY-FE-02/A theo001 ACTIVE; contract nối booking thật002
PROPOSED chờ ACK. Không tự sửa5 file FE, không lấy working tree dở.

Chưa xác minh: Somee/sp_getapplock permissions trên host, provider thật,
email delivery/OTP E2E, collation/timezone khác máy, đầy đủ đổi/hủy/hoàn,
engine inspect/check-in. BookingTime giữ convention server-local hiện hữu;
chưa migration UTC/quote version hay áp schema G0 đích vào production.

Đã đối chiếu nhánh68c4db9 (origin/phan_quyen_admin_taixe_khachhang) có một
phần quyền/Admin khác develop; vẫn còn password mẫu/actor client ở đó.
Không sửa/merge hoặc ghi đè nhánh nhóm. Cần xem phần giao nhau với commit
nhóm bàn giao trước khi tích hợp cuối, không tuyên bố thay thế toàn bộ68c4db9.

---

## Kiểm kê ban đầu trước sửa (lưu lịch sử)

Nền ba98ee9a6912d3de1ee25c91c91739507d69268f. Worktree
codex/coordination-auth-20261021. Đây là kiểm tra source mới, không phải
kết quả test HTTP mới hoặc xác nhận chưa có fix trên nhánh nhóm khác.

## Nguồn nhiệm vụ và ranh giới

Đã truy lại danh sách gốc trong lịch sử chat: 20/10/2026 16:39:17 UTC:
1. Đóng lỗi vượt quyền; 2. Sửa thanh toán và xuất vé; 3. Sửa giữ ghế và trạng thái.
Không đổi thành ba gói authentication mới và không đồng nhất với ba đầu ra G0.
Chưa nhận commit hoàn thành của nhóm khác để tích hợp hoặc xác nhận làm trùng.

## Điểm cần nhận fix / kiểm tra hồi quy

| Nguồn | Quan sát source | Kiểm tra cần có |
|---|---|---|
| Services/PasswordHasher.cs:22 | Có chấp nhận chuỗi thô/hash mẫu | Hash hợp lệ hoạt động; mẫu không tự đăng nhập; nhận thay đổi nhóm trước khi sửa |
| Controllers/DriverController.cs:95,137 | Bypass password và cấp role quản trị khi sign-in driver | Driver không trở thành admin; sai mật khẩu bị từ chối |
| Controllers/AccountController.cs:155 | Catch lỗi DB có fallback tạo principal | DB lỗi không cấp cookie đăng nhập |
| Controllers/AdminController.cs | Chưa có Authorize tại controller | Anonymous/customer/driver không vào admin, admin thật truy cập được |
| Controllers/TicketController.cs | Nhiều query ticket theo mã, có GET ghi trạng thái/tạo vé | Hai customer độc lập; owner xác thực; GET không ghi |
| Controllers/Api/BookingsApiController.cs | Actor từ request và hold trong memory | Giả user/session không chiếm nhả vé người khác; hai process không trùng |
| Controllers/Api/TicketsApiController.cs | Cửa validate cần policy/phân công | Anonymous/customer/driver sai ca không check-in; không mở rộng engine scan trong lượt này |
| Controllers/TripController.cs và DriverController.cs | GET có seed chuyến/vé | Đếm row/status trước/sau không đổi |

## Giao diện / phụ thuộc

Đọc trực tiếp seat-selection.js: pointerEvents đã khóa trong request và
updateSeatSummary đã gọi khi hold lỗi; không coi hai cơ chế này là chưa có.
Catch lỗi mạng vẫn tự chọn ghế; nút tiếp tục đi Detail; Payment có nút đi
Detail mà không chứng minh đã trả tiền. Backend phải chặn độc lập, không
coi việc đổi link hoặc disable nút là fix mục1–3.

Contract route/DTO thực tế được ghi tại nguồn chung AGREEMENT-001, PROPOSED.
Không đưa contract đích G0 thành API thật; chưa có thỏa thuận đổi wire fields.

## Kết quả thực tế / phần chưa xác minh

- Đã kiểm tra Git: develop ba98ee9, tracked sạch; Agent.md/.coordination đã
  có trước lượt. Tạo worktree riêng, không reset/clean hay sửa source gốc.
- Chỉ đọc source/route/DTO trong lượt này; chưa sửa nghiệp vụ, chưa chạy test
  HTTP quyền mới, không ghi SQL/Somee, không gửi email/thanh toán.
- Giữ kết quả G0 đã bàn giao: 14/14 infrastructure, 2/8 baseline; không chạy
  lại trong lượt phối hợp, không dùng chúng chứng minh middleware auth.
- Không làm lại G0 hoặc lấy bản sửa chưa bàn giao của Antigravity.
- Tiếp theo trong mục1–3 đã được giao: nhận SHA nhóm đã làm nếu có; đóng
  từng lỗi trên worktree, test quyền HTTP và baseline SQL tương ứng. Khi
  cần thêm file kiểm thử/service/migration, nêu cụ thể trong đề xuất mới.
