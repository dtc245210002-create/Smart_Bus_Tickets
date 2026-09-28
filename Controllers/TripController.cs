using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class TripController : Controller
    {
        // =========================================================================
        // [BACKEND: KẾT NỐI DATABASE VÀ REPOSITORY TẠI ĐÂY]
        // Khai báo ApplicationDbContext qua Dependency Injection:
        //
        // private readonly ApplicationDbContext _context;
        // public TripController(ApplicationDbContext context)
        // {
        //     _context = context;
        // }
        // =========================================================================

        [HttpGet]
        public IActionResult Search(
            string? from = "Hà Nội", 
            string? to = "Hải Phòng", 
            DateTime? date = null, 
            string? sort = "default", 
            string? busType = null, 
            string? timeOfDay = null)
        {
            var searchDate = date ?? DateTime.Today.AddDays(1);

            // =========================================================================
            // [BACKEND: THAY THẾ KHỐI DỮ LIỆU MOCK BẰNG TRUY VẤN LINQ DATABASE THẬT]
            // Ví dụ câu lệnh EF Core:
            //
            // var query = _context.Trips
            //     .Include(t => t.Route)
            //     .Include(t => t.Bus).ThenInclude(b => b.BusType)
            //     .Where(t => t.Route.StartPoint.Contains(from) && t.Route.EndPoint.Contains(to));
            // =========================================================================

            var model = new TripSearchViewModel
            {
                From = from ?? "Hà Nội",
                To = to ?? "Hải Phòng",
                DepartureDate = searchDate,
                SortBy = sort,
                BusTypeFilter = busType,
                TimeOfDayFilter = timeOfDay
            };

            var mockTrips = GetMockTrips(model.From, model.To, searchDate);

            if (!string.IsNullOrEmpty(busType))
            {
                mockTrips = mockTrips.Where(t => t.BusTypeName.Contains(busType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(timeOfDay))
            {
                if (timeOfDay == "morning")
                    mockTrips = mockTrips.Where(t => t.DepartureTime.Hours >= 6 && t.DepartureTime.Hours < 12).ToList();
                else if (timeOfDay == "afternoon")
                    mockTrips = mockTrips.Where(t => t.DepartureTime.Hours >= 12 && t.DepartureTime.Hours < 18).ToList();
                else if (timeOfDay == "evening")
                    mockTrips = mockTrips.Where(t => t.DepartureTime.Hours >= 18 || t.DepartureTime.Hours < 6).ToList();
            }

            mockTrips = sort switch
            {
                "price_asc" => mockTrips.OrderBy(t => t.Price).ToList(),
                "price_desc" => mockTrips.OrderByDescending(t => t.Price).ToList(),
                "time_earliest" => mockTrips.OrderBy(t => t.DepartureTime).ToList(),
                "time_latest" => mockTrips.OrderByDescending(t => t.DepartureTime).ToList(),
                _ => mockTrips
            };

            model.Trips = mockTrips;
            return View(model);
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

        private List<SeatItemViewModel> GenerateSeats(int capacity, string[] bookedSeats)
        {
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
                        Price = 150000m,
                        Status = bookedSeats.Contains(seatCode) ? "BOOKED" : "AVAILABLE"
                    });
                }
            }
            return seats;
        }
    }
}
