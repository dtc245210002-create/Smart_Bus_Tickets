using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class TripController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WebApplication1.Services.BusLayout.IBusLayoutService _busLayoutService;

        public TripController(ApplicationDbContext context, WebApplication1.Services.BusLayout.IBusLayoutService busLayoutService)
        {
            _context = context;
            _busLayoutService = busLayoutService;
        }

        [HttpGet]
        public IActionResult Search(
            string? from = "Hà Nội", 
            string? to = "Hải Phòng", 
            DateTime? date = null, 
            string? sort = "default", 
            string? busType = null, 
            string? timeOfDay = null)
        {
            // Mặc định ngày khởi hành là ngày hiện tại hôm nay
            var searchDate = date ?? DateTime.Today;

            var formattedFrom = FormatToTitleCase(from);
            var formattedTo = FormatToTitleCase(to);

            var model = new TripSearchViewModel
            {
                From = formattedFrom,
                To = formattedTo,
                DepartureDate = searchDate,
                SortBy = sort,
                BusTypeFilter = busType,
                TimeOfDayFilter = timeOfDay
            };

            // =========================================================================
            // 1. LOGIC VALIDATE DỮ LIỆU ĐẦU VÀO (INPUT VALIDATION)
            // =========================================================================

            // Kiểm tra rỗng: Điểm đi và điểm đến không được để trống
            if (string.IsNullOrWhiteSpace(model.From) || string.IsNullOrWhiteSpace(model.To))
            {
                model.ErrorMessage = "Vui lòng nhập đầy đủ nơi xuất phát và nơi đến.";
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
                return View(model);
            }

            // Kiểm tra điểm đi không được trùng điểm đến (bỏ qua hoa thường và khoảng trắng)
            if (string.Equals(model.From, model.To, StringComparison.OrdinalIgnoreCase))
            {
                model.ErrorMessage = "Điểm đi và điểm đến không được trùng nhau! Vui lòng chọn lộ trình khác.";
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
                return View(model);
            }

            // Kiểm tra ngày chọn không nhỏ hơn ngày hiện tại
            if (searchDate.Date < DateTime.Today)
            {
                model.ErrorMessage = $"Ngày khởi hành ({searchDate:dd/MM/yyyy}) không thể là ngày trong quá khứ! Vui lòng chọn ngày từ hôm nay ({DateTime.Today:dd/MM/yyyy}) trở đi.";
                ModelState.AddModelError(string.Empty, model.ErrorMessage);
                return View(model);
            }

            // =========================================================================
            // 2. TRUY VẤN DATABASE: LỌC CÁC CHUYẾN XE KHỚP ĐIỀU KIỆN & CÒN GHẾ TRỐNG
            // =========================================================================
            var tripDateOnly = DateOnly.FromDateTime(searchDate.Date);

            // Truy vấn database bằng EF Core bao gồm các bảng: Route, Bus, BusType, Driver, Booking, Ticket
            var tripsFromDb = _context.Trips
                .Include(t => t.Route)
                    .ThenInclude(r => r.RouteStops.OrderBy(rs => rs.StopOrder))
                        .ThenInclude(rs => rs.Stop)
                .Include(t => t.Bus)
                    .ThenInclude(b => b.BusType)
                .Include(t => t.Driver)
                    .ThenInclude(d => d.User)
                .Include(t => t.Bookings)
                    .ThenInclude(bk => bk.Tickets)
                .Where(t =>
                    // Lọc tuyến đường theo điểm đi và điểm đến
                    (t.Route.StartPoint.Contains(model.From) || t.Route.RouteName.Contains(model.From)) &&
                    (t.Route.EndPoint.Contains(model.To) || t.Route.RouteName.Contains(model.To)) &&
                    // Lọc theo ngày đi
                    t.TripDate == tripDateOnly &&
                    // Chuyến xe đang hoạt động / không bị hủy
                    (t.Status == null || (t.Status != "Cancelled" && t.Status != "CANCELLED"))
                )
                .ToList();

            var tripViewModels = new List<TripItemViewModel>();

            foreach (var trip in tripsFromDb)
            {
                // Danh sách vé đã đặt hợp lệ
                var validTickets = trip.Bookings
                    .Where(bk => bk.Status != "Cancelled" && bk.Status != "CANCELLED")
                    .SelectMany(bk => bk.Tickets)
                    .Where(tk => tk.Status != "Cancelled" && tk.Status != "CANCELLED")
                    .ToList();

                var bookedSeatNumbers = validTickets
                    .Where(tk => !string.IsNullOrEmpty(tk.SeatNumber))
                    .Select(tk => tk.SeatNumber!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var totalCapacity = trip.Bus?.Capacity ?? 29;
                var bookedCount = validTickets.Count;
                var availableSeats = totalCapacity - bookedCount;

                // =========================================================================
                // ĐIỀU KIỆN: CHỈ LẤY CÁC CHUYẾN CÒN GHẾ TRỐNG (AvailableSeats > 0)
                // =========================================================================
                if (availableSeats <= 0)
                {
                    continue; // Bỏ qua nếu chuyến đã hết chỗ
                }

                // Điểm đón và điểm trả từ danh sách trạm dừng RouteStops
                var boardingStops = trip.Route.RouteStops
                    .OrderBy(rs => rs.StopOrder)
                    .Take(3)
                    .Select(rs => $"{rs.Stop.StopName} ({(rs.DepartureTime.HasValue ? rs.DepartureTime.Value.ToString(@"hh\:mm") : trip.DepartureTime.ToString(@"hh\:mm"))})")
                    .ToList();

                var dropOffStops = trip.Route.RouteStops
                    .OrderBy(rs => rs.StopOrder)
                    .Skip(Math.Max(0, trip.Route.RouteStops.Count - 2))
                    .Select(rs => $"{rs.Stop.StopName} ({(rs.ArrivalTime.HasValue ? rs.ArrivalTime.Value.ToString(@"hh\:mm") : (trip.ArrivalTime.HasValue ? trip.ArrivalTime.Value.ToString(@"hh\:mm") : "11:00"))})")
                    .ToList();

                if (!boardingStops.Any())
                {
                    boardingStops.Add($"{trip.Route.StartPoint} ({trip.DepartureTime:hh\\:mm})");
                }
                if (!dropOffStops.Any())
                {
                    dropOffStops.Add($"{trip.Route.EndPoint} ({(trip.ArrivalTime.HasValue ? trip.ArrivalTime.Value.ToString(@"hh\:mm") : "10:30")})");
                }

                var departureTimeSpan = trip.DepartureTime.ToTimeSpan();
                var arrivalTimeSpan = trip.ArrivalTime?.ToTimeSpan() ?? departureTimeSpan.Add(TimeSpan.FromMinutes(trip.Route.EstimatedDuration ?? 120));
                var duration = arrivalTimeSpan - departureTimeSpan;
                if (duration < TimeSpan.Zero) duration = duration.Add(TimeSpan.FromHours(24));
                var durationText = $"{(int)duration.TotalHours}h {duration.Minutes}m";

                var price = validTickets.FirstOrDefault()?.Price ?? 150000m;
                var originalPrice = price * 1.15m;

                var busTypeName = trip.Bus?.BusType?.TypeName ?? "Xe 29 chỗ";
                var floorLayouts = _busLayoutService.GenerateFloorLayouts(busTypeName, totalCapacity, bookedSeatNumbers, price);
                var flatSeats = floorLayouts.SelectMany(f => f.Seats)
                    .Where(s => s.IsBookable)
                    .Select(s => new SeatItemViewModel
                    {
                        SeatCode = s.SeatCode,
                        Floor = s.Floor,
                        Price = s.Price,
                        Status = s.Status
                    }).ToList();

                tripViewModels.Add(new TripItemViewModel
                {
                    TripId = trip.TripId,
                    OperatorName = trip.Route.RouteName.Contains("VIP") ? "SmartBus VIP Express" : "SmartBus Travel",
                    Rating = 4.8,
                    ReviewCount = 1250,
                    BusTypeName = busTypeName,
                    BusImage = GetBusImage(busTypeName),
                    LicensePlate = trip.Bus?.LicensePlate ?? "29B-888.88",
                    DepartureTime = departureTimeSpan,
                    DeparturePoint = boardingStops.FirstOrDefault() ?? trip.Route.StartPoint,
                    ArrivalTime = arrivalTimeSpan,
                    ArrivalPoint = dropOffStops.LastOrDefault() ?? trip.Route.EndPoint,
                    DurationText = durationText,
                    Price = price,
                    OriginalPrice = originalPrice,
                    AvailableSeats = availableSeats,
                    TotalCapacity = totalCapacity,
                    IsFlashSale = availableSeats < 10,
                    FlashSaleText = availableSeats < 10 ? "SẮP HẾT CHỖ" : "ƯU ĐÃI ĐẶT SỚM",
                    NoticeText = $"Chuyến khởi hành ngày {trip.TripDate:dd/MM/yyyy} từ {trip.Route.StartPoint} đi {trip.Route.EndPoint}",
                    BoardingPoints = boardingStops,
                    DropOffPoints = dropOffStops,
                    BusTypeCode = _busLayoutService.GetLayoutConfig(busTypeName, totalCapacity).BusTypeCode,
                    FloorLayouts = floorLayouts,
                    Seats = flatSeats
                });
            }

            // Nếu DB chưa có dữ liệu cho ngày này, fallback sang mock data để hiển thị demo ổn định
            if (!tripViewModels.Any() && !tripsFromDb.Any())
            {
                var mockTrips = GetMockTrips(model.From, model.To, searchDate);
                tripViewModels.AddRange(mockTrips);
            }

            // Lọc theo loại xe
            if (!string.IsNullOrEmpty(busType))
            {
                tripViewModels = tripViewModels.Where(t => t.BusTypeName.Contains(busType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Lọc theo khung giờ
            if (!string.IsNullOrEmpty(timeOfDay))
            {
                if (timeOfDay == "morning")
                    tripViewModels = tripViewModels.Where(t => t.DepartureTime.Hours >= 6 && t.DepartureTime.Hours < 12).ToList();
                else if (timeOfDay == "afternoon")
                    tripViewModels = tripViewModels.Where(t => t.DepartureTime.Hours >= 12 && t.DepartureTime.Hours < 18).ToList();
                else if (timeOfDay == "evening")
                    tripViewModels = tripViewModels.Where(t => t.DepartureTime.Hours >= 18 || t.DepartureTime.Hours < 6).ToList();
            }

            // Sắp xếp
            tripViewModels = sort switch
            {
                "price_asc" => tripViewModels.OrderBy(t => t.Price).ToList(),
                "price_desc" => tripViewModels.OrderByDescending(t => t.Price).ToList(),
                "time_earliest" => tripViewModels.OrderBy(t => t.DepartureTime).ToList(),
                "time_latest" => tripViewModels.OrderByDescending(t => t.DepartureTime).ToList(),
                _ => tripViewModels
            };

            model.Trips = tripViewModels;
            return View(model);
        }

        private string GetBusImage(string? busType)
        {
            if (busType != null && busType.Contains("Limousine", StringComparison.OrdinalIgnoreCase))
                return "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80";
            if (busType != null && busType.Contains("Giường", StringComparison.OrdinalIgnoreCase))
                return "https://images.unsplash.com/photo-1570125909232-eb263c188f7e?w=400&q=80";
            return "https://images.unsplash.com/photo-1494515843206-f3117d3f51b7?w=400&q=80";
        }

        private List<SeatItemViewModel> GenerateSeats(int capacity, IEnumerable<string> bookedSeats, decimal price = 150000m)
        {
            var bookedSet = new HashSet<string>(bookedSeats, StringComparer.OrdinalIgnoreCase);
            var seats = new List<SeatItemViewModel>();
            var rows = (capacity + 3) / 4;

            for (int r = 1; r <= rows; r++)
            {
                foreach (var col in new[] { "A", "B", "C", "D" })
                {
                    var seatCode = $"{col}{r:D2}";
                    if (seats.Count >= capacity) break;

                    seats.Add(new SeatItemViewModel
                    {
                        SeatCode = seatCode,
                        Floor = r <= (rows / 2) ? 1 : 2,
                        Price = price,
                        Status = bookedSet.Contains(seatCode) ? "BOOKED" : "AVAILABLE"
                    });
                }
            }
            return seats;
        }

        private List<TripItemViewModel> GetMockTrips(string from, string to, DateTime tripDate)
        {
            var trips = new List<TripItemViewModel>();

            // 1. Xe ghế ngồi 29 chỗ
            var trip101Type = "Xe ghế ngồi 29 chỗ";
            var trip101Floors = _busLayoutService.GenerateFloorLayouts(trip101Type, 29, new[] { "A02", "A05", "A12" }, 140000m);
            trips.Add(new TripItemViewModel
            {
                TripId = 101,
                OperatorName = "Anh Huy Travel",
                Rating = 4.8,
                ReviewCount = 1398,
                BusTypeName = trip101Type,
                BusImage = GetBusImage(trip101Type),
                LicensePlate = "29B-888.68",
                DepartureTime = new TimeSpan(9, 0, 0),
                DeparturePoint = "Văn phòng 61 Trần Nhân Tông, Hà Nội",
                ArrivalTime = new TimeSpan(11, 10, 0),
                ArrivalPoint = "Văn phòng 18 Lạch Tray, Hải Phòng",
                DurationText = "2h 10m",
                Price = 140000m,
                OriginalPrice = 160000m,
                AvailableSeats = 26,
                TotalCapacity = 29,
                IsFlashSale = true,
                FlashSaleText = "FLASH SALE 50%",
                NoticeText = $"Chuyến khởi hành {tripDate:dd/MM/yyyy} tuyến Hà Nội - Hải Phòng",
                BoardingPoints = new List<string> { "VP 61 Trần Nhân Tông (09:00)", "Bến xe Nước Ngầm (09:20)", "Đại học Bách Khoa (09:35)" },
                DropOffPoints = new List<string> { "VP 18 Lạch Tray, Hải Phòng (11:10)", "Cầu Rào 2 (11:25)", "Bến xe Cầu Rào (11:35)" },
                BusTypeCode = "SEAT_29",
                FloorLayouts = trip101Floors,
                Seats = trip101Floors.SelectMany(f => f.Seats).Where(s => s.IsBookable).Select(s => new SeatItemViewModel { SeatCode = s.SeatCode, Floor = s.Floor, Price = s.Price, Status = s.Status }).ToList()
            });

            // 2. Xe ghế ngồi 45 chỗ
            var trip102Type = "Xe ghế ngồi 45 chỗ Hyundai Universe";
            var trip102Floors = _busLayoutService.GenerateFloorLayouts(trip102Type, 45, new[] { "A01", "A02", "B01", "B02", "C05", "D05" }, 120000m);
            trips.Add(new TripItemViewModel
            {
                TripId = 102,
                OperatorName = "Hoàng Long Express",
                Rating = 4.6,
                ReviewCount = 980,
                BusTypeName = trip102Type,
                BusImage = GetBusImage(trip102Type),
                LicensePlate = "15B-123.45",
                DepartureTime = new TimeSpan(11, 30, 0),
                DeparturePoint = "Bến xe Giáp Bát (Quầy vé 14)",
                ArrivalTime = new TimeSpan(13, 45, 0),
                ArrivalPoint = "Bến xe Niệm Nghĩa, Hải Phòng",
                DurationText = "2h 15m",
                Price = 120000m,
                OriginalPrice = 140000m,
                AvailableSeats = 39,
                TotalCapacity = 45,
                IsFlashSale = false,
                NoticeText = "Xe 45 chỗ đời mới, khoang hành lý rộng rãi, đón trả đúng giờ",
                BoardingPoints = new List<string> { "Bến xe Giáp Bát (11:30)", "Bến xe Nước Ngầm (11:50)" },
                DropOffPoints = new List<string> { "Quán Toan (13:20)", "Bến xe Niệm Nghĩa (13:45)" },
                BusTypeCode = "SEAT_45",
                FloorLayouts = trip102Floors,
                Seats = trip102Floors.SelectMany(f => f.Seats).Where(s => s.IsBookable).Select(s => new SeatItemViewModel { SeatCode = s.SeatCode, Floor = s.Floor, Price = s.Price, Status = s.Status }).ToList()
            });

            // 3. Xe giường nằm 2 tầng 34 chỗ
            var trip103Type = "Xe giường nằm 2 tầng 34 chỗ";
            var trip103Floors = _busLayoutService.GenerateFloorLayouts(trip103Type, 34, new[] { "A01", "A02", "B02", "C03", "A08", "B10" }, 160000m);
            trips.Add(new TripItemViewModel
            {
                TripId = 103,
                OperatorName = "Green Express Bus 4.0",
                Rating = 4.9,
                ReviewCount = 2150,
                BusTypeName = trip103Type,
                BusImage = GetBusImage(trip103Type),
                LicensePlate = "29B-999.11",
                DepartureTime = new TimeSpan(14, 0, 0),
                DeparturePoint = "Bến xe Mỹ Đình (Cột số 5)",
                ArrivalTime = new TimeSpan(16, 15, 0),
                ArrivalPoint = "Bến xe Vĩnh Niệm, Hải Phòng",
                DurationText = "2h 15m",
                Price = 160000m,
                OriginalPrice = 180000m,
                AvailableSeats = 28,
                TotalCapacity = 34,
                IsFlashSale = true,
                FlashSaleText = "GIẢM 20K HÔM NAY",
                NoticeText = "Giường nằm 2 tầng cao cấp, chăn gối thơm tho, màn rèm che riêng tư từng giường",
                BoardingPoints = new List<string> { "Bến xe Mỹ Đình (14:00)", "Nhà Hát Lớn (14:20)", "Cổ Linh - Long Biên (14:40)" },
                DropOffPoints = new List<string> { "Trạm thu phí Bạch Đằng (15:55)", "Bến xe Vĩnh Niệm (16:15)" },
                BusTypeCode = "SLEEPER_34",
                FloorLayouts = trip103Floors,
                Seats = trip103Floors.SelectMany(f => f.Seats).Where(s => s.IsBookable).Select(s => new SeatItemViewModel { SeatCode = s.SeatCode, Floor = s.Floor, Price = s.Price, Status = s.Status }).ToList()
            });

            // 4. Limousine VIP 22 Phòng Cung Điện
            var trip104Type = "Limousine VIP 22 Phòng Cung Điện";
            var trip104Floors = _busLayoutService.GenerateFloorLayouts(trip104Type, 22, new[] { "VIP-T1-01", "VIP-T1-03", "VIP-T2-02" }, 220000m);
            trips.Add(new TripItemViewModel
            {
                TripId = 104,
                OperatorName = "Hải Phòng Travel (Đất Cảng)",
                Rating = 4.9,
                ReviewCount = 3120,
                BusTypeName = trip104Type,
                BusImage = GetBusImage(trip104Type),
                LicensePlate = "15B-668.99",
                DepartureTime = new TimeSpan(18, 30, 0),
                DeparturePoint = "Bến xe Nước Ngầm (Khu VIP)",
                ArrivalTime = new TimeSpan(20, 30, 0),
                ArrivalPoint = "Văn phòng Đất Cảng, Hải Phòng",
                DurationText = "2h 00m",
                Price = 220000m,
                OriginalPrice = 250000m,
                AvailableSeats = 19,
                TotalCapacity = 22,
                IsFlashSale = false,
                NoticeText = "Khoang cung điện VIP 2 tầng, tivi giải trí, massage, sạc type-C",
                BoardingPoints = new List<string> { "Bến xe Nước Ngầm (18:30)", "Văn phòng Cầu Giấy (18:50)" },
                DropOffPoints = new List<string> { "Cầu Rào 1 (20:15)", "VP Đất Cảng (20:30)" },
                BusTypeCode = "LIMOUSINE_22",
                FloorLayouts = trip104Floors,
                Seats = trip104Floors.SelectMany(f => f.Seats).Where(s => s.IsBookable).Select(s => new SeatItemViewModel { SeatCode = s.SeatCode, Floor = s.Floor, Price = s.Price, Status = s.Status }).ToList()
            });

            return trips;
        }

        private static string FormatToTitleCase(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var textInfo = new System.Globalization.CultureInfo("vi-VN", false).TextInfo;
            return textInfo.ToTitleCase(text.Trim().ToLower());
        }
    }
}
