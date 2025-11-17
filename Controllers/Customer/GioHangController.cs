using Microsoft.AspNetCore.Authorization; // Thêm
using Microsoft.AspNetCore.Mvc;

namespace BHX_Web.Controllers.Customer
{
    [Area("Customer")] // Giữ nguyên
    // Bạn có thể yêu cầu "Customer" hoặc cho phép cả "Admin" và "Store"
    [Authorize(Roles = "Customer, Admin, Store")]
    public class GioHangController : Controller
    {
        // ... (Code của bạn)
        public IActionResult Index()
        {
            return View();
        }
    }
}