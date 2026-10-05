namespace WebApplication1.Models.ViewModels {
    public class DriverPortalDemoViewModel
    {
        // Thông tin tài xế
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LicenseNo { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateOnly? DateOfBirth { get; set; }
        public DateOnly? LicenseExpiryDate { get; set; }
        public int? SelectedTripId { get; set; }
        public string? LoadError { get; set; }
        public DriverPortalTripDemoViewModel? SelectedTrip => AssignedTrips.FirstOrDefault(t => t.TripId == SelectedTripId) ?? AssignedTrips.FirstOrDefault();

        // Thống kê ca làm việc
        public int TodayTripsCount { get; set; }
        public int TotalPassengersCount { get; set; }

        // Danh sách các chuyến xe được giao
        public List<DriverPortalTripDemoViewModel> AssignedTrips { get; set; } = new();
    }

    /// <summary>
    /// ViewModel thông tin chuyến xe được phân công cho tài xế
    /// </summary>
    public class DriverPortalTripDemoViewModel
    {
        public int TripId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string StartPoint { get; set; } = string.Empty;
        public string EndPoint { get; set; } = string.Empty;
        public DateOnly TripDate { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public TimeOnly? ArrivalTime { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string BusTypeName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int BookedSeats { get; set; }
        public int CheckedInCount { get; set; }
        public List<DriverPortalPassengerDemoViewModel> Passengers { get; set; } = new();
        public string Status { get; set; } = "ACTIVE";
    }

    /// <summary>
    /// Kết quả xác thực / soát vé bằng mã QR hoặc mã vé
    /// </summary>
    public class DriverPortalResultDemoViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TicketCode { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class DriverPortalPassengerDemoViewModel
    {
        public string FullName { get; set; } = "";
        public string SeatNumber { get; set; } = "";
        public string TicketCode { get; set; } = "";
        public string DropOffStop { get; set; } = "";
        public bool CheckedIn { get; set; }
    }
}

