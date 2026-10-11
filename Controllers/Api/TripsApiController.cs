using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsApiController : ControllerBase
    {
        private readonly SmartBusDbContext _context;

        public TripsApiController(SmartBusDbContext context)
        {
            _context = context;
        }

        // GET: /api/tripsapi/search?from=Hà Nội&to=Thái Nguyên&date=2026-10-01
        [HttpGet("search")]
        public async Task<IActionResult> SearchTrips(string? from, string? to, string? date)
        {
            // 1. Truy vấn chuyến xe kèm các bảng liên quan từ CSDL
            var query = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Bus).ThenInclude(b => b.BusType)
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .AsQueryable();

            // 2. Lọc theo ngày đi nếu có
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
            {
                query = query.Where(t => t.TripDate.Date == parsedDate.Date);
            }

            var trips = await query.ToListAsync();

            // 3. Lọc thông minh hỗ trợ cả tiếng Việt có dấu, không dấu, viết tắt
            if (!string.IsNullOrWhiteSpace(from))
            {
                trips = trips.Where(t => 
                    MatchesSearch(t.Route?.StartPoint, from) || 
                    MatchesSearch(t.Route?.RouteName, from) || 
                    MatchesSearch(t.Route?.RouteCode, from)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                trips = trips.Where(t => 
                    MatchesSearch(t.Route?.EndPoint, to) || 
                    MatchesSearch(t.Route?.RouteName, to) || 
                    MatchesSearch(t.Route?.RouteCode, to)).ToList();
            }

            // 4. Tính toán thời gian thực: số vé đã bán, số ghế trống, giá vé
            var results = new List<object>();

            foreach (var trip in trips)
            {
                var soldTickets = await _context.Tickets
                    .Where(tk => tk.Booking.TripId == trip.TripId && tk.Status != "CANCELLED")
                    .CountAsync();

                var capacity = trip.Bus?.Capacity ?? 29;
                var availableSeats = Math.Max(0, capacity - soldTickets);

                var sampleTicket = await _context.Tickets
                    .Where(tk => tk.Booking.TripId == trip.TripId)
                    .FirstOrDefaultAsync();
                var basePrice = sampleTicket?.Price ?? 100000m;

                results.Add(new
                {
                    tripId = trip.TripId,
                    routeCode = trip.Route?.RouteCode ?? "HN-TN-01",
                    routeName = trip.Route?.RouteName ?? "Hà Nội - Thái Nguyên",
                    startPoint = trip.Route?.StartPoint ?? "Hà Nội",
                    endPoint = trip.Route?.EndPoint ?? "Thái Nguyên",
                    distanceKm = trip.Route?.Distance ?? 80m,
                    tripDate = trip.TripDate.ToString("dd/MM/yyyy"),
                    tripDateRaw = trip.TripDate.ToString("yyyy-MM-dd"),
                    departureTime = trip.DepartureTime.ToString(@"hh\:mm"),
                    arrivalTime = trip.ArrivalTime.HasValue ? trip.ArrivalTime.Value.ToString(@"hh\:mm") : "08:30",
                    bus = new
                    {
                        licensePlate = trip.Bus?.LicensePlate ?? "20A-12345",
                        busTypeName = trip.Bus?.BusType?.TypeName ?? "Xe 29 chỗ",
                        capacity = capacity
                    },
                    driverName = trip.Driver?.User?.FullName ?? "Nguyễn Văn An",
                    soldTickets = soldTickets,
                    availableSeats = availableSeats,
                    price = basePrice,
                    status = trip.Status ?? "Scheduled"
                });
            }

            return Ok(new
            {
                success = true,
                count = results.Count,
                data = results
            });
        }

        // GET: /api/tripsapi/cities
        [HttpGet("cities")]
        public async Task<IActionResult> GetAvailableCities()
        {
            var startPoints = await _context.Routes.Select(r => r.StartPoint).Distinct().ToListAsync();
            var endPoints = await _context.Routes.Select(r => r.EndPoint).Distinct().ToListAsync();
            var allCities = startPoints.Union(endPoints)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            return Ok(new
            {
                success = true,
                cities = allCities
            });
        }

        private static readonly Dictionary<string, string[]> CityAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["hochiminh"] = new[] { "saigon", "sg", "hcm", "tphcm" },
            ["hanoi"] = new[] { "hn" },
            ["danang"] = new[] { "dn" },
            ["buonmathuot"] = new[] { "bmt", "daklak" },
            ["vungtau"] = new[] { "vt" },
            ["cantho"] = new[] { "ct" },
            ["dalat"] = new[] { "dl" },
            ["nhatrang"] = new[] { "nt" },
            ["haiphong"] = new[] { "hp" },
            ["quynhon"] = new[] { "qn" },
            ["ninhbinh"] = new[] { "nb", "trangan" },
            ["hagiang"] = new[] { "hg", "dongvan" },
            ["caobang"] = new[] { "cb", "bangioc" },
            ["quangbinh"] = new[] { "qb", "donghoi", "phongnha" },
            ["phanthiet"] = new[] { "pt", "muine" },
            ["camau"] = new[] { "cm", "datmui" },
            ["rachgia"] = new[] { "rg", "kiengiang" },
            ["chaudoc"] = new[] { "cd", "angiang" },
            ["mocchau"] = new[] { "mc", "sonla" },
            ["thainguyen"] = new[] { "tn" },
            ["sapa"] = new[] { "sp", "laocai" },
            ["halong"] = new[] { "hl", "quangninh", "baichay" }
        };

        // Hàm so khớp tìm kiếm tiếng Việt thông minh (hỗ trợ có dấu, không dấu, viết liền, viết tắt, địa danh phụ)
        private static bool MatchesSearch(string? source, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return true;
            if (string.IsNullOrWhiteSpace(source)) return false;

            var normSource = NormalizeString(source);
            var normKeyword = NormalizeString(keyword);

            // 1. So khớp trực tiếp hoặc so khớp bỏ dấu
            if (normSource.Contains(normKeyword) || normKeyword.Contains(normSource)) return true;

            // 2. So khớp không khoảng trắng (ví dụ "hanoi" khớp "ha noi", "danang" khớp "da nang", "sapa" khớp "sa pa")
            var compactSource = normSource.Replace(" ", "");
            var compactKeyword = normKeyword.Replace(" ", "");
            if (compactSource.Contains(compactKeyword) || compactKeyword.Contains(compactSource)) return true;

            // 3. Hỗ trợ tên gọi thông dụng, viết tắt, tỉnh thành tương ứng
            foreach (var kvp in CityAliases)
            {
                var canonical = kvp.Key;
                var aliases = kvp.Value;

                bool sourceMatches = compactSource.Contains(canonical) || aliases.Any(a => compactSource.Contains(a));
                bool keywordMatches = compactKeyword == canonical || compactKeyword.Contains(canonical) || aliases.Any(a => compactKeyword == a || compactKeyword.Contains(a));

                if (sourceMatches && keywordMatches) return true;
            }

            // 4. So khớp từng từ rời
            var keywordParts = normKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return keywordParts.Length > 0 && keywordParts.All(part => normSource.Contains(part) || compactSource.Contains(part));
        }

        private static string NormalizeString(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            // Thay thế chữ 'đ' / 'Đ' thành 'd'
            text = text.Replace("đ", "d").Replace("Đ", "d").Replace("?", "o");
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLower().Trim();
        }
    }
}
