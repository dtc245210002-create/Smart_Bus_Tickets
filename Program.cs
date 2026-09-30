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

            builder.Services.AddDbContext<WebApplication1.Data.ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Đăng ký dịch vụ Quản lý cấu hình loại xe & sơ đồ ghế (29 chỗ, 45 chỗ, giường nằm 2 tầng)
            builder.Services.AddSingleton<WebApplication1.Services.BusLayout.IBusLayoutService, WebApplication1.Services.BusLayout.BusLayoutService>();

            // Đăng ký dịch vụ Soát vé & Xác thực mã QR cho Ứng dụng Nhân viên / Tài xế
            builder.Services.AddScoped<WebApplication1.Services.TicketValidation.ITicketValidationService, WebApplication1.Services.TicketValidation.TicketValidationService>();

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

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
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
