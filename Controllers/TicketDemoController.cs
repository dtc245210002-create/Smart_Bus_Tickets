using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

public partial class TicketController
{
    private string CustomerId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Hold([FromBody] HoldRequest request)
    {
        if (!_demo.Enabled) return NotFound();
        if (User.Identity?.IsAuthenticated != true) return Unauthorized(new { success=false, message="Vui lòng đăng nhập để giữ ghế.", loginUrl=Url.Action("Login","Account",new { returnUrl=Url.Action("Search","Trip",new {from=request.From,to=request.To,date=request.Date.ToString("yyyy-MM-dd")}) }) });
        var (booking,error)=_demo.Hold(CustomerId,request.TripId,request.Date,request.Seats??[],request.Boarding,request.Dropoff);
        return booking==null ? Conflict(new {success=false,message=error}) : Json(new {success=true,data=new {booking.BookingCode,booking.ExpiresAt,paymentUrl=Url.Action("Payment",new{code=booking.BookingCode})}});
    }

    [Authorize(Roles="ROLE_CUSTOMER"), HttpGet]
    public IActionResult MyTickets(string filter="upcoming")
    {
        if(!_demo.Enabled)return NotFound();
        ViewBag.Filter=filter;
        var bookings=_demo.MyBookings(CustomerId);
        return View(bookings.Where(b=>filter switch {"cancelled"=>b.Status=="CANCELLED","history"=>b.Status=="PAID"&&b.TravelDate<DemoStore.Today,"pending"=>b.Status=="HOLD",_=>b.Status=="PAID"&&b.TravelDate>=DemoStore.Today}).ToList());
    }

    [Authorize(Roles="ROLE_CUSTOMER"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult ConfirmPayment(string code,string method,string? voucher)
    {
        if(!_demo.Enabled)return NotFound();
        var (booking,error)=_demo.Pay(code,CustomerId,method,voucher);
        if(error!=null){TempData["ErrorMessage"]=error;return RedirectToAction("Payment",new{code});}
        TempData["SuccessMessage"]="Thanh toán demo thành công. Vé đã được cấp; không có tiền thật được chuyển.";
        return RedirectToAction("Detail",new{id=booking!.TicketCodes[0]});
    }

    [Authorize(Roles="ROLE_CUSTOMER"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult SimulateFailure(string code)
    {
        if(!_demo.Enabled)return NotFound();if(_demo.Booking(code,CustomerId)==null)return NotFound();
        TempData["ErrorMessage"]="Thanh toán demo thất bại. Chưa có vé được cấp; bạn có thể thử lại trong thời gian giữ ghế.";
        return RedirectToAction("Payment",new{code});
    }

    [Authorize(Roles="ROLE_CUSTOMER"), HttpGet]
    public IActionResult Manage(string? code,string mode="change",DateTime? date=null)
    {
        if(!_demo.Enabled)return NotFound();
        if(string.IsNullOrEmpty(code)){ViewBag.ManageMode=mode;return View("MyTickets",_demo.MyBookings(CustomerId).Where(b=>b.Status=="PAID").ToList());}
        var booking=_demo.Booking(code,CustomerId);if(booking==null)return NotFound();
        ViewBag.ManageMode=mode;var travelDate=date?.Date??booking.TravelDate;
        var route=booking.Trip;
        var from=route.DeparturePoint.Split(" (")[0];var to=route.ArrivalPoint.Split(" (")[0];
        return View(new TicketChangeViewModel {Booking=booking,NewDate=travelDate,Alternatives=_demo.Trips(from,to,travelDate).Where(t=>t.Price==route.Price&&(t.TripId!=route.TripId||travelDate!=booking.TravelDate)).ToList()});
    }

    [Authorize(Roles="ROLE_CUSTOMER"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel(string code)
    {
        if(!_demo.Enabled)return NotFound();var error=_demo.Cancel(code,CustomerId);
        TempData[error==null?"SuccessMessage":"ErrorMessage"]=error??"Đã hủy vé và hoàn tiền mô phỏng 100%. Ghế đã được trả về sơ đồ.";
        return RedirectToAction("MyTickets",new{filter=error==null?"cancelled":"upcoming"});
    }

    [Authorize(Roles="ROLE_CUSTOMER"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Change(string code,int tripId,DateTime date)
    {
        if(!_demo.Enabled)return NotFound();var error=_demo.Change(code,CustomerId,tripId,date);
        TempData[error==null?"SuccessMessage":"ErrorMessage"]=error??"Đổi chuyến thành công. Vé QR đã cập nhật thông tin chuyến mới.";
        return error==null?RedirectToAction("MyTickets"):RedirectToAction("Manage",new{code,date=date.ToString("yyyy-MM-dd")});
    }
}

public sealed class HoldRequest
{
    public int TripId {get;set;} public DateTime Date {get;set;}
    public string[]? Seats {get;set;} public string? Boarding {get;set;} public string? Dropoff {get;set;}
    public string From {get;set;}="Hà Nội";public string To {get;set;}="Hải Phòng";
}
