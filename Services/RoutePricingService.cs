using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Services
{
    /// <summary>
    /// Thông tin số đo hành trình: Khoảng cách (km), Thời gian ước tính (phút), Tên tuyến chuẩn
    /// </summary>
    public class RouteMetrics
    {
        public decimal Distance { get; set; } = 100m;
        public int EstimatedMinutes { get; set; } = 120;
        public string RouteName { get; set; } = string.Empty;
        public bool IsMountainous { get; set; } = false;
        public bool HasExpressway { get; set; } = false;
    }

    /// <summary>
    /// Dịch vụ tính toán khoảng cách và giá vé thị trường thực tế cho toàn bộ tuyến xe buýt / xe khách Việt Nam
    /// Tích hợp dữ liệu khảo sát giá vé từ các nền tảng xe khách lớn (Vexere, Futa Bus Lines, Phương Trang, Hoàng Long...)
    /// </summary>
    public static class RoutePricingService
    {
        // -----------------------------------------------------------------------------------------
        // 1. TỪ ĐIỂN CÁC TUYẾN ĐƯỜNG PHỔ BIẾN THỰC TẾ (KM & SỐ PHÚT DI CHUYỂN CHUẨN XÁC)
        // -----------------------------------------------------------------------------------------
        private static readonly Dictionary<string, (decimal Distance, int Minutes, bool IsMountain)> KnownRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            // Tuyến cự ly ngắn & liên tỉnh lân cận
            ["Thái Bình-Ninh Bình"] = (55m, 75, false),
            ["Ninh Bình-Thái Bình"] = (55m, 75, false),
            ["Thái Bình-Nam Định"] = (25m, 35, false),
            ["Nam Định-Thái Bình"] = (25m, 35, false),
            ["Nam Định-Ninh Bình"] = (32m, 45, false),
            ["Ninh Bình-Nam Định"] = (32m, 45, false),
            ["Thái Bình-Hải Phòng"] = (70m, 85, false),
            ["Hải Phòng-Thái Bình"] = (70m, 85, false),
            ["Hà Nội-Thái Bình"] = (105m, 110, false),
            ["Thái Bình-Hà Nội"] = (105m, 110, false),
            ["Hà Nội-Hải Dương"] = (60m, 75, false),
            ["Hải Dương-Hà Nội"] = (60m, 75, false),
            ["Hà Nội-Hưng Yên"] = (50m, 65, false),
            ["Hưng Yên-Hà Nội"] = (50m, 65, false),
            ["Hà Nội-Bắc Ninh"] = (35m, 45, false),
            ["Bắc Ninh-Hà Nội"] = (35m, 45, false),
            ["Hà Nội-Bắc Giang"] = (60m, 75, false),
            ["Bắc Giang-Hà Nội"] = (60m, 75, false),
            ["Hà Nội-Vĩnh Phúc"] = (55m, 65, false),
            ["Vĩnh Phúc-Hà Nội"] = (55m, 65, false),
            ["Hà Nội-Phú Thọ"] = (85m, 100, false),
            ["Phú Thọ-Hà Nội"] = (85m, 100, false),
            ["Hà Nội-Nam Định"] = (90m, 95, false),
            ["Nam Định-Hà Nội"] = (90m, 95, false),
            ["Hà Nội-Ninh Bình"] = (95m, 100, false),
            ["Ninh Bình-Hà Nội"] = (95m, 100, false),
            ["Hà Nội-Hải Phòng"] = (105m, 90, false),
            ["Hải Phòng-Hà Nội"] = (105m, 90, false),

            // Tuyến Đông Bắc & Tây Bắc (Có đèo núi)
            ["Hà Nội-Thái Nguyên"] = (80m, 90, false),
            ["Thái Nguyên-Hà Nội"] = (80m, 90, false),
            ["Thái Nguyên-Cao Bằng"] = (135m, 180, true),
            ["Cao Bằng-Thái Nguyên"] = (135m, 180, true),
            ["Thái Nguyên-Bắc Kạn"] = (75m, 100, true),
            ["Bắc Kạn-Thái Nguyên"] = (75m, 100, true),
            ["Bắc Kạn-Cao Bằng"] = (115m, 160, true),
            ["Cao Bằng-Bắc Kạn"] = (115m, 160, true),
            ["Hà Nội-Cao Bằng"] = (280m, 330, true),
            ["Cao Bằng-Hà Nội"] = (280m, 330, true),
            ["Hà Nội-Lạng Sơn"] = (155m, 140, false),
            ["Lạng Sơn-Hà Nội"] = (155m, 140, false),
            ["Hà Nội-Bắc Kạn"] = (160m, 180, true),
            ["Bắc Kạn-Hà Nội"] = (160m, 180, true),
            ["Hà Nội-Hạ Long"] = (160m, 130, false),
            ["Hạ Long-Hà Nội"] = (160m, 130, false),
            ["Hải Phòng-Hạ Long"] = (55m, 50, false),
            ["Hạ Long-Hải Phòng"] = (55m, 50, false),
            ["Hà Nội-Hà Giang"] = (300m, 390, true),
            ["Hà Giang-Hà Nội"] = (300m, 390, true),
            ["Hà Nội-Tuyên Quang"] = (135m, 150, false),
            ["Tuyên Quang-Hà Nội"] = (135m, 150, false),
            ["Hà Nội-Yên Bái"] = (160m, 150, false),
            ["Yên Bái-Hà Nội"] = (160m, 150, false),
            ["Hà Nội-Lào Cai"] = (290m, 250, false),
            ["Lào Cai-Hà Nội"] = (290m, 250, false),
            ["Hà Nội-Sa Pa"] = (320m, 300, true),
            ["Sa Pa-Hà Nội"] = (320m, 300, true),
            ["Hà Nội-Hòa Bình"] = (75m, 90, false),
            ["Hòa Bình-Hà Nội"] = (75m, 90, false),
            ["Hà Nội-Mộc Châu"] = (205m, 250, true),
            ["Mộc Châu-Hà Nội"] = (205m, 250, true),
            ["Hà Nội-Sơn La"] = (300m, 360, true),
            ["Sơn La-Hà Nội"] = (300m, 360, true),
            ["Hà Nội-Điện Biên"] = (460m, 540, true),
            ["Điện Biên-Hà Nội"] = (460m, 540, true),
            ["Hà Nội-Lai Châu"] = (400m, 450, true),
            ["Lai Châu-Hà Nội"] = (400m, 450, true),

            // Tuyến Bắc Trung Bộ & Miền Trung
            ["Hà Nội-Thanh Hóa"] = (160m, 140, false),
            ["Thanh Hóa-Hà Nội"] = (160m, 140, false),
            ["Hà Nội-Nghệ An"] = (300m, 260, false),
            ["Nghệ An-Hà Nội"] = (300m, 260, false),
            ["Hà Nội-Hà Tĩnh"] = (350m, 320, false),
            ["Hà Tĩnh-Hà Nội"] = (350m, 320, false),
            ["Hà Nội-Quảng Bình"] = (500m, 510, false),
            ["Quảng Bình-Hà Nội"] = (500m, 510, false),
            ["Hà Nội-Quảng Trị"] = (600m, 600, false),
            ["Quảng Trị-Hà Nội"] = (600m, 600, false),
            ["Hà Nội-Huế"] = (680m, 660, false),
            ["Huế-Hà Nội"] = (680m, 660, false),
            ["Hà Nội-Đà Nẵng"] = (760m, 720, false),
            ["Đà Nẵng-Hà Nội"] = (760m, 720, false),
            ["Đà Nẵng-Huế"] = (100m, 110, false),
            ["Huế-Đà Nẵng"] = (100m, 110, false),
            ["Đà Nẵng-Hội An"] = (30m, 45, false),
            ["Hội An-Đà Nẵng"] = (30m, 45, false),
            ["Đà Nẵng-Quảng Ngãi"] = (140m, 130, false),
            ["Quảng Ngãi-Đà Nẵng"] = (140m, 130, false),
            ["Đà Nẵng-Quy Nhơn"] = (310m, 330, false),
            ["Quy Nhơn-Đà Nẵng"] = (310m, 330, false),
            ["Đà Nẵng-Phú Yên"] = (410m, 420, false),
            ["Phú Yên-Đà Nẵng"] = (410m, 420, false),
            ["Đà Nẵng-Nha Trang"] = (530m, 540, false),
            ["Nha Trang-Đà Nẵng"] = (530m, 540, false),
            ["Đà Nẵng-Buôn Ma Thuột"] = (530m, 600, true),
            ["Buôn Ma Thuột-Đà Nẵng"] = (530m, 600, true),

            // Tuyến Tây Nguyên & Nam Trung Bộ
            ["Đà Lạt-Nha Trang"] = (135m, 180, true),
            ["Nha Trang-Đà Lạt"] = (135m, 180, true),
            ["Đà Lạt-Phan Thiết"] = (160m, 200, true),
            ["Phan Thiết-Đà Lạt"] = (160m, 200, true),
            ["Đà Lạt-Buôn Ma Thuột"] = (210m, 270, true),
            ["Buôn Ma Thuột-Đà Lạt"] = (210m, 270, true),
            ["Quy Nhơn-Pleiku"] = (170m, 220, true),
            ["Pleiku-Quy Nhơn"] = (170m, 220, true),
            ["Nha Trang-Phan Thiết"] = (220m, 210, false),
            ["Phan Thiết-Nha Trang"] = (220m, 210, false),

            // Tuyến TP. Hồ Chí Minh (Sài Gòn) & Đông Nam Bộ
            ["TP. Hồ Chí Minh-Vũng Tàu"] = (100m, 115, false),
            ["Vũng Tàu-TP. Hồ Chí Minh"] = (100m, 115, false),
            ["TP. Hồ Chí Minh-Bình Dương"] = (35m, 50, false),
            ["Bình Dương-TP. Hồ Chí Minh"] = (35m, 50, false),
            ["TP. Hồ Chí Minh-Đồng Nai"] = (35m, 45, false),
            ["Đồng Nai-TP. Hồ Chí Minh"] = (35m, 45, false),
            ["TP. Hồ Chí Minh-Tây Ninh"] = (95m, 120, false),
            ["Tây Ninh-TP. Hồ Chí Minh"] = (95m, 120, false),
            ["TP. Hồ Chí Minh-Bình Phước"] = (120m, 150, false),
            ["Bình Phước-TP. Hồ Chí Minh"] = (120m, 150, false),
            ["TP. Hồ Chí Minh-Phan Thiết"] = (200m, 160, false),
            ["Phan Thiết-TP. Hồ Chí Minh"] = (200m, 160, false),
            ["TP. Hồ Chí Minh-Đà Lạt"] = (305m, 360, true),
            ["Đà Lạt-TP. Hồ Chí Minh"] = (305m, 360, true),
            ["TP. Hồ Chí Minh-Nha Trang"] = (430m, 390, false),
            ["Nha Trang-TP. Hồ Chí Minh"] = (430m, 390, false),
            ["TP. Hồ Chí Minh-Buôn Ma Thuột"] = (350m, 420, true),
            ["Buôn Ma Thuột-TP. Hồ Chí Minh"] = (350m, 420, true),
            ["TP. Hồ Chí Minh-Quy Nhơn"] = (650m, 660, false),
            ["Quy Nhơn-TP. Hồ Chí Minh"] = (650m, 660, false),
            ["TP. Hồ Chí Minh-Hà Nội"] = (1720m, 1920, false),
            ["Hà Nội-TP. Hồ Chí Minh"] = (1720m, 1920, false),

            // Tuyến Đồng Bằng Sông Cửu Long (Miền Tây)
            ["TP. Hồ Chí Minh-Long An"] = (45m, 55, false),
            ["Long An-TP. Hồ Chí Minh"] = (45m, 55, false),
            ["TP. Hồ Chí Minh-Tiền Giang"] = (70m, 80, false),
            ["Tiền Giang-TP. Hồ Chí Minh"] = (70m, 80, false),
            ["TP. Hồ Chí Minh-Bến Tre"] = (85m, 105, false),
            ["Bến Tre-TP. Hồ Chí Minh"] = (85m, 105, false),
            ["TP. Hồ Chí Minh-Vĩnh Long"] = (135m, 150, false),
            ["Vĩnh Long-TP. Hồ Chí Minh"] = (135m, 150, false),
            ["TP. Hồ Chí Minh-Trà Vinh"] = (130m, 160, false),
            ["Trà Vinh-TP. Hồ Chí Minh"] = (130m, 160, false),
            ["TP. Hồ Chí Minh-Đồng Tháp"] = (145m, 170, false),
            ["Đồng Tháp-TP. Hồ Chí Minh"] = (145m, 170, false),
            ["TP. Hồ Chí Minh-Cần Thơ"] = (165m, 175, false),
            ["Cần Thơ-TP. Hồ Chí Minh"] = (165m, 175, false),
            ["TP. Hồ Chí Minh-Hậu Giang"] = (200m, 220, false),
            ["Hậu Giang-TP. Hồ Chí Minh"] = (200m, 220, false),
            ["TP. Hồ Chí Minh-Sóc Trăng"] = (220m, 250, false),
            ["Sóc Trăng-TP. Hồ Chí Minh"] = (220m, 250, false),
            ["TP. Hồ Chí Minh-An Giang"] = (240m, 330, false),
            ["An Giang-TP. Hồ Chí Minh"] = (240m, 330, false),
            ["TP. Hồ Chí Minh-Châu Đốc"] = (240m, 330, false),
            ["Châu Đốc-TP. Hồ Chí Minh"] = (240m, 330, false),
            ["TP. Hồ Chí Minh-Kiên Giang"] = (250m, 330, false),
            ["Kiên Giang-TP. Hồ Chí Minh"] = (250m, 330, false),
            ["TP. Hồ Chí Minh-Rạch Giá"] = (250m, 330, false),
            ["Rạch Giá-TP. Hồ Chí Minh"] = (250m, 330, false),
            ["TP. Hồ Chí Minh-Bạc Liêu"] = (270m, 330, false),
            ["Bạc Liêu-TP. Hồ Chí Minh"] = (270m, 330, false),
            ["TP. Hồ Chí Minh-Cà Mau"] = (305m, 400, false),
            ["Cà Mau-TP. Hồ Chí Minh"] = (305m, 400, false),
            ["Cần Thơ-Rạch Giá"] = (110m, 135, false),
            ["Rạch Giá-Cần Thơ"] = (110m, 135, false),
            ["Cần Thơ-Cà Mau"] = (150m, 180, false),
            ["Cà Mau-Cần Thơ"] = (150m, 180, false),
            ["Cần Thơ-Châu Đốc"] = (120m, 160, false),
            ["Châu Đốc-Cần Thơ"] = (120m, 160, false)
        };

        // -----------------------------------------------------------------------------------------
        // 2. TOẠ ĐỘ GPS TỈNH THÀNH VIỆT NAM (DÙNG ĐỂ TÍNH TOÁN BẤT KỲ CẶP ĐỊA ĐIỂM NÀO CHƯA CÓ TRONG TỪ ĐIỂN)
        // -----------------------------------------------------------------------------------------
        private static readonly Dictionary<string, (double Lat, double Lon)> ProvinceCoordinates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Hà Nội"] = (21.0285, 105.8542),
            ["Hải Phòng"] = (20.8449, 106.6881),
            ["Thái Bình"] = (20.4463, 106.3366),
            ["Nam Định"] = (20.4200, 106.1683),
            ["Ninh Bình"] = (20.2506, 105.9745),
            ["Hải Dương"] = (20.9373, 106.3146),
            ["Hưng Yên"] = (20.6464, 106.0511),
            ["Bắc Ninh"] = (21.1861, 106.0763),
            ["Bắc Giang"] = (21.2731, 106.1946),
            ["Quảng Ninh"] = (20.9505, 107.0734),
            ["Hạ Long"] = (20.9505, 107.0734),
            ["Thái Nguyên"] = (21.5942, 105.8442),
            ["Vĩnh Phúc"] = (21.3089, 105.6049),
            ["Phú Thọ"] = (21.3228, 105.2280),
            ["Hà Nam"] = (20.5452, 105.9126),
            ["Lào Cai"] = (22.4856, 103.9707),
            ["Sa Pa"] = (22.3364, 103.8438),
            ["Hà Giang"] = (22.8233, 104.9839),
            ["Cao Bằng"] = (22.6667, 106.2625),
            ["Lạng Sơn"] = (21.8537, 106.7615),
            ["Bắc Kạn"] = (22.1470, 105.8348),
            ["Tuyên Quang"] = (21.8236, 105.2180),
            ["Yên Bái"] = (21.7168, 104.9113),
            ["Sơn La"] = (21.3283, 103.9144),
            ["Mộc Châu"] = (20.8436, 104.6473),
            ["Hòa Bình"] = (20.8136, 105.3383),
            ["Điện Biên"] = (21.3853, 103.0209),
            ["Lai Châu"] = (22.3964, 103.4582),
            ["Thanh Hóa"] = (19.8067, 105.7852),
            ["Nghệ An"] = (18.6796, 105.6813),
            ["Hà Tĩnh"] = (18.3560, 105.9057),
            ["Quảng Bình"] = (17.4690, 106.6222),
            ["Quảng Trị"] = (16.7500, 107.1855),
            ["Thừa Thiên Huế"] = (16.4637, 107.5909),
            ["Huế"] = (16.4637, 107.5909),
            ["Đà Nẵng"] = (16.0544, 108.2022),
            ["Quảng Nam"] = (15.5658, 108.4800),
            ["Hội An"] = (15.8801, 108.3380),
            ["Quảng Ngãi"] = (15.1205, 108.7923),
            ["Bình Định"] = (13.7830, 109.2197),
            ["Quy Nhơn"] = (13.7830, 109.2197),
            ["Phú Yên"] = (13.0882, 109.3074),
            ["Khánh Hòa"] = (12.2388, 109.1967),
            ["Nha Trang"] = (12.2388, 109.1967),
            ["Ninh Thuận"] = (11.5644, 108.9882),
            ["Bình Thuận"] = (10.9273, 108.1018),
            ["Phan Thiết"] = (10.9273, 108.1018),
            ["Kon Tum"] = (14.3541, 108.0076),
            ["Gia Lai"] = (13.9833, 108.0000),
            ["Pleiku"] = (13.9833, 108.0000),
            ["Đắk Lắk"] = (12.6667, 108.0500),
            ["Buôn Ma Thuột"] = (12.6667, 108.0500),
            ["Đắk Nông"] = (12.0000, 107.6833),
            ["Lâm Đồng"] = (11.9404, 108.4583),
            ["Đà Lạt"] = (11.9404, 108.4583),
            ["TP. Hồ Chí Minh"] = (10.8231, 106.6297),
            ["Sài Gòn"] = (10.8231, 106.6297),
            ["Bà Rịa - Vũng Tàu"] = (10.3460, 107.0843),
            ["Vũng Tàu"] = (10.3460, 107.0843),
            ["Bình Dương"] = (11.1604, 106.6568),
            ["Đồng Nai"] = (10.9460, 106.8242),
            ["Tây Ninh"] = (11.3344, 106.1265),
            ["Bình Phước"] = (11.7512, 106.9068),
            ["Long An"] = (10.5333, 106.4167),
            ["Tiền Giang"] = (10.3600, 106.3600),
            ["Mỹ Tho"] = (10.3600, 106.3600),
            ["Bến Tre"] = (10.2433, 106.3756),
            ["Trà Vinh"] = (9.9347, 106.3455),
            ["Vĩnh Long"] = (10.2537, 105.9722),
            ["Đồng Tháp"] = (10.4578, 105.6331),
            ["An Giang"] = (10.5216, 105.1259),
            ["Châu Đốc"] = (10.7042, 105.1189),
            ["Kiên Giang"] = (10.0125, 105.0809),
            ["Rạch Giá"] = (10.0125, 105.0809),
            ["Cần Thơ"] = (10.0452, 105.7469),
            ["Hậu Giang"] = (9.7844, 105.4701),
            ["Sóc Trăng"] = (9.6033, 105.9745),
            ["Bạc Liêu"] = (9.2941, 105.7278),
            ["Cà Mau"] = (9.1769, 105.1524)
        };

        /// <summary>
        /// Chuẩn hóa địa danh thành tên tỉnh thành chính
        /// </summary>
        public static string ResolveProvinceName(string? location)
        {
            if (string.IsNullOrWhiteSpace(location)) return "Hà Nội";
            var clean = location.Trim();

            // Đối soát với danh sách toạ độ
            foreach (var key in ProvinceCoordinates.Keys)
            {
                if (clean.Contains(key, StringComparison.OrdinalIgnoreCase) || 
                    key.Contains(clean, StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }

            // Đối soát LocationHelper
            var aliases = LocationHelper.GetLocationAliases(clean);
            foreach (var a in aliases)
            {
                foreach (var key in ProvinceCoordinates.Keys)
                {
                    if (a.Contains(key, StringComparison.OrdinalIgnoreCase) || 
                        key.Contains(a, StringComparison.OrdinalIgnoreCase))
                    {
                        return key;
                    }
                }
            }

            return clean;
        }

        /// <summary>
        /// Lấy khoảng cách (km) và số phút di chuyển thực tế giữa 2 điểm
        /// </summary>
        public static RouteMetrics GetRouteMetrics(string? from, string? to)
        {
            var pFrom = ResolveProvinceName(from);
            var pTo = ResolveProvinceName(to);

            var result = new RouteMetrics
            {
                RouteName = $"{pFrom} - {pTo}"
            };

            // 1. Kiểm tra cặp tuyến đã biết (Known Routes)
            var key1 = $"{pFrom}-{pTo}";
            var key2 = $"{pTo}-{pFrom}";

            if (KnownRoutes.TryGetValue(key1, out var val))
            {
                result.Distance = val.Distance;
                result.EstimatedMinutes = val.Minutes;
                result.IsMountainous = val.IsMountain;
                return result;
            }

            if (KnownRoutes.TryGetValue(key2, out var valRev))
            {
                result.Distance = valRev.Distance;
                result.EstimatedMinutes = valRev.Minutes;
                result.IsMountainous = valRev.IsMountain;
                return result;
            }

            // 2. Nếu chưa có trong từ điển, tính toán theo toạ độ địa lý (Haversine Formula)
            if (ProvinceCoordinates.TryGetValue(pFrom, out var coordFrom) &&
                ProvinceCoordinates.TryGetValue(pTo, out var coordTo))
            {
                var straightDistance = CalculateHaversine(coordFrom.Lat, coordFrom.Lon, coordTo.Lat, coordTo.Lon);
                
                // Nhận diện vùng đồi núi phía Bắc / Tây Nguyên
                bool isMountain = IsMountainProvince(pFrom) || IsMountainProvince(pTo);
                
                // Hệ số uốn khúc đường bộ Việt Nam (Detour Factor) bao quát toàn bộ 331.212 km² lãnh thổ
                // Do hình dáng chữ S uốn lượn ven biển, các tuyến đường bộ dài Bắc - Nam có hệ số uốn khúc cao hơn
                double detour;
                if (straightDistance > 850)
                {
                    detour = 1.52; // Tuyến dài Bắc - Nam ôm theo bờ biển hình chữ S
                }
                else if (straightDistance > 350)
                {
                    detour = isMountain ? 1.45 : 1.36;
                }
                else
                {
                    detour = isMountain ? 1.42 : 1.28;
                }

                decimal roadDistance = Math.Max(25m, Math.Round((decimal)(straightDistance * detour), 0));
                
                result.Distance = roadDistance;
                result.IsMountainous = isMountain;

                // Tính thời gian di chuyển thực tế (vận tốc trung bình của xe khách kèm dừng nghỉ)
                double avgSpeed = isMountain ? 42.0 : (roadDistance > 800 ? 54.0 : (roadDistance > 120 ? 62.0 : 50.0));
                int minutes = (int)Math.Round((double)roadDistance / avgSpeed * 60.0);

                // Thời gian dừng đón trả khách và các bữa ăn/nghỉ trạm theo cự ly
                if (roadDistance > 1200) minutes += 240; // Nghỉ 4 tiếng (3-4 bữa ăn, đổi 3-4 tài xế chạy luân phiên)
                else if (roadDistance > 600) minutes += 120; // Nghỉ 2 tiếng ăn trưa/tối
                else if (roadDistance > 250) minutes += 45;
                else if (roadDistance > 80) minutes += 20;
                else minutes += 10;

                result.EstimatedMinutes = minutes;
                return result;
            }

            // Fallback cự ly mặc định an toàn
            result.Distance = 90m;
            result.EstimatedMinutes = 110;
            return result;
        }

        private static bool IsMountainProvince(string province)
        {
            var p = province.ToLower();
            return p.Contains("cao bằng") || p.Contains("hà giang") || p.Contains("lào cai") || 
                   p.Contains("sa pa") || p.Contains("sơn la") || p.Contains("mộc châu") || 
                   p.Contains("điện biên") || p.Contains("lai châu") || p.Contains("bắc kạn") || 
                   p.Contains("đà lạt") || p.Contains("lâm đồng") || p.Contains("kon tum") || 
                   p.Contains("đắk");
        }

        private static double CalculateHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Bán kính trái đất (km)
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        // -----------------------------------------------------------------------------------------
        // 3. TÍNH TOÁN GIÁ VÉ THỊ TRƯỜNG THỰC TẾ (REALISTIC MARKET BUS PRICING)
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Tính giá vé cơ bản (Base price) theo khoảng cách km dựa trên dữ liệu giá thị trường xe khách Việt Nam
        /// Bao quát toàn bộ 331.212 km² từ cự ly siêu ngắn đến cự ly cực đại xuyên Việt (2.400 km)
        /// </summary>
        public static decimal GetMarketBasePrice(decimal distance)
        {
            if (distance <= 35) return 50000m;
            if (distance <= 65) return 65000m;   // Tuyến ngắn: Thái Bình - Ninh Bình 55km: ~65.000đ
            if (distance <= 95) return 80000m;   // Tuyến Thái Bình - Hải Phòng 70km, Hà Nội - Thái Nguyên 80km
            if (distance <= 125) return 105000m; // Tuyến Hà Nội - Hải Phòng 105km, TP.HCM - Vũng Tàu 100km
            if (distance <= 170) return 135000m; // Tuyến Thái Nguyên - Cao Bằng 135km, Hà Nội - Hạ Long 160km
            if (distance <= 230) return 175000m; // Tuyến Hà Nội - Mộc Châu 205km, TP.HCM - Phan Thiết 200km
            if (distance <= 320) return 240000m; // Tuyến Hà Nội - Cao Bằng 280km, Hà Nội - Sa Pa 320km, TP.HCM - Đà Lạt 305km
            if (distance <= 450) return 320000m; // Tuyến TP.HCM - Nha Trang 430km, TP.HCM - Buôn Ma Thuột 350km
            if (distance <= 650) return 420000m; // Tuyến Hà Nội - Quảng Bình 500km, TP.HCM - Quy Nhơn 650km
            if (distance <= 850) return 500000m; // Tuyến Hà Nội - Huế 680km, Hà Nội - Đà Nẵng 760km
            if (distance <= 1150) return 640000m; // Tuyến Hà Nội - Quy Nhơn (~1050km)
            if (distance <= 1450) return 780000m; // Tuyến Hà Nội - Nha Trang / Buôn Ma Thuột (~1300km)
            if (distance <= 1800) return 920000m; // Tuyến Hà Nội - TP.HCM (~1720km)
            if (distance <= 2150) return 1050000m; // Tuyến Hà Nội - Cần Thơ / An Giang (~1950 - 2100km)
            
            // Cự ly cực đại xuyên Việt (Hà Giang / Cao Bằng / Móng Cái -> Cà Mau / Kiên Giang 2150 - 2450km)
            var extraKm = distance - 2150m;
            var extraAmount = Math.Round((extraKm * 220m) / 5000m, MidpointRounding.AwayFromZero) * 5000m;
            return 1050000m + extraAmount;
        }

        /// <summary>
        /// Tính giá vé chính xác cho một chuyến xe cụ thể dựa trên Khoảng cách, Loại xe, Sức chứa và Khung giờ
        /// </summary>
        /// <param name="distance">Khoảng cách km của tuyến</param>
        /// <param name="busTypeName">Tên loại xe (45 chỗ, 29 chỗ, giường nằm, Limousine VIP...)</param>
        /// <param name="capacity">Số chỗ ngồi</param>
        /// <param name="slotIndex">Thứ tự chuyến trong ngày (0: Sáng sớm, 1: Trưa, 2: Chiều, 3: Tối)</param>
        /// <returns>Mức giá vé chuẩn xác bằng VNĐ (đã làm tròn đẹp đến 5.000đ)</returns>
        public static decimal CalculateTripPrice(decimal distance, string? busTypeName, int capacity = 29, int slotIndex = 0)
        {
            var basePrice = GetMarketBasePrice(distance);
            var name = (busTypeName ?? string.Empty).ToLower();

            // 1. Xác định hệ số nhân theo Loại xe (Bus Type Multiplier)
            decimal typeMultiplier = 1.0m;

            if (name.Contains("limousine") || name.Contains("cung điện") || name.Contains("vip") || name.Contains("royal"))
            {
                // Limousine VIP: Cự ly ngắn thì phụ thu vừa phải, cự ly dài có phòng cung điện riêng
                typeMultiplier = distance <= 80 ? 1.45m : 1.70m;
            }
            else if (name.Contains("giường") || name.Contains("sleeper") || name.Contains("phòng nằm"))
            {
                // Giường nằm 2 tầng
                typeMultiplier = distance <= 80 ? 1.20m : 1.30m;
            }
            else if (capacity >= 40 || name.Contains("45"))
            {
                // Xe ghế ngồi 45 chỗ: Giá rẻ nhất, kinh tế phổ thông
                typeMultiplier = 0.90m;
            }
            else if (capacity <= 30 || name.Contains("29") || name.Contains("eco"))
            {
                // Xe ghế ngồi 29 chỗ: Giá tiêu chuẩn
                typeMultiplier = 1.05m;
            }

            // 2. Xác định biến thiên theo từng chuyến trong ngày (Chuyến sáng Flash sale, Chuyến tối/VIP)
            decimal slotMultiplier = 1.0m;
            switch (slotIndex % 4)
            {
                case 0:
                    // Chuyến sáng sớm: Ưu đãi khởi hành sớm (-5%)
                    slotMultiplier = 0.95m;
                    break;
                case 1:
                    // Chuyến trưa: Giá bình ổn chuẩn
                    slotMultiplier = 1.0m;
                    break;
                case 2:
                    // Chuyến chiều: Giờ cao điểm thuận tiện (+3%)
                    slotMultiplier = 1.03m;
                    break;
                case 3:
                    // Chuyến tối: Chuyến đêm tiện nghi (+6%)
                    slotMultiplier = 1.06m;
                    break;
            }

            var calculated = basePrice * typeMultiplier * slotMultiplier;

            // 3. Làm tròn đến 5.000 VNĐ hoặc 10.000 VNĐ gần nhất
            var rounded = Math.Round(calculated / 5000m, MidpointRounding.AwayFromZero) * 5000m;

            // Đảm bảo không thấp hơn mức sàn tối thiểu của ngành xe khách (40.000đ)
            return Math.Max(40000m, rounded);
        }

        /// <summary>
        /// Tạo cấu hình 4 chuyến xe đa dạng phù hợp nhất với cung đường (phân biệt cự ly ngắn vs cự ly xa)
        /// Tuyến ngắn (&lt;= 80km như Thái Bình - Ninh Bình): Ghế ngồi 45, Ghế ngồi 29, Limousine 16 chỗ, Limousine VIP 9 chỗ
        /// Tuyến xa (&gt; 80km như Thái Nguyên - Cao Bằng, Hà Nội - Đà Nẵng): Ghế ngồi 45, Ghế ngồi 29, Giường nằm 34, Limousine Cung Điện VIP 22
        /// </summary>
        public static List<DynamicTripConfig> GetTripConfigsForRoute(decimal distance)
        {
            if (distance <= 80)
            {
                // 1. TUYẾN NGẮN (<= 80km như Thái Bình - Ninh Bình 55km, Hà Nội - Bắc Ninh 35km, Nam Định - Thái Bình 25km)
                // Tuyệt đối không dùng xe giường nằm 2 tầng cồng kềnh, sử dụng xe ghế ngồi và Limousine đưa đón
                return new List<DynamicTripConfig>
                {
                    new DynamicTripConfig
                    {
                        SlotIndex = 0,
                        DepartureTime = new TimeSpan(7, 30, 0),
                        BusTypeName = "Xe ghế ngồi 45 chỗ Hyundai Universe",
                        Capacity = 45,
                        BusTypeCode = "SEAT_45",
                        OperatorName = "SmartBus Eco Line",
                        ReviewCount = 1240,
                        Rating = 4.7,
                        IsFlashSale = true,
                        FlashSaleText = "GIÁ SIÊU TIẾT KIỆM",
                        NoticeText = "Xe 45 chỗ đời mới, điều hòa mát lạnh, đón trả chuẩn giờ"
                    },
                    new DynamicTripConfig
                    {
                        SlotIndex = 1,
                        DepartureTime = new TimeSpan(10, 30, 0),
                        BusTypeName = "Xe ghế ngồi 29 chỗ Eco Express",
                        Capacity = 29,
                        BusTypeCode = "SEAT_29",
                        OperatorName = "Hoàng Long Liên Tỉnh",
                        ReviewCount = 980,
                        Rating = 4.8,
                        IsFlashSale = false,
                        NoticeText = "Xe 29 chỗ ghế ngả êm ái, cổng sạc USB thoại mái, nước suối khăn lạnh"
                    },
                    new DynamicTripConfig
                    {
                        SlotIndex = 2,
                        DepartureTime = new TimeSpan(14, 0, 0),
                        BusTypeName = "Xe Limousine 16 chỗ Transit VIP",
                        Capacity = 29,
                        BusTypeCode = "SEAT_29",
                        OperatorName = "Phúc Xuyên Limousine",
                        ReviewCount = 2150,
                        Rating = 4.9,
                        IsFlashSale = true,
                        FlashSaleText = "ƯU ĐÃI GIỜ VÀNG",
                        NoticeText = "Limousine ghế da cao cấp, wifi tốc độ cao, hỗ trợ đón tận nơi trung tâm"
                    },
                    new DynamicTripConfig
                    {
                        SlotIndex = 3,
                        DepartureTime = new TimeSpan(17, 30, 0),
                        BusTypeName = "Limousine VIP 9 Chỗ Ghế Massage",
                        Capacity = 22,
                        BusTypeCode = "LIMOUSINE_22",
                        OperatorName = "SmartBus Royal Express",
                        ReviewCount = 3120,
                        Rating = 5.0,
                        IsFlashSale = false,
                        NoticeText = "Dòng xe VIP thương gia, ghế massage ngả 180 độ, đón trả tận cửa"
                    }
                };
            }

            if (distance <= 650)
            {
                // 2. TUYẾN TRUNG BÌNH (80 - 650km, ví dụ Thái Nguyên - Cao Bằng 135km, Hà Nội - Hải Phòng 105km, Hà Nội - Quảng Bình 500km...)
                return new List<DynamicTripConfig>
                {
                    new DynamicTripConfig
                    {
                        SlotIndex = 0,
                        DepartureTime = new TimeSpan(8, 0, 0),
                        BusTypeName = "Xe ghế ngồi 29 chỗ Eco Express",
                        Capacity = 29,
                        BusTypeCode = "SEAT_29",
                        OperatorName = "SmartBus Eco Line",
                        ReviewCount = 1398,
                        Rating = 4.8,
                        IsFlashSale = true,
                        FlashSaleText = "ƯU ĐÃI ĐẶT SỚM",
                        NoticeText = "Ghế ngồi ngả êm ái, khởi hành buổi sáng mát mẻ, đón trả linh hoạt"
                    },
                    new DynamicTripConfig
                    {
                        SlotIndex = 1,
                        DepartureTime = new TimeSpan(11, 30, 0),
                        BusTypeName = "Xe ghế ngồi 45 chỗ Hyundai Universe",
                        Capacity = 45,
                        BusTypeCode = "SEAT_45",
                        OperatorName = "Hoàng Long Express",
                        ReviewCount = 980,
                        Rating = 4.7,
                        IsFlashSale = false,
                        NoticeText = "Xe 45 chỗ khoang hành lý cực rộng, tài xế kinh nghiệm đường dài an toàn"
                    },
                    new DynamicTripConfig
                    {
                        SlotIndex = 2,
                        DepartureTime = new TimeSpan(14, 30, 0),
                        BusTypeName = "Xe giường nằm 2 tầng 34 chỗ Green Express",
                        Capacity = 34,
                        BusTypeCode = "SLEEPER_34",
                        OperatorName = "Green Express Bus 4.0",
                        ReviewCount = 2150,
                        Rating = 4.9,
                        IsFlashSale = true,
                        FlashSaleText = "TIẾT KIỆM 20K",
                        NoticeText = "Giường nằm 2 tầng cao cấp, chăn gối thơm tho, rèm che riêng tư từng phòng"
                    },
                    new DynamicTripConfig
                    {
                        SlotIndex = 3,
                        DepartureTime = new TimeSpan(19, 0, 0),
                        BusTypeName = "Limousine VIP 22 Phòng Cung Điện",
                        Capacity = 22,
                        BusTypeCode = "LIMOUSINE_22",
                        OperatorName = "SmartBus Royal VIP Travel",
                        ReviewCount = 3120,
                        Rating = 5.0,
                        IsFlashSale = false,
                        NoticeText = "Khoang cung điện VIP 2 tầng, tivi giải trí, massage, sạc type-C cao cấp"
                    }
                };
            }

            // 3. TUYẾN ĐƯỜNG DÀI XUYÊN VIỆT BẮC - TRUNG - NAM (> 650km đến 2.450km trên toàn bộ 331.212 km² lãnh thổ)
            // Tuyệt đối không dùng xe ghế ngồi, 100% là dòng xe giường nằm cao cấp và Limousine Cabin chuyên tuyến Bắc Nam
            return new List<DynamicTripConfig>
            {
                new DynamicTripConfig
                {
                    SlotIndex = 0,
                    DepartureTime = new TimeSpan(7, 0, 0),
                    BusTypeName = "Xe giường nằm 40 chỗ Bắc Nam Express",
                    Capacity = 34,
                    BusTypeCode = "SLEEPER_34",
                    OperatorName = "Hoàng Long Xuyên Việt",
                    ReviewCount = 2890,
                    Rating = 4.8,
                    IsFlashSale = true,
                    FlashSaleText = "GIÁ VÉ TIẾT KIỆM",
                    NoticeText = "Tuyến cao tốc Bắc Nam liên tục, giường nằm êm ái, hỗ trợ nước suối & khăn lạnh"
                },
                new DynamicTripConfig
                {
                    SlotIndex = 1,
                    DepartureTime = new TimeSpan(12, 0, 0),
                    BusTypeName = "Xe giường nằm 34 phòng Luxury Express",
                    Capacity = 34,
                    BusTypeCode = "SLEEPER_34",
                    OperatorName = "Phương Trang FUTA Bus Lines",
                    ReviewCount = 3850,
                    Rating = 4.9,
                    IsFlashSale = false,
                    NoticeText = "Giường nằm 34 phòng riêng biệt, rèm che kín đáo, bao gồm suất ăn trạm dừng chân"
                },
                new DynamicTripConfig
                {
                    SlotIndex = 2,
                    DepartureTime = new TimeSpan(16, 30, 0),
                    BusTypeName = "Limousine Cabin đôi 24 Phòng Suite",
                    Capacity = 22,
                    BusTypeCode = "LIMOUSINE_22",
                    OperatorName = "Thuận Thảo VIP Grand",
                    ReviewCount = 2150,
                    Rating = 4.9,
                    IsFlashSale = true,
                    FlashSaleText = "CABIN ĐÔI VIP",
                    NoticeText = "Phòng Suite tiện nghi, nệm cao su non êm ái, cổng sạc Type-C, tivi giải trí"
                },
                new DynamicTripConfig
                {
                    SlotIndex = 3,
                    DepartureTime = new TimeSpan(20, 30, 0),
                    BusTypeName = "Limousine VIP 22 Phòng Cung Điện Hoàng Gia",
                    Capacity = 22,
                    BusTypeCode = "LIMOUSINE_22",
                    OperatorName = "SmartBus Royal Grand Express",
                    ReviewCount = 4200,
                    Rating = 5.0,
                    IsFlashSale = false,
                    NoticeText = "Chuyên cơ mặt đất 22 phòng cung điện cao cấp nhất, ghế massage, wifi 5G xuyên suốt hành trình"
                }
            };
        }
    }

    public class DynamicTripConfig
    {
        public int SlotIndex { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public string BusTypeName { get; set; } = string.Empty;
        public int Capacity { get; set; } = 29;
        public string BusTypeCode { get; set; } = "SEAT_29";
        public string OperatorName { get; set; } = string.Empty;
        public int ReviewCount { get; set; } = 1000;
        public double Rating { get; set; } = 4.8;
        public bool IsFlashSale { get; set; } = false;
        public string? FlashSaleText { get; set; }
        public string NoticeText { get; set; } = string.Empty;
    }
}
