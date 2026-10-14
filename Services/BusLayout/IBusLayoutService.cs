using System.Collections.Generic;
using WebApplication1.Models.BusLayout;

namespace WebApplication1.Services.BusLayout
{
    public interface IBusLayoutService
    {
        /// <summary>
        /// Lấy thông tin cấu hình loại xe chuẩn theo tên loại xe hoặc số lượng chỗ ngồi
        /// </summary>
        BusLayoutConfig GetLayoutConfig(string? busTypeName, int capacity = 0);

        /// <summary>
        /// Sinh ra sơ đồ ghế theo tầng có gắn trạng thái ghế đã đặt (Booked) và giá tiền tương ứng
        /// </summary>
        List<BusFloorLayout> GenerateFloorLayouts(string? busTypeName, int capacity, IEnumerable<string> bookedSeats, decimal basePrice);

        /// <summary>
        /// Lấy toàn bộ danh sách các loại xe được hỗ trợ cấu hình trong hệ thống
        /// </summary>
        IReadOnlyList<BusLayoutConfig> GetAllPredefinedConfigs();
    }
}
