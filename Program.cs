using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Đăng ký dịch vụ quản lý giữ chỗ ghế (Thread-safe In-Memory Lock với TTL)
            builder.Services.AddSingleton<WebApplication1.Services.ISeatHoldService, WebApplication1.Services.SeatHoldService>();

            builder.Services.AddDbContext<WebApplication1.Data.ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Đăng ký dịch vụ Quản lý cấu hình loại xe & sơ đồ ghế (29 chỗ, 45 chỗ, giường nằm 2 tầng)
            builder.Services.AddSingleton<WebApplication1.Services.BusLayout.IBusLayoutService, WebApplication1.Services.BusLayout.BusLayoutService>();

            // Đăng ký dịch vụ Soát vé & Xác thực mã QR cho Ứng dụng Nhân viên / Tài xế
            builder.Services.AddScoped<WebApplication1.Services.TicketValidation.ITicketValidationService, WebApplication1.Services.TicketValidation.TicketValidationService>();

            // Đăng ký dịch vụ Gửi Email (SMTP) xác thực tài khoản & thông báo vé
            builder.Services.AddScoped<WebApplication1.Services.Email.IEmailService, WebApplication1.Services.Email.EmailService>();

            // Cấu hình Cookie Authentication cho Đăng nhập / Đăng ký
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = "SmartBusGo_Auth";
                });

            var app = builder.Build();

            // Cấu hình Localization chuẩn Việt Nam (Ngày/Tháng/Năm - dd/MM/yyyy)
            var viCulture = new System.Globalization.CultureInfo("vi-VN");
            viCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            viCulture.DateTimeFormat.DateSeparator = "/";

            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(viCulture),
                SupportedCultures = new[] { viCulture },
                SupportedUICultures = new[] { viCulture }
            };
            app.UseRequestLocalization(localizationOptions);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllers();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
