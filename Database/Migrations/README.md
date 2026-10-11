# Kết nối Somee và cập nhật schema

Ứng dụng đọc ConnectionStrings:DefaultConnection qua cấu hình ASP.NET Core. Không lưu mật khẩu trong file được commit.

## Chạy local (Development)

Project dùng UserSecretsId SmartBusTickets-Somee-Local. Trong Visual Studio chọn Manage User Secrets và thêm:

```json
{
  "ConnectionStrings:DefaultConnection": "Server=<SQL_SERVER>;Database=<DATABASE>;User Id=<SQL_LOGIN>;Password=<SQL_PASSWORD>;Encrypt=True;TrustServerCertificate=True;"
}
```

Lấy chuỗi kết nối từ trang quản trị Somee; thay các placeholder bằng thông tin thật. User Secrets lưu ngoài repository, dành cho phát triển local. Mỗi máy phải thiết lập riêng. appsettings.json để trống chuỗi kết nối; ứng dụng không tự chuyển sang DB mẫu.

## Triển khai

Thiết lập biến môi trường ConnectionStrings__DefaultConnection bằng chuỗi kết nối thật trong cấu hình máy chủ. Không đưa mật khẩu vào commit hoặc log.

## Cập nhật schema

Mở add-driver-columns.sql và chạy thủ công trên đúng database qua SSMS hoặc công cụ Run scripts của Somee. Script thêm DateOfBirth (date NULL) và Gender (nvarchar(10) NULL) vào dbo.Driver nếu chưa có. Script có transaction, không xóa bảng hay cập nhật dữ liệu hiện có, không được tự chạy lúc khởi động app.

Somee của dự án đã được bổ sung hai cột ngày 11/10/2026; không cần chạy lại. Database khác cần kiểm tra trước khi áp dụng.