using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BHX_Web.Controllers.Customer
{
    [Area("Customer")]
    // [Authorize(Roles = "Customer")] // Có thể bỏ [Authorize] nếu muốn trang chủ ai cũng xem được
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Trả về View Index.cshtml trong Areas/Customer/Views/Home/
            return View();
        }
    }
}