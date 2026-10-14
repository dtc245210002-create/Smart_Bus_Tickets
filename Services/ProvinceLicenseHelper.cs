using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WebApplication1.Models.Entities;

namespace WebApplication1.Services
{
    /// <summary>
    /// Công cụ quản lý và đối soát mã biển số xe của 63 tỉnh thành Việt Nam
    /// Hỗ trợ tra cứu biển số theo tỉnh đi/đến, nhận diện tên tỉnh từ biển số xe
    /// </summary>
    public static class ProvinceLicenseHelper
    {
        // Danh mục mã đầu biển số các tỉnh thành Việt Nam (Thông tư 24/2023/TT-BCA)
        private static readonly Dictionary<string, string> ProvinceToCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            // Đồng bằng sông Hồng & Đông Bắc Bộ
            ["Thái Bình"] = "17",
            ["Hải Phòng"] = "15,16",
            ["Hà Nội"] = "29,30,31,32,33,40",
            ["Nam Định"] = "18",
            ["Hải Dương"] = "34",
            ["Hưng Yên"] = "89",
            ["Quảng Ninh"] = "14",
            ["Bắc Ninh"] = "99",
            ["Bắc Giang"] = "98",
            ["Thái Nguyên"] = "20",
            ["Vĩnh Phúc"] = "88",
            ["Phú Thọ"] = "19",
            ["Hà Nam"] = "90",
            ["Ninh Bình"] = "35",

            // Tây Bắc Bộ
            ["Lào Cai"] = "24",
            ["Hà Giang"] = "23",
            ["Sơn La"] = "26",
            ["Mộc Châu"] = "26",
            ["Sa Pa"] = "24",
            ["Cao Bằng"] = "11",
            ["Lạng Sơn"] = "12",
            ["Tuyên Quang"] = "22",
            ["Yên Bái"] = "21",
            ["Bắc Kạn"] = "97",
            ["Hòa Bình"] = "28",
            ["Điện Biên"] = "27",
            ["Lai Châu"] = "25",

            // Bắc Trung Bộ
            ["Thanh Hóa"] = "36",
            ["Nghệ An"] = "37",
            ["Hà Tĩnh"] = "38",
            ["Quảng Bình"] = "73",
            ["Quảng Trị"] = "74",
            ["Thừa Thiên Huế"] = "75",
            ["Huế"] = "75",

            // Duyên hải Nam Trung Bộ & Đà Nẵng
            ["Đà Nẵng"] = "43",
            ["Quảng Nam"] = "92",
            ["Quảng Ngãi"] = "76",
            ["Bình Định"] = "77",
            ["Quy Nhơn"] = "77",
            ["Phú Yên"] = "78",
            ["Khánh Hòa"] = "79",
            ["Nha Trang"] = "79",
            ["Ninh Thuận"] = "85",
            ["Bình Thuận"] = "86",
            ["Phan Thiết"] = "86",

            // Tây Nguyên
            ["Lâm Đồng"] = "49",
            ["Đà Lạt"] = "49",
            ["Đắk Lắk"] = "47",
            ["Buôn Ma Thuột"] = "47",
            ["Gia Lai"] = "81",
            ["Pleiku"] = "81",
            ["Kon Tum"] = "82",
            ["Đắk Nông"] = "48",

            // Đông Nam Bộ
            ["TP. Hồ Chí Minh"] = "50,51,52,53,54,55,56,57,58,59,41",
            ["Sài Gòn"] = "50,51,52,53,54,55,56,57,58,59,41",
            ["Hồ Chí Minh"] = "50,51,52,53,54,55,56,57,58,59,41",
            ["Bà Rịa - Vũng Tàu"] = "72",
            ["Vũng Tàu"] = "72",
            ["Bình Dương"] = "61",
            ["Đồng Nai"] = "60,39",
            ["Tây Ninh"] = "70",
            ["Bình Phước"] = "93",

            // Tây Nam Bộ (Đồng bằng sông Cửu Long)
            ["Long An"] = "62",
            ["Tiền Giang"] = "63",
            ["Mỹ Tho"] = "63",
            ["Bến Tre"] = "71",
            ["Trà Vinh"] = "84",
            ["Vĩnh Long"] = "64",
            ["Đồng Tháp"] = "66",
            ["An Giang"] = "67",
            ["Châu Đốc"] = "67",
            ["Kiên Giang"] = "68",
            ["Rạch Giá"] = "68",
            ["Phú Quốc"] = "68",
            ["Cần Thơ"] = "65",
            ["Hậu Giang"] = "95",
            ["Sóc Trăng"] = "83",
            ["Bạc Liêu"] = "94",
            ["Cà Mau"] = "69"
        };

        // Tra cứu ngược từ mã 2 số sang tên tỉnh
        private static readonly Dictionary<string, string> CodeToProvince = new()
        {
            ["11"] = "Cao Bằng", ["12"] = "Lạng Sơn", ["14"] = "Quảng Ninh",
            ["15"] = "Hải Phòng", ["16"] = "Hải Phòng", ["17"] = "Thái Bình",
            ["18"] = "Nam Định", ["19"] = "Phú Thọ", ["20"] = "Thái Nguyên",
            ["21"] = "Yên Bái", ["22"] = "Tuyên Quang", ["23"] = "Hà Giang",
            ["24"] = "Lào Cai", ["25"] = "Lai Châu", ["26"] = "Sơn La",
            ["27"] = "Điện Biên", ["28"] = "Hòa Bình", ["29"] = "Hà Nội",
            ["30"] = "Hà Nội", ["31"] = "Hà Nội", ["32"] = "Hà Nội", ["33"] = "Hà Nội", ["40"] = "Hà Nội",
            ["34"] = "Hải Dương", ["35"] = "Ninh Bình", ["36"] = "Thanh Hóa",
            ["37"] = "Nghệ An", ["38"] = "Hà Tĩnh", ["73"] = "Quảng Bình",
            ["74"] = "Quảng Trị", ["75"] = "Thừa Thiên Huế", ["43"] = "Đà Nẵng",
            ["92"] = "Quảng Nam", ["76"] = "Quảng Ngãi", ["77"] = "Bình Định",
            ["78"] = "Phú Yên", ["79"] = "Khánh Hòa", ["85"] = "Ninh Thuận",
            ["86"] = "Bình Thuận", ["81"] = "Gia Lai", ["82"] = "Kon Tum",
            ["47"] = "Đắk Lắk", ["48"] = "Đắk Nông", ["49"] = "Lâm Đồng",
            ["50"] = "TP. Hồ Chí Minh", ["51"] = "TP. Hồ Chí Minh", ["52"] = "TP. Hồ Chí Minh",
            ["53"] = "TP. Hồ Chí Minh", ["54"] = "TP. Hồ Chí Minh", ["55"] = "TP. Hồ Chí Minh",
            ["56"] = "TP. Hồ Chí Minh", ["57"] = "TP. Hồ Chí Minh", ["58"] = "TP. Hồ Chí Minh",
            ["59"] = "TP. Hồ Chí Minh", ["41"] = "TP. Hồ Chí Minh",
            ["60"] = "Đồng Nai", ["39"] = "Đồng Nai", ["61"] = "Bình Dương",
            ["70"] = "Tây Ninh", ["72"] = "Bà Rịa - Vũng Tàu", ["93"] = "Bình Phước",
            ["62"] = "Long An", ["63"] = "Tiền Giang", ["71"] = "Bến Tre",
            ["84"] = "Trà Vinh", ["64"] = "Vĩnh Long", ["66"] = "Đồng Tháp",
            ["67"] = "An Giang", ["68"] = "Kiên Giang", ["65"] = "Cần Thơ",
            ["95"] = "Hậu Giang", ["83"] = "Sóc Trăng", ["94"] = "Bạc Liêu",
            ["69"] = "Cà Mau", ["88"] = "Vĩnh Phúc", ["89"] = "Hưng Yên",
            ["90"] = "Hà Nam", ["98"] = "Bắc Giang", ["99"] = "Bắc Ninh"
        };

        /// <summary>
        /// Lấy mã đầu biển số chính của tỉnh từ tên địa danh
        /// </summary>
        public static string? GetPrimaryCode(string? location)
        {
            if (string.IsNullOrWhiteSpace(location)) return null;

            foreach (var kvp in ProvinceToCodes)
            {
                if (location.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) || 
                    kvp.Key.Contains(location, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.Split(',')[0].Trim();
                }
            }

            // Thử bỏ dấu tiếng Việt để đối soát
            var normalizedLocation = LocationHelper.RemoveDiacritics(location);
            foreach (var kvp in ProvinceToCodes)
            {
                var normKey = LocationHelper.RemoveDiacritics(kvp.Key);
                if (normalizedLocation.Contains(normKey, StringComparison.OrdinalIgnoreCase) ||
                    normKey.Contains(normalizedLocation, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.Split(',')[0].Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Lấy tất cả các mã biển số hợp lệ của tỉnh (ví dụ Hà Nội có 29, 30,...)
        /// </summary>
        public static List<string> GetAllCodes(string? location)
        {
            if (string.IsNullOrWhiteSpace(location)) return new List<string>();

            foreach (var kvp in ProvinceToCodes)
            {
                if (location.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) || 
                    kvp.Key.Contains(location, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.Split(',').Select(c => c.Trim()).ToList();
                }
            }

            var normalizedLocation = LocationHelper.RemoveDiacritics(location);
            foreach (var kvp in ProvinceToCodes)
            {
                var normKey = LocationHelper.RemoveDiacritics(kvp.Key);
                if (normalizedLocation.Contains(normKey, StringComparison.OrdinalIgnoreCase) ||
                    normKey.Contains(normalizedLocation, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.Split(',').Select(c => c.Trim()).ToList();
                }
            }

            return new List<string>();
        }

        /// <summary>
        /// Nhận diện tên tỉnh từ biển số xe (ví dụ: '17B-012.34' -> 'Thái Bình')
        /// </summary>
        public static string GetProvinceName(string? licensePlate)
        {
            if (string.IsNullOrWhiteSpace(licensePlate)) return "Toàn quốc";

            var match = Regex.Match(licensePlate.Trim(), @"^(\d{2})");
            if (match.Success)
            {
                var code = match.Groups[1].Value;
                if (CodeToProvince.TryGetValue(code, out var province))
                {
                    return province;
                }
            }

            return "Toàn quốc";
        }

        /// <summary>
        /// Lọc danh sách xe thông minh từ DB theo tuyến:
        /// - Chuyến 1: Ưu tiên xe biển tỉnh đi (From)
        /// - Chuyến 2: Ưu tiên xe biển tỉnh đến (To)
        /// - Chuyến 3: Xe biển tỉnh lân cận hoặc tỉnh đi/đến khác
        /// - Chuyến 4: Xe biển đầu mối vận tải lớn (Hà Nội, Sài Gòn, Đà Nẵng...)
        /// <summary>
        /// Lọc danh sách xe thật từ Database theo tuyến:
        /// - Chuyến 1 (07:30): Xe 29 chỗ (Ưu tiên biển tỉnh đi)
        /// - Chuyến 2 (11:00): Xe 45 chỗ (Ưu tiên biển tỉnh đến)
        /// - Chuyến 3 (14:30): Limousine VIP 22 Phòng (Ưu tiên biển tỉnh đi)
        /// - Chuyến 4 (19:00): Giường Nằm 34 Phòng (Ưu tiên biển tỉnh đến hoặc đầu mối)
        /// Tất cả xe đều được lấy từ bảng Bus trong CSDL SQL Server, đồng nhất 100% giữa tất cả người dùng
        /// </summary>
        public static List<Bus> SelectDiverseBusesForRoute(List<Bus> allBuses, string from, string to, int targetCount = 4)
        {
            if (allBuses == null || !allBuses.Any()) return new List<Bus>();

            var selected = new List<Bus>();
            var usedIds = new HashSet<int>();

            var fromCodes = GetAllCodes(from);
            var toCodes = GetAllCodes(to);
            var hubCodes = new[] { "29", "30", "51", "50", "15", "43", "18", "34", "36", "37" };

            // 4 dòng xe tiêu chuẩn tương ứng 4 khung giờ trong ngày:
            // Khung 1 (07:30): Xe 29 chỗ (BusTypeId 2)
            // Khung 2 (11:00): Xe 45 chỗ (BusTypeId 3)
            // Khung 3 (14:30): Limousine VIP 22 Phòng (BusTypeId 4)
            // Khung 4 (19:00): Giường Nằm 34 Phòng (BusTypeId 5)
            var targetTypes = new[] { 2, 3, 4, 5 };

            for (int i = 0; i < targetTypes.Length && selected.Count < targetCount; i++)
            {
                int typeId = targetTypes[i];
                var preferredCodes = (i % 2 == 0) ? fromCodes.Concat(toCodes).ToList() : toCodes.Concat(fromCodes).ToList();

                // Ưu tiên 1: Xe đúng loại thuộc tỉnh đi hoặc tỉnh đến
                var bus = allBuses.FirstOrDefault(b => !usedIds.Contains(b.BusId) 
                    && b.BusTypeId == typeId 
                    && preferredCodes.Any(c => b.LicensePlate.StartsWith(c)));

                // Ưu tiên 2: Xe đúng loại thuộc các tỉnh trung tâm kết nối
                if (bus == null)
                {
                    bus = allBuses.FirstOrDefault(b => !usedIds.Contains(b.BusId) 
                        && b.BusTypeId == typeId 
                        && hubCodes.Any(c => b.LicensePlate.StartsWith(c)));
                }

                // Ưu tiên 3: Xe bất kỳ đúng loại trong DB
                if (bus == null)
                {
                    bus = allBuses.FirstOrDefault(b => !usedIds.Contains(b.BusId) && b.BusTypeId == typeId);
                }

                // Ưu tiên 4: Xe bất kỳ còn lại trong DB
                if (bus == null)
                {
                    bus = allBuses.FirstOrDefault(b => !usedIds.Contains(b.BusId));
                }

                if (bus != null)
                {
                    selected.Add(bus);
                    usedIds.Add(bus.BusId);
                }
            }

            return selected;
        }

        /// <summary>
        /// Danh sách 4 biển số cố định chuẩn theo tỉnh đi và đến, khớp 100% với dữ liệu trong bảng Bus của Database
        /// </summary>
        public static string[] GenerateDiverseLicensePlates(string from, string to)
        {
            var fromCode = GetPrimaryCode(from) ?? "29";
            var toCode = GetPrimaryCode(to) ?? "15";

            // Nếu from và to trùng mã, đổi toCode sang mã khác
            if (fromCode == toCode)
            {
                toCode = fromCode == "29" ? "15" : "29";
            }

            var hubCode = (fromCode != "29" && toCode != "29") ? "29" : (fromCode != "51" && toCode != "51" ? "51" : "43");

            return new[]
            {
                $"{fromCode}B-012.34",  // Chuyến 1: Xe 29 chỗ (Tỉnh đi)
                $"{toCode}B-025.79",    // Chuyến 2: Xe 45 chỗ (Tỉnh đến)
                $"{fromCode}B-068.86",  // Chuyến 3: Xe Limousine VIP 22 Phòng (Tỉnh đi)
                $"{hubCode}B-088.99"    // Chuyến 4: Xe Giường Nằm 34 Phòng (Đầu mối trung tâm)
            };
        }

        /// <summary>
        /// Khởi tạo và lưu cố định vào bảng Bus toàn bộ danh mục xe chuẩn của 63 tỉnh thành Việt Nam
        /// Đảm bảo mỗi tỉnh thành đều có sẵn 12 xe đa dạng trong SQL Server (3 xe 29 chỗ, 3 xe 45 chỗ, 3 Limousine VIP, 3 Giường nằm)
        /// </summary>
        public static async Task EnsureAllProvincesBusesSeededAsync(WebApplication1.Data.ApplicationDbContext context)
        {
            try
            {
                var existingPlates = new HashSet<string>(
                    await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                        System.Linq.Queryable.Select(context.Buses, b => b.LicensePlate)
                    ),
                    StringComparer.OrdinalIgnoreCase
                );

                var newBuses = new List<Bus>();
                var distinctCodes = CodeToProvince.Keys.Distinct().ToList();

                var busTemplates = new (string suffix, int busTypeId, int capacity)[]
                {
                    // Xe 29 chỗ (BusTypeId: 2, Capacity: 29)
                    ("B-012.34", 2, 29),
                    ("B-015.68", 2, 29),
                    ("F-016.78", 2, 29),
                    // Xe 45 chỗ (BusTypeId: 3, Capacity: 45)
                    ("B-025.79", 3, 45),
                    ("B-036.88", 3, 45),
                    ("F-045.67", 3, 45),
                    // Limousine VIP 22 Phòng (BusTypeId: 4, Capacity: 22)
                    ("B-068.86", 4, 22),
                    ("B-079.97", 4, 22),
                    ("F-066.88", 4, 22),
                    // Giường Nằm 34 Phòng (BusTypeId: 5, Capacity: 34)
                    ("B-088.99", 5, 34),
                    ("B-099.88", 5, 34),
                    ("F-089.98", 5, 34)
                };

                foreach (var code in distinctCodes)
                {
                    foreach (var (suffix, busTypeId, capacity) in busTemplates)
                    {
                        var plate = $"{code}{suffix}";
                        if (!existingPlates.Contains(plate))
                        {
                            newBuses.Add(new Bus { BusTypeId = busTypeId, LicensePlate = plate, Capacity = capacity, Status = "Active" });
                            existingPlates.Add(plate);
                        }
                    }
                }

                if (newBuses.Any())
                {
                    context.Buses.AddRange(newBuses);
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
                // Bỏ qua nếu có xung đột kết nối
            }
        }
    }
}
