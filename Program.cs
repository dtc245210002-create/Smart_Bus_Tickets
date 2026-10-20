using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApplication1.Data;
using WebApplication1.Models.Api;
using WebApplication1.Models.DTOs;
using WebApplication1.Models.ViewModels;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(options => options.Filters.Add<BackendAccessFilter>());
            builder.Services.AddScoped<BackendAccessFilter>();

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
                    // Validate DB authority on every request, including cookies minted
                    // by the old driver login which incorrectly granted Admin claims.
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                        {
                            context.RejectPrincipal();
                            return;
                        }
                        try
                        {
                            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                            var principal = await BackendIdentity.LoadAsync(db, userId);
                            if (principal == null) context.RejectPrincipal();
                            else context.ReplacePrincipal(principal);
                        }
                        catch (Exception)
                        {
                            // A database outage must never authenticate a demo user.
                            context.RejectPrincipal();
                        }
                    };
                    options.Events.OnRedirectToLogin = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path == "/Driver/VerifyTicket")
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        else context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    };
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

    // Kept with backend policy registration: these are server authority checks,
    // independent of the frontend's userId/sessionId and visible navigation.
    public static class BackendIdentity
    {
        public static async Task<ClaimsPrincipal?> LoadAsync(ApplicationDbContext db, int userId)
        {
            var user = await db.Users.AsNoTracking().Include(u => u.Roles).Include(u => u.Driver)
                .SingleOrDefaultAsync(u => u.UserId == userId);
            if (user == null || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase)) return null;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.MobilePhone, user.Phone ?? "")
            };
            foreach (var role in user.Roles)
            {
                if (role.RoleName == "Tài xế") continue;
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }
            if (user.Driver != null && string.Equals(user.Driver.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Tài xế"));
                claims.Add(new Claim("DriverId", user.Driver.DriverId.ToString()));
                claims.Add(new Claim("LicenseNo", user.Driver.LicenseNo));
            }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }

    public sealed class BackendAccessFilter(ApplicationDbContext db) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();
            var user = context.HttpContext.User;
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            var authenticated = user.Identity?.IsAuthenticated == true && userId > 0;
            var admin = user.IsInRole("Admin") || user.IsInRole("Quản trị viên");
            var driver = user.IsInRole("Tài xế");

            if (controller == "TripsApi")
            {
                // Anonymous seat maps remain public; identity hints cannot expose
                // somebody else's holds as "mine".
                context.ActionArguments["userId"] = authenticated ? userId : null;
                context.ActionArguments["sessionId"] = authenticated ? $"actor-{userId}" : null;
            }
            var driverArea = controller is "Driver" or "DriverDemo" or "TicketsApi";
            var driverLogin = controller == "Driver" && action == "Login";
            var privateArea = controller is "Admin" or "Ticket" or "BookingsApi" or "TicketEligibilityApi"
                || (driverArea && !driverLogin);
            if (!privateArea) { await next(); return; }
            if (!authenticated) { context.Result = new ChallengeResult(); return; }
            if (controller == "Admin" && !admin || driverArea && !driverLogin && !admin && !driver)
            { context.Result = new ForbidResult(); return; }

            try
            {
                if (controller == "BookingsApi")
                {
                    foreach (var argument in context.ActionArguments.Values)
                    {
                        if (argument is HoldSeatsRequest hold) { hold.UserId = userId; hold.SessionId = $"actor-{userId}"; }
                        if (argument is ReleaseSeatsRequest release) { release.UserId = userId; release.SessionId = $"actor-{userId}"; }
                    }
                    if (action == "GetTripSeatStatus")
                    {
                        context.ActionArguments["userId"] = userId;
                        context.ActionArguments["sessionId"] = $"actor-{userId}";
                    }
                }

                if (controller is "Ticket" or "TicketEligibilityApi" || controller == "BookingsApi" && action == "VerifyPayment")
                {
                    var keys = new List<string>();
                    foreach (var name in new[] { "id", "code", "ticketCode", "bookingCode", "orderCode", "orderId" })
                        if (context.ActionArguments.TryGetValue(name, out var value) && value is string text && !string.IsNullOrWhiteSpace(text))
                            keys.Add(text.Trim());
                    foreach (var value in context.ActionArguments.Values)
                    {
                        if (value is VerifyPaymentRequest payment)
                        {
                            if (!string.IsNullOrWhiteSpace(payment.TicketCode)) keys.Add(payment.TicketCode.Trim());
                            if (!string.IsNullOrWhiteSpace(payment.BookingCode)) keys.Add(payment.BookingCode.Trim());
                        }
                        if (value is RescheduleViewModel reschedule)
                        {
                            if (!string.IsNullOrWhiteSpace(reschedule.OldTicket.TicketCode)) keys.Add(reschedule.OldTicket.TicketCode.Trim());
                            if (reschedule.OldTicket.TicketId > 0) keys.Add(reschedule.OldTicket.TicketId.ToString());
                        }
                    }
                    if (keys.Count == 0) { context.Result = new BadRequestObjectResult(new { success = false, message = "Cần mã vé hoặc đơn đặt vé hợp lệ." }); return; }
                    int? bookingId = null;
                    foreach (var key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        int.TryParse(key, out var ticketId);
                        var matches = await db.Tickets.AsNoTracking()
                            .Where(t => t.Booking.UserId == userId && (t.TicketCode == key || t.Booking.BookingCode == key || t.TicketId == ticketId))
                            .Select(t => t.BookingId).Distinct().Take(2).ToListAsync();
                        if (matches.Count != 1 || bookingId.HasValue && bookingId != matches[0])
                        { context.Result = new NotFoundResult(); return; }
                        bookingId = matches[0];
                    }
                }

                if (controller == "TicketsApi" || controller == "Driver" && action == "VerifyTicket")
                {
                    var qr = context.ActionArguments.Values.OfType<ValidateQrRequest>().FirstOrDefault();
                    var quick = context.ActionArguments.Values.OfType<Controllers.VerifyTicketRequest>().FirstOrDefault();
                    var tripId = qr?.CurrentTripId ?? quick?.TripId;
                    if (!tripId.HasValue || tripId <= 0)
                    { context.Result = new BadRequestObjectResult(new { success = false, message = "Cần chuyến xe đang soát vé." }); return; }
                    if (!admin && !await db.Trips.AnyAsync(t => t.TripId == tripId && t.Driver != null && t.Driver.UserId == userId && t.Driver.Status == "Active"))
                    { context.Result = new ForbidResult(); return; }
                    var validation = context.HttpContext.RequestServices.GetRequiredService<Services.TicketValidation.ITicketValidationService>();
                    var ticketCode = validation.ExtractTicketCode(qr?.QrPayload ?? quick?.EffectiveCode ?? "");
                    if (string.IsNullOrWhiteSpace(ticketCode) || !await db.Tickets.AnyAsync(t => t.TicketCode == ticketCode && t.Booking.TripId == tripId))
                    { context.Result = new NotFoundResult(); return; }
                    // The legacy validator still uses fuzzy/group matching. Do not
                    // let an exact authorized ticket become a scan of other tickets.
                    var key = ticketCode.Trim().ToUpperInvariant();
                    var matches = await db.Tickets.AsNoTracking().Where(t => t.TicketCode == key
                        || t.TicketCode.StartsWith(key + "-") || t.TicketCode.StartsWith(key + "_")
                        || key.Length >= 6 && t.TicketCode.Contains(key)
                        || t.Booking.BookingCode == key || t.Booking.BookingCode.Contains(key)
                        || t.TicketId.ToString() == key)
                        .Select(t => new { t.TicketCode, t.Status, t.Booking.TripId, t.Booking.TotalAmount,
                            PaidAmount = t.Booking.Payments.Where(p => p.Status == "Paid").Sum(p => p.Amount) })
                        .ToListAsync();
                    if (matches.Count != 1 || matches[0].TripId != tripId || !string.Equals(matches[0].TicketCode, key, StringComparison.OrdinalIgnoreCase))
                    { context.Result = new ConflictObjectResult(new { success = false, message = "Mã vé khớp nhiều dữ liệu. Cần đối soát trước khi soát vé." }); return; }
                    if (matches[0].TotalAmount <= 0 || matches[0].PaidAmount < matches[0].TotalAmount)
                    { context.Result = new BadRequestObjectResult(new { success = false, message = "Vé chưa có thanh toán được xác nhận." }); return; }
                }
                await next();
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                context.Result = new ObjectResult(new { success = false, message = "Không thể kiểm tra dữ liệu lúc này. Vui lòng thử lại." }) { StatusCode = 503 };
            }
        }
    }
}
