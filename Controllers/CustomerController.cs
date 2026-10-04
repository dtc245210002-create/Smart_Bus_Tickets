using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Authorize(Roles="ROLE_CUSTOMER")]
public sealed class CustomerController(DemoStore demo) : Controller
{
    private string Id=>User.FindFirstValue(ClaimTypes.NameIdentifier)??"";
    [HttpGet] public IActionResult Profile(){if(!demo.Enabled)return NotFound();var customer=demo.Customer(Id);return customer==null?NotFound():View(customer.Profile);}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Profile(CustomerProfileViewModel model)
    {
        if(!demo.Enabled)return NotFound();
        if(model.BirthDate>DemoStore.Today)ModelState.AddModelError(nameof(model.BirthDate),"Ngày sinh không thể trong tương lai.");
        if(!new[]{"Thông thường","Sinh viên","Học sinh","Người cao tuổi"}.Contains(model.DiscountGroup))ModelState.AddModelError(nameof(model.DiscountGroup),"Đối tượng không hợp lệ.");
        if(!ModelState.IsValid)return View(model);
        if(!demo.SaveProfile(Id,model)){ModelState.AddModelError(string.Empty,"Email hoặc số điện thoại đã được sử dụng.");return View(model);}
        var auth=await HttpContext.AuthenticateAsync();
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,Id),new Claim(ClaimTypes.Name,model.FullName),new Claim(ClaimTypes.Email,model.Email),new Claim(ClaimTypes.MobilePhone,model.Phone),new Claim(ClaimTypes.Role,"ROLE_CUSTOMER")};
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),auth.Properties);
        TempData["SuccessMessage"]="Đã lưu hồ sơ khách hàng.";return RedirectToAction("Profile");
    }
    [HttpPost,ValidateAntiForgeryToken] public IActionResult RequestDiscount(string group)
    {
        if(!demo.Enabled)return NotFound();if(!new[]{"Sinh viên","Học sinh","Người cao tuổi"}.Contains(group)){TempData["ErrorMessage"]="Vui lòng chọn đối tượng ưu đãi.";return RedirectToAction("Profile");}
        demo.RequestDiscount(Id,group);TempData["SuccessMessage"]="Đã gửi yêu cầu ưu đãi demo. Hồ sơ chuyển sang trạng thái chờ xét duyệt.";return RedirectToAction("Profile");
    }
}
