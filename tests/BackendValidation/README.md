# Kiểm thử backend và review frontend

Bộ console regression tự chứa trong repo, tái sử dụng fixture/guard G0 đã
phát triển. Không cần clone G0 bên ngoài. Không dùng DB Somee/User Secrets,
gửi email hoặc thanh toán thật.

## Yêu cầu và chạy

Windows có SQL Server LocalDB và .NET hỗ trợ net9.0. Tạo instance test riêng
nếu chưa có, không đổi sang DB chung:

```powershell
sqllocaldb info SmartBusG0
# Chỉ tạo nếu instance chưa tồn tại:
sqllocaldb create SmartBusG0 -s
dotnet build tests/BackendValidation/BackendValidation.csproj -c Debug -p:NuGetAudit=false
# Đợi build kết thúc thành công trước khi chạy:
dotnet tests/BackendValidation/bin/Debug/net9.0/BackendValidation.dll
node tests/BackendValidation/review-antigravity.cjs HEAD
```

LocalDB phải hỗ trợ schema SQL Server/EF hiện tại. Tại máy phát triển đã
kiểm tra với LocalDB2019 và SDK hỗ trợ net9.0. Không build đồng thời với test
trên cùng DLL vì Windows khóa file đang chạy.

Harness tự tạo DB ephemeral có marker/token và prefix riêng, khởi động
Kestrel HTTP/cookie/antiforgery thật, chạy hai worker OS tranh ghế, và dọn
đúng DB sở hữu. Không reset fixture lâu dài hoặc xóa DB theo danh sách.
Kết quả/log ở TestResults/ (đã ignore và loại khỏi web publish).

## Phạm vi

73 kiểm tra backend: quyền/owner/assignment, chặn login mẫu/lỗi DB, GET
không ghi, payment chưa xác minh không cấp vé, SQL hold/expiry/retry/max6/
giá server, inspect chỉ đọc và check-in tường minh/quét lặp tuần tự.

Review JS dùng Node vm/mock fetch/DOM trên source Git, không phải browser
desktop/mobile. Không suy các kết quả thành nghiệm thu provider thật,
SMTP/OTP delivery, Somee, check-in đồng thời hoặc đầy đủ đổi/hủy/hoàn tiền.
Inspect giữ DTO thời gian legacy; giá trị mặc định là unknown, không phải
giờ check-in đã lưu bền vững. Không có migration DB production trong bộ này.
