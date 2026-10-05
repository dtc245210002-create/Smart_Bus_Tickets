using System.Text.Json;
using WebApplication1.Models.BusLayout;

namespace WebApplication1.Services.BusLayout
{
    public class BusLayoutService : IBusLayoutService
    {
        private readonly List<BusLayoutConfig> _cachedConfigs;

        public BusLayoutService()
        {
            _cachedConfigs = InitializeStandardLayouts();
        }

        public IReadOnlyList<BusLayoutConfig> GetAllPredefinedConfigs()
        {
            return _cachedConfigs.AsReadOnly();
        }

        public BusLayoutConfig GetLayoutConfig(string? busTypeName, int capacity = 0)
        {
            if (!string.IsNullOrEmpty(busTypeName))
            {
                var name = busTypeName.ToLower();

                // 1. Nhận diện xe giường nằm 2 tầng
                if (name.Contains("giường") || name.Contains("sleeper") || name.Contains("2 tầng") || name.Contains("hai tầng"))
                {
                    return _cachedConfigs.First(c => c.BusTypeCode == "SLEEPER_34");
                }

                // 2. Nhận diện Limousine VIP 22 phòng
                if (name.Contains("limousine") || name.Contains("vip") || name.Contains("phòng") || name.Contains("cabin"))
                {
                    return _cachedConfigs.First(c => c.BusTypeCode == "LIMOUSINE_22");
                }

                // 3. Nhận diện xe 45 chỗ
                if (name.Contains("45") || capacity == 45)
                {
                    return _cachedConfigs.First(c => c.BusTypeCode == "SEAT_45");
                }

                // 4. Nhận diện xe 29 chỗ
                if (name.Contains("29") || name.Contains("16") || capacity <= 29)
                {
                    return _cachedConfigs.First(c => c.BusTypeCode == "SEAT_29");
                }
            }

            // Kiểm tra theo capacity nếu tên loại xe không khớp
            if (capacity >= 40)
            {
                return _cachedConfigs.First(c => c.BusTypeCode == "SEAT_45");
            }
            if (capacity >= 30)
            {
                return _cachedConfigs.First(c => c.BusTypeCode == "SLEEPER_34");
            }

            // Mặc định xe ghế ngồi 29 chỗ
            return _cachedConfigs.First(c => c.BusTypeCode == "SEAT_29");
        }

        public List<BusFloorLayout> GenerateFloorLayouts(string? busTypeName, int capacity, IEnumerable<string> bookedSeats, decimal basePrice)
        {
            var config = GetLayoutConfig(busTypeName, capacity);
            var bookedSet = new HashSet<string>(bookedSeats ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            // Clone sơ đồ để gán trạng thái và giá tiền theo từng chuyến cụ thể
            var resultFloors = new List<BusFloorLayout>();

            foreach (var floor in config.Floors)
            {
                var newFloor = new BusFloorLayout
                {
                    FloorNumber = floor.FloorNumber,
                    FloorName = floor.FloorName,
                    TotalRows = floor.TotalRows,
                    TotalCols = floor.TotalCols,
                    Seats = new List<BusSeatPosition>()
                };

                foreach (var seat in floor.Seats)
                {
                    var newSeat = new BusSeatPosition
                    {
                        SeatCode = seat.SeatCode,
                        Floor = seat.Floor,
                        Row = seat.Row,
                        Col = seat.Col,
                        Type = seat.Type,
                        DisplayName = seat.DisplayName,
                        Description = seat.Description,
                        Price = seat.Price > 0 ? seat.Price : basePrice
                    };

                    // Nếu là vị trí có thể đặt vé (ghế/giường/phòng)
                    if (seat.IsBookable)
                    {
                        newSeat.Status = bookedSet.Contains(seat.SeatCode) ? "BOOKED" : "AVAILABLE";
                    }
                    else
                    {
                        newSeat.Status = "NONE";
                    }

                    newFloor.Seats.Add(newSeat);
                }

                resultFloors.Add(newFloor);
            }

            return resultFloors;
        }

        /// <summary>
        /// Khởi tạo cấu hình tiêu chuẩn cho 3 loại xe chủ đạo: 29 chỗ, 45 chỗ và Giường nằm 2 tầng
        /// </summary>
        private List<BusLayoutConfig> InitializeStandardLayouts()
        {
            var list = new List<BusLayoutConfig>();

            // =====================================================================
            // 1. CẤU HÌNH XE GHẾ NGỒI 29 CHỖ (1 TẦNG - 2x2 CÓ LỐI ĐI Ở GIỮA)
            // =====================================================================
            var seat29 = new BusLayoutConfig
            {
                BusTypeCode = "SEAT_29",
                TypeName = "Xe ghế ngồi 29 chỗ",
                TotalFloors = 1,
                Capacity = 29,
                Description = "Ghế ngả cao cấp 29 chỗ, điều hòa mát lạnh, rèm che nắng, cổng sạc USB thoại mái.",
                IconClass = "bi-bus-front"
            };

            var floor29 = new BusFloorLayout
            {
                FloorNumber = 1,
                FloorName = "Sơ đồ xe 29 chỗ",
                TotalRows = 8,
                TotalCols = 4
            };

            // Hàng 0: Đầu xe (Tài xế & Cửa lên)
            floor29.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 1, Type = SeatType.Driver, DisplayName = "Tài xế" });
            floor29.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 2, Type = SeatType.Aisle });
            floor29.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 3, Type = SeatType.Aisle });
            floor29.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 4, Type = SeatType.Door, DisplayName = "Cửa lên" });

            // Hàng 1 -> 6: Mỗi hàng 4 ghế (2 bên trái + lối đi + 1 hoặc 2 bên phải)
            int seat29Index = 1;
            for (int r = 1; r <= 6; r++)
            {
                // Cột 1 & 2: Bên trái
                floor29.Seats.Add(new BusSeatPosition { SeatCode = $"A{seat29Index:D2}", Floor = 1, Row = r, Col = 1, Type = SeatType.RegularSeat, DisplayName = $"A{seat29Index:D2}", Description = "Ghế cửa sổ bên trái" });
                seat29Index++;
                floor29.Seats.Add(new BusSeatPosition { SeatCode = $"A{seat29Index:D2}", Floor = 1, Row = r, Col = 2, Type = SeatType.RegularSeat, DisplayName = $"A{seat29Index:D2}", Description = "Ghế lối đi bên trái" });
                seat29Index++;

                // Cột 3: Lối đi
                floor29.Seats.Add(new BusSeatPosition { Floor = 1, Row = r, Col = 3, Type = SeatType.Aisle });

                // Cột 4: Bên phải
                floor29.Seats.Add(new BusSeatPosition { SeatCode = $"A{seat29Index:D2}", Floor = 1, Row = r, Col = 4, Type = SeatType.RegularSeat, DisplayName = $"A{seat29Index:D2}", Description = "Ghế cửa sổ bên phải" });
                seat29Index++;
            }

            // Hàng 7: 1 ghế phụ + Lối đi + 1 ghế
            floor29.Seats.Add(new BusSeatPosition { SeatCode = $"A{seat29Index:D2}", Floor = 1, Row = 7, Col = 1, Type = SeatType.RegularSeat, DisplayName = $"A{seat29Index:D2}", Description = "Ghế hàng 7 bên trái" });
            seat29Index++;
            floor29.Seats.Add(new BusSeatPosition { Floor = 1, Row = 7, Col = 2, Type = SeatType.Aisle });
            floor29.Seats.Add(new BusSeatPosition { Floor = 1, Row = 7, Col = 3, Type = SeatType.Aisle });
            floor29.Seats.Add(new BusSeatPosition { SeatCode = $"A{seat29Index:D2}", Floor = 1, Row = 7, Col = 4, Type = SeatType.RegularSeat, DisplayName = $"A{seat29Index:D2}", Description = "Ghế hàng 7 bên phải" });
            seat29Index++;

            // Hàng 8 (Hàng cuối): 5 ghế liền nhau (4 cột) -> Ghế A21..A25 hoặc A25..A29
            // Đảm bảo đủ 29 ghế
            for (int c = 1; c <= 4; c++)
            {
                if (seat29Index <= 29)
                {
                    floor29.Seats.Add(new BusSeatPosition { SeatCode = $"A{seat29Index:D2}", Floor = 1, Row = 8, Col = c, Type = SeatType.RegularSeat, DisplayName = $"A{seat29Index:D2}", Description = "Hàng ghế cuối" });
                    seat29Index++;
                }
            }
            while (seat29Index <= 29)
            {
                floor29.Seats.Add(new BusSeatPosition { SeatCode = $"A{seat29Index:D2}", Floor = 1, Row = 8, Col = 4, Type = SeatType.RegularSeat, DisplayName = $"A{seat29Index:D2}", Description = "Hàng ghế cuối" });
                seat29Index++;
            }

            seat29.Floors.Add(floor29);
            list.Add(seat29);

            // =====================================================================
            // 2. CẤU HÌNH XE GHẾ NGỒI 45 CHỖ (1 TẦNG - 2x2 CÓ LỐI ĐI Ở GIỮA)
            // =====================================================================
            var seat45 = new BusLayoutConfig
            {
                BusTypeCode = "SEAT_45",
                TypeName = "Xe ghế ngồi 45 chỗ",
                TotalFloors = 1,
                Capacity = 45,
                Description = "Xe khách chất lượng cao 45 chỗ Hyundai Universe, ghế nỉ êm ái, wifi tốc độ cao.",
                IconClass = "bi-bus-front"
            };

            var floor45 = new BusFloorLayout
            {
                FloorNumber = 1,
                FloorName = "Sơ đồ xe 45 chỗ",
                TotalRows = 11,
                TotalCols = 5
            };

            // Hàng 0: Đầu xe
            floor45.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 1, Type = SeatType.Driver, DisplayName = "Tài xế" });
            floor45.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 2, Type = SeatType.Aisle });
            floor45.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 3, Type = SeatType.Aisle });
            floor45.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 4, Type = SeatType.Aisle });
            floor45.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 5, Type = SeatType.Door, DisplayName = "Cửa lên" });

            // Hàng 1 -> 10: 10 hàng x 4 ghế = 40 ghế (A: Cột 1, B: Cột 2, C: Cột 4, D: Cột 5)
            for (int r = 1; r <= 10; r++)
            {
                floor45.Seats.Add(new BusSeatPosition { SeatCode = $"A{r:D2}", Floor = 1, Row = r, Col = 1, Type = SeatType.RegularSeat, DisplayName = $"A{r:D2}", Description = "Ghế cửa sổ bên trái" });
                floor45.Seats.Add(new BusSeatPosition { SeatCode = $"B{r:D2}", Floor = 1, Row = r, Col = 2, Type = SeatType.RegularSeat, DisplayName = $"B{r:D2}", Description = "Ghế lối đi bên trái" });
                floor45.Seats.Add(new BusSeatPosition { Floor = 1, Row = r, Col = 3, Type = SeatType.Aisle });
                floor45.Seats.Add(new BusSeatPosition { SeatCode = $"C{r:D2}", Floor = 1, Row = r, Col = 4, Type = SeatType.RegularSeat, DisplayName = $"C{r:D2}", Description = "Ghế lối đi bên phải" });
                floor45.Seats.Add(new BusSeatPosition { SeatCode = $"D{r:D2}", Floor = 1, Row = r, Col = 5, Type = SeatType.RegularSeat, DisplayName = $"D{r:D2}", Description = "Ghế cửa sổ bên phải" });
            }

            // Hàng 11 (Hàng cuối): 5 ghế liền nhau = 5 ghế (Tổng cộng đúng 45 ghế)
            floor45.Seats.Add(new BusSeatPosition { SeatCode = "E01", Floor = 1, Row = 11, Col = 1, Type = SeatType.RegularSeat, DisplayName = "E01", Description = "Ghế cuối bên trái" });
            floor45.Seats.Add(new BusSeatPosition { SeatCode = "E02", Floor = 1, Row = 11, Col = 2, Type = SeatType.RegularSeat, DisplayName = "E02", Description = "Ghế cuối" });
            floor45.Seats.Add(new BusSeatPosition { SeatCode = "E03", Floor = 1, Row = 11, Col = 3, Type = SeatType.RegularSeat, DisplayName = "E03", Description = "Ghế cuối giữa xe" });
            floor45.Seats.Add(new BusSeatPosition { SeatCode = "E04", Floor = 1, Row = 11, Col = 4, Type = SeatType.RegularSeat, DisplayName = "E04", Description = "Ghế cuối" });
            floor45.Seats.Add(new BusSeatPosition { SeatCode = "E05", Floor = 1, Row = 11, Col = 5, Type = SeatType.RegularSeat, DisplayName = "E05", Description = "Ghế cuối bên phải" });

            seat45.Floors.Add(floor45);
            list.Add(seat45);

            // =====================================================================
            // 3. CẤU HÌNH XE GIƯỜNG NẰM 2 TẦNG (34 GIƯỜNG - 2 FLOORS, 3 DÃY A, B, C)
            // =====================================================================
            var sleeper34 = new BusLayoutConfig
            {
                BusTypeCode = "SLEEPER_34",
                TypeName = "Xe giường nằm 2 tầng 34 chỗ",
                TotalFloors = 2,
                Capacity = 34,
                Description = "Giường nằm cao cấp 2 tầng Green Express, sạc điện thoại, chăn gối thơm tho, màn rèm riêng tư.",
                IconClass = "bi-moon-stars"
            };

            // --- TẦNG 1 (TẦNG DƯỚI) - 17 GIƯỜNG ---
            var lowerFloor = new BusFloorLayout
            {
                FloorNumber = 1,
                FloorName = "Tầng dưới (Tầng 1)",
                TotalRows = 6,
                TotalCols = 5
            };

            // Đầu xe tầng 1: Tài xế và Cửa
            lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 1, Type = SeatType.Driver, DisplayName = "Tài xế" });
            lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 2, Type = SeatType.Aisle });
            lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 3, Type = SeatType.Aisle });
            lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 4, Type = SeatType.Aisle });
            lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 5, Type = SeatType.Door, DisplayName = "Cửa lên" });

            // Hàng 1 -> 5: Mỗi hàng 3 dãy giường: Dãy Trái (Col 1), Lối đi 1 (Col 2), Dãy Giữa (Col 3), Lối đi 2 (Col 4), Dãy Phải (Col 5)
            // Mã giường tầng 1: A01..A06 (Trái), B01..B06 (Giữa), C01..C05 (Phải)
            for (int r = 1; r <= 5; r++)
            {
                lowerFloor.Seats.Add(new BusSeatPosition { SeatCode = $"A{r:D2}", Floor = 1, Row = r, Col = 1, Type = SeatType.SleeperLower, DisplayName = $"A{r:D2}", Description = "Giường tầng dưới dãy trái" });
                lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = r, Col = 2, Type = SeatType.Aisle });
                lowerFloor.Seats.Add(new BusSeatPosition { SeatCode = $"B{r:D2}", Floor = 1, Row = r, Col = 3, Type = SeatType.SleeperLower, DisplayName = $"B{r:D2}", Description = "Giường tầng dưới dãy giữa" });
                lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = r, Col = 4, Type = SeatType.Aisle });
                lowerFloor.Seats.Add(new BusSeatPosition { SeatCode = $"C{r:D2}", Floor = 1, Row = r, Col = 5, Type = SeatType.SleeperLower, DisplayName = $"C{r:D2}", Description = "Giường tầng dưới dãy phải" });
            }
            // Hàng 6 tầng 1: Giường cuối A06 và B06 (Tổng 15 + 2 = 17 giường)
            lowerFloor.Seats.Add(new BusSeatPosition { SeatCode = "A06", Floor = 1, Row = 6, Col = 1, Type = SeatType.SleeperLower, DisplayName = "A06", Description = "Giường tầng dưới cuối xe" });
            lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = 6, Col = 2, Type = SeatType.Aisle });
            lowerFloor.Seats.Add(new BusSeatPosition { SeatCode = "B06", Floor = 1, Row = 6, Col = 3, Type = SeatType.SleeperLower, DisplayName = "B06", Description = "Giường tầng dưới cuối xe" });
            lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = 6, Col = 4, Type = SeatType.Aisle });
            lowerFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = 6, Col = 5, Type = SeatType.Aisle });

            sleeper34.Floors.Add(lowerFloor);

            // --- TẦNG 2 (TẦNG TRÊN) - 17 GIƯỜNG ---
            var upperFloor = new BusFloorLayout
            {
                FloorNumber = 2,
                FloorName = "Tầng trên (Tầng 2)",
                TotalRows = 6,
                TotalCols = 5
            };

            // Hàng 0 tầng 2: Khoang thoáng đầu xe
            upperFloor.Seats.Add(new BusSeatPosition { Floor = 2, Row = 0, Col = 1, Type = SeatType.Aisle });
            upperFloor.Seats.Add(new BusSeatPosition { Floor = 2, Row = 0, Col = 2, Type = SeatType.Aisle });
            upperFloor.Seats.Add(new BusSeatPosition { Floor = 2, Row = 0, Col = 3, Type = SeatType.Aisle });
            upperFloor.Seats.Add(new BusSeatPosition { Floor = 2, Row = 0, Col = 4, Type = SeatType.Aisle });
            upperFloor.Seats.Add(new BusSeatPosition { Floor = 2, Row = 0, Col = 5, Type = SeatType.Aisle });

            // Hàng 1 -> 5 tầng 2: Giường tầng trên A07..A11, B07..B11, C06..C10
            for (int r = 1; r <= 5; r++)
            {
                int aCode = r + 6; // A07..A11
                int bCode = r + 6; // B07..B11
                int cCode = r + 5; // C06..C10

                upperFloor.Seats.Add(new BusSeatPosition { SeatCode = $"A{aCode:D2}", Floor = 2, Row = r, Col = 1, Type = SeatType.SleeperUpper, DisplayName = $"A{aCode:D2}", Description = "Giường tầng trên dãy trái" });
                upperFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = r, Col = 2, Type = SeatType.Aisle });
                upperFloor.Seats.Add(new BusSeatPosition { SeatCode = $"B{bCode:D2}", Floor = 2, Row = r, Col = 3, Type = SeatType.SleeperUpper, DisplayName = $"B{bCode:D2}", Description = "Giường tầng trên dãy giữa" });
                upperFloor.Seats.Add(new BusSeatPosition { Floor = 1, Row = r, Col = 4, Type = SeatType.Aisle });
                upperFloor.Seats.Add(new BusSeatPosition { SeatCode = $"C{cCode:D2}", Floor = 2, Row = r, Col = 5, Type = SeatType.SleeperUpper, DisplayName = $"C{cCode:D2}", Description = "Giường tầng trên dãy phải" });
            }

            // Hàng 6 tầng 2: A12 & B12
            upperFloor.Seats.Add(new BusSeatPosition { SeatCode = "A12", Floor = 2, Row = 6, Col = 1, Type = SeatType.SleeperUpper, DisplayName = "A12", Description = "Giường tầng trên cuối xe" });
            upperFloor.Seats.Add(new BusSeatPosition { Floor = 2, Row = 6, Col = 2, Type = SeatType.Aisle });
            upperFloor.Seats.Add(new BusSeatPosition { SeatCode = "B12", Floor = 2, Row = 6, Col = 3, Type = SeatType.SleeperUpper, DisplayName = "B12", Description = "Giường tầng trên cuối xe" });
            upperFloor.Seats.Add(new BusSeatPosition { Floor = 2, Row = 6, Col = 4, Type = SeatType.Aisle });
            upperFloor.Seats.Add(new BusSeatPosition { Floor = 2, Row = 6, Col = 5, Type = SeatType.Aisle });

            sleeper34.Floors.Add(upperFloor);
            list.Add(sleeper34);

            // =====================================================================
            // 4. CẤU HÌNH LIMOUSINE VIP 22 PHÒNG (2 TẦNG - MỖI TẦNG 11 CABIN)
            // =====================================================================
            var limo22 = new BusLayoutConfig
            {
                BusTypeCode = "LIMOUSINE_22",
                TypeName = "Limousine VIP 22 Phòng",
                TotalFloors = 2,
                Capacity = 22,
                Description = "Khoang cung điện VIP riêng tư, massage đa điểm, màn hình Android, tai nghe chống ồn.",
                IconClass = "bi-gem"
            };

            var limoFloor1 = new BusFloorLayout { FloorNumber = 1, FloorName = "Tầng 1 (11 Phòng)", TotalRows = 6, TotalCols = 3 };
            limoFloor1.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 1, Type = SeatType.Driver, DisplayName = "Tài xế" });
            limoFloor1.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 2, Type = SeatType.Aisle });
            limoFloor1.Seats.Add(new BusSeatPosition { Floor = 1, Row = 0, Col = 3, Type = SeatType.Door, DisplayName = "Cửa" });

            for (int r = 1; r <= 5; r++)
            {
                limoFloor1.Seats.Add(new BusSeatPosition { SeatCode = $"VIP-T1-{r:D2}", Floor = 1, Row = r, Col = 1, Type = SeatType.VipCabin, DisplayName = $"P.{r:D2}", Description = "Phòng VIP tầng 1 dãy trái" });
                limoFloor1.Seats.Add(new BusSeatPosition { Floor = 1, Row = r, Col = 2, Type = SeatType.Aisle });
                limoFloor1.Seats.Add(new BusSeatPosition { SeatCode = $"VIP-T1-{(r + 5):D2}", Floor = 1, Row = r, Col = 3, Type = SeatType.VipCabin, DisplayName = $"P.{(r + 5):D2}", Description = "Phòng VIP tầng 1 dãy phải" });
            }
            limoFloor1.Seats.Add(new BusSeatPosition { SeatCode = "VIP-T1-11", Floor = 1, Row = 6, Col = 2, Type = SeatType.VipCabin, DisplayName = "P.11", Description = "Phòng VIP cuối tầng 1" });
            limo22.Floors.Add(limoFloor1);

            var limoFloor2 = new BusFloorLayout { FloorNumber = 2, FloorName = "Tầng 2 (11 Phòng)", TotalRows = 6, TotalCols = 3 };
            for (int r = 1; r <= 5; r++)
            {
                limoFloor2.Seats.Add(new BusSeatPosition { SeatCode = $"VIP-T2-{r:D2}", Floor = 2, Row = r, Col = 1, Type = SeatType.VipCabin, DisplayName = $"P.{r + 11:D2}", Description = "Phòng VIP tầng 2 dãy trái" });
                limoFloor2.Seats.Add(new BusSeatPosition { Floor = 2, Row = r, Col = 2, Type = SeatType.Aisle });
                limoFloor2.Seats.Add(new BusSeatPosition { SeatCode = $"VIP-T2-{(r + 5):D2}", Floor = 2, Row = r, Col = 3, Type = SeatType.VipCabin, DisplayName = $"P.{r + 16:D2}", Description = "Phòng VIP tầng 2 dãy phải" });
            }
            limoFloor2.Seats.Add(new BusSeatPosition { SeatCode = "VIP-T2-11", Floor = 2, Row = 6, Col = 2, Type = SeatType.VipCabin, DisplayName = "P.22", Description = "Phòng VIP cuối tầng 2" });
            limo22.Floors.Add(limoFloor2);

            list.Add(limo22);

            return list;
        }
    }
}
