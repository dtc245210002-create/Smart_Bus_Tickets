using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WebApplication1.Services
{
    /// <summary>
    /// Bộ công cụ chuẩn hóa và đối soát địa danh thông minh phục vụ tìm kiếm tuyến xe buýt
    /// Hỗ trợ xử lý viết tắt (TP.HCM, SG, HN), tên đồng nghĩa, không dấu và tên bến xe
    /// </summary>
    public static class LocationHelper
    {
        private static readonly Dictionary<string, List<string>> LocationClusters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Hà Nội"] = new() { "Hà Nội", "Ha Noi", "Hanoi", "HN", "Bến xe Mỹ Đình", "Bến xe Giáp Bát", "Bến xe Nước Ngầm", "Mỹ Đình", "Giáp Bát", "Nước Ngầm", "Trần Nhân Tông" },
            ["Hải Phòng"] = new() { "Hải Phòng", "Hai Phong", "Haiphong", "HP", "Bến xe Cầu Rào", "Bến xe Niệm Nghĩa", "Bến xe Vĩnh Niệm", "Lạch Tray", "Đất Cảng" },
            ["TP. Hồ Chí Minh"] = new() { "TP. Hồ Chí Minh", "TP Hồ Chí Minh", "Hồ Chí Minh", "Sài Gòn", "Sai Gon", "TP.HCM", "TPHCM", "HCM", "Bến xe Miền Đông", "Bến xe Miền Tây", "Bến xe An Sương", "Miền Đông", "Miền Tây", "An Sương" },
            ["Đà Nẵng"] = new() { "Đà Nẵng", "Da Nang", "Danang", "ĐN", "Bến xe Trung Tâm Đà Nẵng", "Ngã 3 Huế" },
            ["Huế"] = new() { "Huế", "Hue", "Thừa Thiên Huế", "Bến xe Phía Nam Huế" },
            ["Sa Pa"] = new() { "Sa Pa", "Sapa", "Lào Cai", "Lao Cai", "Bến xe Sa Pa" },
            ["Hạ Long"] = new() { "Hạ Long", "Ha Long", "Quảng Ninh", "Quang Ninh", "Bãi Cháy", "Bến xe Bãi Cháy" },
            ["Đà Lạt"] = new() { "Đà Lạt", "Da Lat", "Dalat", "Lâm Đồng", "Lam Dong", "Bến xe Liên Tỉnh Đà Lạt" },
            ["Vũng Tàu"] = new() { "Vũng Tàu", "Vung Tau", "Bà Rịa", "Ba Ria", "Bến xe Vũng Tàu" },
            ["Cần Thơ"] = new() { "Cần Thơ", "Can Tho", "Tây Đô", "Bến xe Cần Thơ" },
            ["Nha Trang"] = new() { "Nha Trang", "Khánh Hòa", "Khanh Hoa", "Bến xe Nha Trang" },
            ["Hà Giang"] = new() { "Hà Giang", "Ha Giang", "Đồng Văn", "Bến xe Hà Giang" },
            ["Ninh Bình"] = new() { "Ninh Bình", "Ninh Binh", "Tràng An", "Bái Đính", "Bến xe Ninh Bình" },
            ["Mộc Châu"] = new() { "Mộc Châu", "Moc Chau", "Sơn La", "Son La", "Bến xe Mộc Châu" },
            ["Cao Bằng"] = new() { "Cao Bằng", "Cao Bang", "Thác Bản Giốc", "Bến xe Cao Bằng" },
            ["Quảng Bình"] = new() { "Quảng Bình", "Quang Binh", "Đồng Hới", "Dong Hoi", "Phong Nha", "Bến xe Đồng Hới" },
            ["Quy Nhơn"] = new() { "Quy Nhơn", "Quy Nhon", "Bình Định", "Binh Dinh", "Bến xe Quy Nhơn" },
            ["Buôn Ma Thuột"] = new() { "Buôn Ma Thuột", "Buon Ma Thuot", "BMT", "Đắk Lắk", "Dak Lak", "Bến xe Buôn Ma Thuột" },
            ["Phan Thiết"] = new() { "Phan Thiết", "Phan Thiet", "Mũi Né", "Mui Ne", "Bình Thuận", "Binh Thuan", "Bến xe Phan Thiết" },
            ["Cà Mau"] = new() { "Cà Mau", "Ca Mau", "Đất Mũi", "Bến xe Cà Mau" },
            ["Châu Đốc"] = new() { "Châu Đốc", "Chau Doc", "An Giang", "Núi Sam", "Bến xe Châu Đốc" },
            ["Rạch Giá"] = new() { "Rạch Giá", "Rach Gia", "Kiên Giang", "Kien Giang", "Bến xe Rạch Sỏi" },
            ["Thái Nguyên"] = new() { "Thái Nguyên", "Thai Nguyen", "Phổ Yên", "Bến xe Thái Nguyên" },
            ["Thái Bình"] = new() { "Thái Bình", "Thai Binh", "TB", "Bến xe Thái Bình", "Bến xe Hoàng Hà", "Bến xe Chợ Tư" },
            ["Nam Định"] = new() { "Nam Định", "Nam Dinh", "NĐ", "Bến xe Nam Định", "Bến xe Đò Quan" },
            ["Hải Dương"] = new() { "Hải Dương", "Hai Duong", "HD", "Bến xe Hải Dương" },
            ["Hưng Yên"] = new() { "Hưng Yên", "Hung Yen", "HY", "Bến xe Hưng Yên" },
            ["Thanh Hóa"] = new() { "Thanh Hóa", "Thanh Hoa", "TH", "Bến xe Phía Bắc Thanh Hóa", "Bến xe Phía Nam Thanh Hóa" }
        };

        /// <summary>
        /// Lấy danh sách các từ khóa đồng nghĩa của một địa danh
        /// </summary>
        public static List<string> GetLocationAliases(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            var clean = input.Trim();
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { clean };

            // Tìm cluster tương ứng
            foreach (var kvp in LocationClusters)
            {
                if (kvp.Value.Any(alias => alias.Equals(clean, StringComparison.OrdinalIgnoreCase) || 
                                          clean.Contains(alias, StringComparison.OrdinalIgnoreCase) ||
                                          alias.Contains(clean, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(kvp.Key);
                    foreach (var a in kvp.Value)
                    {
                        result.Add(a);
                    }
                }
            }

            return result.ToList();
        }

        /// <summary>
        /// Kiểm tra xem chuỗi nguồn (ví dụ tên tuyến, điểm đi/đến, trạm dừng) có khớp với địa danh tìm kiếm hay không
        /// </summary>
        public static bool Matches(string? source, string? searchTarget)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(searchTarget))
                return false;

            var aliases = GetLocationAliases(searchTarget);
            var normalizedSource = RemoveDiacritics(source);

            foreach (var alias in aliases)
            {
                if (source.Contains(alias, StringComparison.OrdinalIgnoreCase))
                    return true;

                var normalizedAlias = RemoveDiacritics(alias);
                if (normalizedSource.Contains(normalizedAlias, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Chuẩn hóa địa danh về dạng tên chuẩn phổ biến
        /// </summary>
        public static string NormalizeLocation(string? location)
        {
            if (string.IsNullOrWhiteSpace(location)) return string.Empty;
            var aliases = GetLocationAliases(location);
            return aliases.FirstOrDefault() ?? location.Trim();
        }

        /// <summary>
        /// Chuẩn hóa bỏ dấu tiếng Việt để đối soát linh hoạt
        /// </summary>
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC)
                .Replace("đ", "d", StringComparison.OrdinalIgnoreCase)
                .Replace("Đ", "D", StringComparison.OrdinalIgnoreCase);
        }
    }
}
