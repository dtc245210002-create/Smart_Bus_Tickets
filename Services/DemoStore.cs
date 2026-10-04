using WebApplication1.Models.ViewModels;

namespace WebApplication1.Services;

/// <summary>Development-only, process-local demo data. The gate serializes seat reservations.</summary>
public sealed class DemoStore
{
    public bool Enabled { get; }
    private readonly object gate = new();
    private readonly Dictionary<string, DemoCustomer> customers = new();
    private readonly Dictionary<string, DemoReservation> reservations = new();
    private readonly HashSet<string> checkedIn = [];
    private readonly List<string> incidents = [];
    public static DateTime Today => DateTime.UtcNow.AddHours(7).Date;
    private static readonly (int Id, string From, string To, decimal Price, int Minutes)[] routes =
    [ (1,"Hà Nội","Hải Phòng",150000,90), (2,"Hà Nội","Sa Pa",250000,330),
      (3,"Sài Gòn","Đà Lạt",300000,390), (4,"Đà Nẵng","Huế",120000,105),
      (5,"Thái Nguyên","Hà Nội",80000,120) ];

    public DemoStore(bool enabled)
    {
        Enabled = enabled;
        if (!enabled) return;
        customers["demo-passenger"] = new DemoCustomer("demo-passenger", new() { FullName="Nguyễn Văn An",Email="demo@smartbus.vn",Phone="0987654321", BirthDate=new DateTime(2004,5,12) }, PasswordHasher.HashPassword("Demo123!"), true);
        AddSeed("demo-passenger", 11, Today.AddDays(1), ["A01", "B01"], "BK-DEMO-001");
        AddSeed("demo-passenger", 12, Today.AddDays(2), ["C02"], "BK-DEMO-002");
    }

    public DemoCustomer? Customer(string id) { lock(gate) return customers.GetValueOrDefault(id); }
    public DemoCustomer? Login(string username,string password)
    {
        lock(gate) return customers.Values.FirstOrDefault(c => c.Active && (c.Profile.Email.Equals(username.Trim(),StringComparison.OrdinalIgnoreCase) || c.Profile.Phone==username.Trim()) && PasswordHasher.VerifyPassword(password,c.PasswordHash));
    }
    public DemoCustomer? Register(RegisterViewModel model)
    {
        lock(gate)
        {
            if(customers.Values.Any(c=>c.Profile.Email.Equals(model.Email.Trim(),StringComparison.OrdinalIgnoreCase)||c.Profile.Phone==model.Phone.Trim()))return null;
            var id="demo-"+Guid.NewGuid().ToString("N");
            var c=new DemoCustomer(id,new() { FullName=model.FullName.Trim(),Email=model.Email.Trim(),Phone=model.Phone.Trim() },PasswordHasher.HashPassword(model.Password),false);
            customers[id]=c; return c;
        }
    }
    public bool Activate(string email) { lock(gate){var c=customers.Values.FirstOrDefault(c=>c.Profile.Email.Equals(email,StringComparison.OrdinalIgnoreCase));if(c==null)return false;c.Active=true;return true;} }
    public DemoCustomer GoogleCustomer()
    {
        lock(gate)
        {
            if(customers.TryGetValue("demo-google",out var c))return c;
            c=new("demo-google",new(){FullName="Hành Khách Google",Email="customer.google@smartbus.vn",Phone="0988112233"},PasswordHasher.HashPassword(Guid.NewGuid().ToString()),true);
            customers[c.Id]=c;AddSeed(c.Id,11,Today.AddDays(1),["C01"],"BK-GOOGLE-001");return c;
        }
    }
    public bool SaveProfile(string id,CustomerProfileViewModel profile)
    {
        lock(gate)
        {
            if(!customers.TryGetValue(id,out var c)||customers.Values.Any(x=>x.Id!=id&&(x.Profile.Email.Equals(profile.Email,StringComparison.OrdinalIgnoreCase)||x.Profile.Phone==profile.Phone)))return false;
            profile.VerificationStatus=c.Profile.VerificationStatus;c.Profile=profile;return true;
        }
    }
    public void RequestDiscount(string id,string group) { lock(gate){if(customers.TryGetValue(id,out var c)){c.Profile.DiscountGroup=group;c.Profile.VerificationStatus="Chờ xét duyệt (demo)";}} }

    private static DateTime Departure(int tripId, DateTime date) => date.Date.AddHours((tripId%10) switch {1=>8,2=>14,_=>20});
    public List<TripItemViewModel> Trips(string from,string to,DateTime date)
    {
        lock(gate)
        {
            Expire();var route=routes.FirstOrDefault(r=>r.From.Equals(from.Trim(),StringComparison.OrdinalIgnoreCase)&&r.To.Equals(to.Trim(),StringComparison.OrdinalIgnoreCase));
            if(route.Id==0||date.Date<Today)return [];
            return Enumerable.Range(1,3).Select(i=>BuildTrip(route.Id*10+i,date)).Where(t=>date.Date>Today||Departure(t.TripId,date)>DateTime.UtcNow.AddHours(7)).ToList();
        }
    }
    private TripItemViewModel BuildTrip(int id,DateTime date)
    {
        var route=routes.Single(r=>r.Id==id/10);var index=id%10;var price=route.Price+(index==2?50000:0);var capacity=index==2?22:29;
        var occupied=reservations.Values.Where(r=>r.TripId==id&&r.Date.Date==date.Date&&r.Status is "HOLD" or "PAID").ToList();
        var seats=Enumerable.Range(0,capacity).Select(i=>{var code=$"{(char)('A'+i%4)}{i/4+1:D2}";var owner=occupied.FirstOrDefault(r=>r.Seats.Contains(code));return new SeatItemViewModel {SeatCode=code,Price=price,Status=owner==null?"AVAILABLE":owner.Status=="PAID"?"BOOKED":"HOLD"};}).ToList();
        var depart=Departure(id,date);var arrive=depart.AddMinutes(route.Minutes);
        return new() {TripId=id,OperatorName=index==2?"SmartBus VIP Express":"SmartBus Travel",BusTypeName=index==2?"Limousine VIP 22 Chỗ":index==3?"Xe Giường Nằm 29 Chỗ":"Xe 29 chỗ",BusImage="/images/bus-interior.svg",LicensePlate=index==1?"15B-112.23":"29B-888.68",DepartureTime=depart.TimeOfDay,ArrivalTime=arrive.TimeOfDay,DeparturePoint=$"{route.From} ({depart:HH:mm})",ArrivalPoint=$"{route.To} ({arrive:HH:mm})",DurationText=$"{route.Minutes/60}h {route.Minutes%60}m",Price=price,OriginalPrice=price*1.15m,TotalCapacity=capacity,AvailableSeats=seats.Count(s=>s.Status=="AVAILABLE"),Rating=4.8,ReviewCount=1250,IsFlashSale=false,BoardingPoints=[$"{route.From} ({depart:HH:mm})"],DropOffPoints=[$"{route.To} ({arrive:HH:mm})"],Seats=seats};
    }
    public (DemoPaymentViewModel? Booking,string? Error) Hold(string user,int tripId,DateTime date,string[] seats,string? boarding,string? dropoff)
    {
        lock(gate)
        {
            Expire();if(!customers.ContainsKey(user))return (null,"Vui lòng đăng nhập.");
            if(!routes.Any(r=>r.Id==tripId/10)||tripId%10 is <1 or >3||Departure(tripId,date)<=DateTime.UtcNow.AddHours(7))return (null,"Chuyến xe không còn mở đặt vé.");
            if(seats.Any(string.IsNullOrWhiteSpace))return (null,"Ghế không hợp lệ.");
            var trip=BuildTrip(tripId,date);var chosen=seats.Select(s=>s.Trim().ToUpperInvariant()).ToList();
            if(chosen.Count is <1 or >6||chosen.Distinct().Count()!=chosen.Count||chosen.Any(s=>!trip.Seats.Any(t=>t.SeatCode==s)))return (null,"Vui lòng chọn từ 1 đến 6 ghế hợp lệ.");
            if(chosen.Any(s=>trip.Seats.Single(t=>t.SeatCode==s).Status!="AVAILABLE"))return (null,"Một ghế vừa được người khác giữ hoặc đặt. Vui lòng chọn lại.");
            var r=new DemoReservation {Code="BK-"+Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),UserId=user,TripId=tripId,Date=date.Date,Seats=chosen,ExpiresAt=DateTimeOffset.UtcNow.AddMinutes(10),Boarding=trip.BoardingPoints.Contains(boarding??"")?boarding!:trip.BoardingPoints[0],Dropoff=trip.DropOffPoints.Contains(dropoff??"")?dropoff!:trip.DropOffPoints[0],UnitPrice=trip.Price};
            reservations[r.Code]=r;return (View(r),null);
        }
    }
    public DemoPaymentViewModel? Booking(string code,string user) { lock(gate){Expire();return reservations.TryGetValue(code??"",out var r)&&r.UserId==user?View(r):null;} }
    public List<DemoPaymentViewModel> MyBookings(string user) {lock(gate){Expire();return reservations.Values.Where(r=>r.UserId==user).OrderByDescending(r=>r.Created).Select(View).ToList();}}
    public (DemoPaymentViewModel? Booking,string? Error) Pay(string code,string user,string method,string? voucher)
    {
        lock(gate)
        {
            Expire();if(!reservations.TryGetValue(code??"",out var r)||r.UserId!=user)return(null,"Không tìm thấy đơn đặt vé.");
            if(r.Status=="PAID")return(View(r),null);
            if(r.Status!="HOLD")return(null,"Phiên giữ ghế đã hết hạn hoặc bị hủy.");
            if(!new[]{"VietQR","VNPay","MoMo","ZaloPay"}.Contains(method))return(null,"Phương thức thanh toán không hợp lệ.");
            var clean=voucher?.Trim().ToUpperInvariant();if(!string.IsNullOrEmpty(clean)&&clean!="SMARTBUSNEW")return(null,"Mã giảm giá không hợp lệ. Thử SMARTBUSNEW.");
            r.Voucher=clean;r.Discount=clean=="SMARTBUSNEW"?Math.Min(20000,r.UnitPrice*r.Seats.Count):0;r.Method=method;r.Status="PAID";return(View(r),null);
        }
    }
    public string? Cancel(string code,string user)
    {
        lock(gate){Expire();if(!reservations.TryGetValue(code??"",out var r)||r.UserId!=user)return"Không tìm thấy vé.";if(r.Status=="CANCELLED")return null;if(r.Status!="PAID"||Departure(r.TripId,r.Date)<=DateTime.UtcNow.AddHours(7)||r.Seats.Any(s=>checkedIn.Contains(TicketCode(r,s))))return"Chỉ hủy vé chưa sử dụng trước giờ khởi hành.";r.Status="CANCELLED";return null;}
    }
    public string? Change(string code,string user,int tripId,DateTime date)
    {
        lock(gate)
        {
            Expire();if(!reservations.TryGetValue(code??"",out var r)||r.UserId!=user)return"Không tìm thấy vé.";
            if(r.Status!="PAID"||Departure(r.TripId,r.Date)<=DateTime.UtcNow.AddHours(7)||r.Seats.Any(s=>checkedIn.Contains(TicketCode(r,s))))return"Chỉ đổi vé chưa sử dụng trước giờ khởi hành.";
            if(tripId/10!=r.TripId/10||tripId%10 is <1 or >3||Departure(tripId,date)<=DateTime.UtcNow.AddHours(7))return"Chuyến mới phải cùng tuyến và chưa khởi hành.";
            if(tripId==r.TripId&&date.Date==r.Date.Date)return"Vui lòng chọn một chuyến khác.";
            var trip=BuildTrip(tripId,date);if(trip.Price!=r.UnitPrice)return"Demo chỉ hỗ trợ đổi chuyến cùng giá vé.";
            if(r.Seats.Any(s=>!trip.Seats.Any(t=>t.SeatCode==s&&t.Status=="AVAILABLE")))return"Ghế cũ không còn trống trên chuyến mới. Vui lòng chọn chuyến khác.";
            r.TripId=tripId;r.Date=date.Date;r.Boarding=trip.BoardingPoints[0];r.Dropoff=trip.DropOffPoints[0];return null;
        }
    }
    public TicketDetailViewModel? Ticket(string? code,string user)
    {
        lock(gate)
        {
            Expire();var r=reservations.Values.FirstOrDefault(r=>r.UserId==user&&(code==null?r.Status=="PAID":r.Seats.Any(s=>TicketCode(r,s)==code)));
            if(r==null||r.Status is "HOLD" or "EXPIRED")return null;var seat=r.Seats.FirstOrDefault(s=>TicketCode(r,s)==code)??r.Seats[0];var c=customers[user];var trip=BuildTrip(r.TripId,r.Date);var route=routes.Single(t=>t.Id==r.TripId/10);
            return new(){TicketCode=TicketCode(r,seat),BookingCode=r.Code,BookingTime=r.Created.UtcDateTime.AddHours(7),TotalAmount=r.UnitPrice*r.Seats.Count-r.Discount,Price=r.UnitPrice-r.Discount/r.Seats.Count,SeatNumber=seat,Status=r.Status=="CANCELLED"?"CANCELLED":checkedIn.Contains(TicketCode(r,seat))?"USED":Departure(r.TripId,r.Date)<DateTime.UtcNow.AddHours(7)?"EXPIRED":"ACTIVE",PassengerName=c.Profile.FullName,PassengerPhone=c.Profile.Phone,PassengerEmail=c.Profile.Email,RouteName=$"{route.From} - {route.To}",RouteCode=$"R{route.Id:D2}",StartPoint=route.From,EndPoint=route.To,BoardingStopName=r.Boarding,DropOffStopName=r.Dropoff,BoardingStopAddress="Điểm đón SmartBus",DropOffStopAddress="Điểm trả SmartBus",DepartureTime=trip.DepartureTime,ArrivalTime=trip.ArrivalTime,TripDate=r.Date,TripId=r.TripId,LicensePlate=trip.LicensePlate,BusTypeName=trip.BusTypeName,DriverName="Trần Đình Trọng",DriverPhone="0988777999",EstimatedDuration=route.Minutes,QrDataPayload=TicketCode(r,seat)};
        }
    }
    public DriverDashboardViewModel DriverDashboard() {lock(gate){Expire();return new(){Bookings=reservations.Values.Where(r=>r.Status=="PAID").Select(View).ToList(),Incidents=incidents.ToList()};}}
    public (bool Valid,string Message) CheckIn(string ticket,int tripId,DateTime date)
    {
        lock(gate){Expire();var r=reservations.Values.FirstOrDefault(r=>r.Seats.Any(s=>TicketCode(r,s)==ticket));if(r==null||r.Status!="PAID")return(false,"Mã vé không hợp lệ hoặc đã bị hủy.");if(r.TripId!=tripId||r.Date.Date!=date.Date)return(false,"Vé không thuộc chuyến và ngày đang chọn.");if(!checkedIn.Add(ticket))return(false,"Vé đã được check-in trước đó.");return(true,$"Vé hợp lệ • {customers[r.UserId].Profile.FullName} • Ghế {r.Seats.First(s=>TicketCode(r,s)==ticket)}");}
    }
    public void Incident(string message) {lock(gate)incidents.Insert(0,$"{DateTime.UtcNow.AddHours(7):dd/MM HH:mm} • {message}");}
    private static string TicketCode(DemoReservation r,string seat)=>$"SBG-{r.Code[3..]}-{seat}";
    private void Expire(){foreach(var r in reservations.Values.Where(r=>r.Status=="HOLD"&&r.ExpiresAt<=DateTimeOffset.UtcNow))r.Status="EXPIRED";}
    private void AddSeed(string user,int tripId,DateTime date,List<string> seats,string code) {var trip=BuildTrip(tripId,date);reservations[code]=new(){Code=code,UserId=user,TripId=tripId,Date=date,Seats=seats,Status="PAID",UnitPrice=trip.Price,Boarding=trip.BoardingPoints[0],Dropoff=trip.DropOffPoints[0]};}
    private DemoPaymentViewModel View(DemoReservation r) {var c=customers[r.UserId];return new(){BookingCode=r.Code,Trip=BuildTrip(r.TripId,r.Date),TravelDate=r.Date,Seats=r.Seats.ToList(),ExpiresAt=r.ExpiresAt,Status=r.Status,PassengerName=c.Profile.FullName,Phone=c.Profile.Phone,Email=c.Profile.Email,BoardingPoint=r.Boarding,DropOffPoint=r.Dropoff,Subtotal=r.UnitPrice*r.Seats.Count,Discount=r.Discount,Voucher=r.Voucher,PaymentMethod=r.Method,TicketCodes=r.Seats.Select(s=>TicketCode(r,s)).ToList()};}
}

public sealed class DemoCustomer(string id,CustomerProfileViewModel profile,string passwordHash,bool active)
{ public string Id {get;}=id; public CustomerProfileViewModel Profile {get;set;}=profile; public string PasswordHash {get;}=passwordHash; public bool Active {get;set;}=active; }
internal sealed class DemoReservation
{
    public string Code {get;set;}="";public string UserId {get;set;}="";public int TripId {get;set;} public DateTime Date {get;set;} public List<string> Seats {get;set;}=[];
    public string Status {get;set;}="HOLD";public DateTimeOffset ExpiresAt {get;set;} public DateTimeOffset Created {get;set;}=DateTimeOffset.UtcNow;
    public string Boarding {get;set;}="";public string Dropoff {get;set;}="";public decimal UnitPrice {get;set;} public decimal Discount {get;set;} public string? Voucher {get;set;} public string? Method {get;set;}
}
