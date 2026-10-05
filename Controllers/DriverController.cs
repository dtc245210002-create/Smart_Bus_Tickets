using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;
namespace WebApplication1.Controllers;
// Frontend demo only. Backend will replace the sample login and sample data.
public class DriverController : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new DriverLoginViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Login(DriverLoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var identifier = model.DriverIdentifier.Trim();
        if ((identifier != "TX-001" && identifier != "0988777999") || model.Password != "Demo123!")
        {
            ModelState.AddModelError(string.Empty, "Dùng tài khoản demo TX-001 và mật khẩu Demo123!.");
            return View(model);
        }
        return RedirectToAction(nameof(Dashboard));
    }
    [HttpGet]
    public IActionResult Dashboard(int? tripId) => View(Sample(tripId));
    [HttpGet]
    public IActionResult Profile() => View(Sample(null));
    [HttpGet]
    public IActionResult Scan(int? tripId) => View(Sample(tripId));
    [HttpGet]
    public IActionResult Incident(int? tripId) => View(Sample(tripId));
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout() => RedirectToAction(nameof(Login));
    private static DriverPortalDemoViewModel Sample(int? tripId)
    {
        var trip = new DriverPortalTripDemoViewModel
        {
            TripId = 1, RouteName = "TN-HN-01", StartPoint = "Thái Nguyên", EndPoint = "Hà Nội",
            TripDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)),
            DepartureTime = new TimeOnly(8, 0), ArrivalTime = new TimeOnly(10, 0),
            LicensePlate = "29B-888.68", BusTypeName = "Xe 29 chỗ", Capacity = 29, BookedSeats = 3,
            Passengers = new()
            {
                new() { FullName = "Nguyễn Văn An", SeatNumber = "A01", TicketCode = "SBG-001", DropOffStop = "Mỹ Đình" },
                new() { FullName = "Trần Văn Bình", SeatNumber = "A02", TicketCode = "SBG-002", DropOffStop = "Mỹ Đình" },
                new() { FullName = "Lê Văn Minh", SeatNumber = "B01", TicketCode = "SBG-003", DropOffStop = "Mỹ Đình" }
            }
        };
        return new DriverPortalDemoViewModel
        {
            DriverId = 1, FullName = "Trần Đình Trọng", Phone = "0988777999",
            LicenseNo = "010088012345", DateOfBirth = new DateOnly(1988, 8, 15),
            LicenseExpiryDate = new DateOnly(2030, 8, 15), SelectedTripId = tripId,
            TodayTripsCount = 1, TotalPassengersCount = 3, AssignedTrips = new() { trip }
        };
    }
}
