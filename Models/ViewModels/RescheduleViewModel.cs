using System;
using System.Collections.Generic;

namespace WebApplication1.Models.ViewModels
{
    public class RescheduleViewModel
    {
        // Bước hiện tại trong quy trình đổi vé (1: Tìm chuyến, 2: Chọn ghế, 3: So sánh & Xác nhận)
        public int Step { get; set; } = 1;

        // Thông tin vé hiện tại (Vé cũ)
        public TicketDetailViewModel OldTicket { get; set; } = new();

        // --- BƯỚC 1: TÌM KIẾM CHUYẾN XE MỚI ---
        public DateTime NewDepartureDate { get; set; } = DateTime.Today.AddDays(2);
        public string? TimeOfDayFilter { get; set; }
        public string? BusTypeFilter { get; set; }
        public List<TripItemViewModel> AvailableTrips { get; set; } = new();

        // --- BƯỚC 2: CHUYẾN XE MỚI ĐƯỢC CHỌN & CHỌN GHẾ NGỒI ---
        public int? SelectedTripId { get; set; }
        public TripItemViewModel? SelectedTrip { get; set; }
        public string? SelectedNewSeat { get; set; }
        public decimal NewSeatPrice { get; set; }
        public string? SelectedBoardingPoint { get; set; }
        public string? SelectedDropOffPoint { get; set; }

        // --- BƯỚC 3: TÍNH TOÁN SO SÁNH & CHÊNH LỆCH GIÁ ---
        // Giá vé mới thực tế (nếu chọn ghế riêng có giá thì lấy theo ghế, nếu không lấy theo giá chuyến)
        public decimal EffectiveNewPrice => NewSeatPrice > 0 ? NewSeatPrice : (SelectedTrip?.Price ?? 0);

        // Chênh lệch giá = Giá mới - Giá cũ
        public decimal PriceDifference => EffectiveNewPrice - OldTicket.Price;

        // Phí xử lý đổi vé (Miễn phí đổi vé trước 24 giờ)
        public decimal RescheduleFee { get; set; } = 0;

        // Số tiền khách cần bù thêm (nếu vé mới đắt hơn)
        public decimal AmountToPay => PriceDifference > 0 ? (PriceDifference + RescheduleFee) : 0;

        // Số tiền hoàn lại cho khách (nếu vé mới rẻ hơn)
        public decimal AmountToRefund => PriceDifference < 0 ? Math.Abs(PriceDifference) : 0;

        // Thông tin ngân hàng nhận hoàn tiền (nếu vé mới rẻ hơn vé cũ)
        public string? RefundBankName { get; set; }
        public string? RefundAccountNumber { get; set; }
        public string? RefundAccountHolder { get; set; }

        // Phương thức thanh toán khoản chênh lệch (nếu vé mới đắt hơn)
        public string PaymentMethod { get; set; } = "VietQR"; // VietQR, Momo, Card

        // Ghi chú hoặc lý do đổi vé
        public string? Reason { get; set; }
    }
}
