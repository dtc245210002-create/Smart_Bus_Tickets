# SmartBus Go - Smart Bus Ticketing System

## Demo giao diện ASP.NET

Giữ giao diện Razor/Bootstrap hiện tại: trang chủ, tìm chuyến/chọn ghế, đăng nhập hành khách và bác tài. Bổ sung menu **Vé của tôi**, hồ sơ khách hàng, đổi/hủy vé và luồng thanh toán mẫu.

Chạy trong thư mục `SmartBus_Web` bằng PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --urls http://localhost:5188
```

- Hành khách: `demo@smartbus.vn` hoặc `0987654321`, mật khẩu `Demo123!`.
- Bác tài: `TX-001` hoặc `0988777999`, mật khẩu `Demo123!`.
- Đăng ký mới: OTP demo `123456` được hiển thị trên màn hình; không gửi email thật.
- Google: đăng nhập mô phỏng, không gọi OAuth thật.
- Tìm Hà Nội → Hải Phòng vào ngày mai: ba chuyến lúc 08:00, 14:00, 20:00. Có thêm Hà Nội → Sa Pa, Sài Gòn → Đà Lạt, Đà Nẵng → Huế, Thái Nguyên → Hà Nội.
- Có sẵn hai đơn đã thanh toán cho hành khách mẫu. Chọn ghế khác để đặt mới; ghế được giữ 10 phút. Tải lại trang không đặt lại thời gian giữ ghế.
- Thanh toán mẫu hỗ trợ VietQR/VNPay/MoMo/ZaloPay, mã giảm `SMARTBUSNEW` giảm 20.000 đ, và nút thử thất bại. Không chuyển tiền thật.
- Đổi vé giữ số ghế, chỉ đổi chuyến cùng tuyến/cùng giá trước khởi hành. Hủy vé chưa sử dụng trả ghế và mô phỏng hoàn tiền 100%.
- Bác tài chọn chuyến/ngày, nhập hoặc quét QR để check-in; mã trùng/sai chuyến/vé đã hủy bị từ chối. Camera cần trình duyệt hỗ trợ `BarcodeDetector`; luôn có ô nhập mã thủ công.
- Hồ sơ cho phép sửa thông tin liên hệ và gửi yêu cầu ưu đãi để hiển thị trạng thái chờ xét duyệt.

Dữ liệu demo lưu trong bộ nhớ, dùng chung giữa các phiên trình duyệt và mất khi khởi động lại. Chỉ bật khi môi trường là `Development` và `Demo:Enabled=true`; không cần SQL Server trong chế độ này. Đặt `Demo:Enabled=false` để trở lại cấu hình SQL Server hiện có. QR vé demo chỉ chứa mã vé, chưa có chữ ký bảo mật; GPS và xét duyệt ưu đãi chưa tích hợp dịch vụ thật. QRCodeJS, HTML2Canvas và Bootstrap Icons dùng CDN nên các tiện ích này cần Internet khi tải trang lần đầu.

> **He Thong Dat & Quan Ly Ve Xe Buyt Thong Minh Toan Quoc**  
> Du an mon hoc / Do an chuyen nganh nhom TTCSK6N3 (8 thanh vien).

---

## Cong Nghe & Nen Tang
- **Framework**: ASP.NET Core 9.0 Web App (Model-View-Controller)
- **Ngon ngu**: C# (.NET 9)
- **Giao dien (Frontend)**: Razor View (.cshtml), Bootstrap 5, Bootstrap Icons, QRCodeJS, Html2Canvas
- **Design Theme**: SmartBus Go (Tone Deep Teal `#0A4D46` & Mint `#10B981`)
- **Co so du lieu (Database)**: SQL Server (Map theo ERD chuan 16 bang)

---

## Cau Truc Du An
```
SmartBusGo/
|-- Controllers/
|   |-- HomeController.cs        # Trang chu, tra cuu tuyen xe noi bat
|   |-- AccountController.cs     # Dang nhap, Dang ky, Xac thuc OTP 6 so, Google Login
|   |-- DriverController.cs      # Cong dieu hanh rieng cho Bac tai & Phu xe
|   `-- TicketController.cs      # Chi tiet ve dien tu E-Ticket, QR Code, Luu ve offline
|-- Models/
|   `-- ViewModels/              # Cac ViewModels truyen nhan du lieu man hinh
|-- Views/
|   |-- Shared/_Layout.cshtml    # Header/Navbar SmartBus Go, Footer chung
|   |-- Home/                    # Giao dien Trang chu & Tim chuyen
|   |-- Account/                 # Login, Register, VerifyOtp
|   |-- Driver/                  # Cong dang nhap tai xe & Dashboard
|   `-- Ticket/                  # Chi tiet ve cuong Boarding Pass, QR, WakeLock
`-- wwwroot/
    |-- css/smartbus-theme.css   # Bo quy chuan mau sac, the ve, watermark
    `-- js/ticket-qr.js          # Xu ly QR, Screen Wake Lock va Luu ve PNG
```

---

## Quy Uoc Nhanh Git (Git Branching Rules)
De tranh xung dot code giua 8 thanh vien trong nhom, moi nguoi vui long tuan thu quy tac sau:

1. **`main`**: Nhanh chinh thuc, chi merge code khi da test on dinh de nop bai hoac demo. Khong commit truc tiep vao `main`.
2. **`develop`**: Nhanh tich hop chung cua toan bo nhom.
3. **`feature/<ten-tinh-nang>`**: Nhanh ca nhan cua tung thanh vien lam chuc nang duoc phan cong:
   - `feature/auth-ui`: Dang nhap, Dang ky, OTP (Frontend)
   - `feature/ticket-qr`: Giao dien ve dien tu & QR Code (Frontend)
   - `feature/booking-api`: Xu ly logic dat ve, giu ghe (Backend)
   - `feature/payment-api`: Tich hop cong thanh toan (Backend)
   - `feature/gps-tracking`: Ban do vi tri xe truc tiep (Backend/Frontend)

### Quy trinh day code len Git:
```bash
# 1. Chuyen sang nhanh tinh nang cua ban
git checkout -b feature/ten-chuc-nang

# 2. Them file va commit
git add .
git commit -m "feat: mo ta ngan gon cong viec vua lam"

# 3. Day len repo
git push origin feature/ten-chuc-nang

# 4. Len GitHub tao Pull Request (PR) merge vao nhanh develop
```

---

## Huong Dan Chay Du An
1. Clone du an ve may:
   ```bash
   git clone https://github.com/dtc245210002-create/Smart_Bus_Tickets.git
   ```
2. Mo file `WebApplication1.sln` bang Visual Studio 2022 (hoac VS Code).
3. Chay lenh phuc hoi thu vien:
   ```bash
   dotnet restore
   ```
4. Chay ung dung:
   ```bash
   dotnet run
   ```
5. Mo trinh duyet va truy cap: `http://localhost:5122`

---
*(c) 2026 TTCSK6N3 - SmartBus Go Project.*
