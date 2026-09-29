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

        public TripController(ApplicationDbContext context)
        {
            _context = context;
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

            var model = new TripSearchViewModel
            {
                From = from?.Trim() ?? string.Empty,
                To = to?.Trim() ?? string.Empty,
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

            // Kiểm tra điểm đi trùng điểm đến
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

                tripViewModels.Add(new TripItemViewModel
                {
                    TripId = trip.TripId,
                    OperatorName = trip.Route.RouteName.Contains("VIP") ? "SmartBus VIP Express" : "SmartBus Travel",
                    Rating = 4.8,
                    ReviewCount = 1250,
                    BusTypeName = trip.Bus?.BusType?.TypeName ?? "Xe Limousine Cao Cấp",
                    BusImage = GetBusImage(trip.Bus?.BusType?.TypeName),
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
                    Seats = GenerateSeats(totalCapacity, bookedSeatNumbers, price)
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

            trips.Add(new TripItemViewModel
            {
                TripId = 101,
                OperatorName = "Anh Huy Travel",
                Rating = 4.8,
                ReviewCount = 1398,
                BusTypeName = "Limousine VIP 16 Chỗ",
                BusImage = "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=400&q=80",
                LicensePlate = "29B-888.68",
                DepartureTime = new TimeSpan(9, 0, 0),
                DeparturePoint = "Văn phòng 61 Trần Nhân Tông, Hà Nội",
                ArrivalTime = new TimeSpan(11, 10, 0),
                ArrivalPoint = "Văn phòng 18 Lạch Tray, Hải Phòng",
                DurationText = "2h 10m",
                Price = 150000m,
                OriginalPrice = 175000m,
                AvailableSeats = 14,
                TotalCapacity = 16,
                IsFlashSale = true,
                FlashSaleText = "FLASH SALE 50%",
                NoticeText = $"Chuyến khởi hành {tripDate:dd/MM/yyyy} tuyến Hà Nội - Hải Phòng",
                BoardingPoints = new List<string> { "VP 61 Trần Nhân Tông (09:00)", "Bến xe Nước Ngầm (09:20)", "Đại học Bách Khoa (09:35)" },
                DropOffPoints = new List<string> { "VP 18 Lạch Tray, Hải Phòng (11:10)", "Cầu Rào 2 (11:25)", "Bến xe Cầu Rào (11:35)" },
                Seats = GenerateSeats(16, new[] { "A02", "B04" })
            });

            trips.Add(new TripItemViewModel
            {
                TripId = 102,
                OperatorName = "Hải Phòng Travel (Đất Cảng)",
                Rating = 4.9,
                ReviewCount = 2150,
                BusTypeName = "Limousine VIP 22 Phòng Cung Điện",
                BusImage = "https://images.unsplash.com/photo-1570125909232-eb263c188f7e?w=400&q=80",
                LicensePlate = "15B-668.99",
                DepartureTime = new TimeSpan(13, 30, 0),
                DeparturePoint = "Bến xe Mỹ Đình (Cột số 5)",
                ArrivalTime = new TimeSpan(15, 30, 0),
                ArrivalPoint = "Bến xe Niệm Nghĩa, Hải Phòng",
                DurationText = "2h 00m",
                Price = 180000m,
                OriginalPrice = 200000m,
                AvailableSeats = 8,
                TotalCapacity = 22,
                IsFlashSale = false,
                NoticeText = "Chạy cao tốc Hà Nội - Hải Phòng 5B êm ái",
                BoardingPoints = new List<string> { "Bến xe Mỹ Đình (13:30)", "Nhà Hát Lớn Hà Nội (13:50)", "Cổ Linh - Long Biên (14:10)" },
                DropOffPoints = new List<string> { "Trạm thu phí Bạch Đằng (15:15)", "Bến xe Niệm Nghĩa (15:30)" },
                Seats = GenerateSeats(22, new[] { "A01", "A03", "B01", "B02", "B05" })
            });

            trips.Add(new TripItemViewModel
            {
                TripId = 103,
                OperatorName = "Green Express Bus 4.0",
                Rating = 4.7,
                ReviewCount = 890,
                BusTypeName = "Xe Giường Nằm Cao Cấp 34 Chỗ",
                BusImage = "https://images.unsplash.com/photo-1494515843206-f3117d3f51b7?w=400&q=80",
                LicensePlate = "29B-999.11",
                DepartureTime = new TimeSpan(18, 0, 0),
                DeparturePoint = "Bến xe Giáp Bát (Cửa số 3)",
                ArrivalTime = new TimeSpan(20, 15, 0),
                ArrivalPoint = "Bến xe Vĩnh Niệm, Hải Phòng",
                DurationText = "2h 15m",
                Price = 130000m,
                OriginalPrice = 150000m,
                AvailableSeats = 20,
                TotalCapacity = 34,
                IsFlashSale = true,
                FlashSaleText = "GIẢM 20K HÔM NAY",
                NoticeText = "Miễn phí nước khoáng, khăn lạnh và cổng sạc Type-C tại từng ghế",
                BoardingPoints = new List<string> { "Bến xe Giáp Bát (18:00)", "Bến xe Nước Ngầm (18:20)" },
                DropOffPoints = new List<string> { "Ngã tư Quán Toan (19:50)", "Bến xe Vĩnh Niệm (20:15)" },
                Seats = GenerateSeats(34, new[] { "A01", "A02", "A05", "B03" })
            });

            return trips;
        }
    }
}
