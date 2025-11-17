using Microsoft.AspNetCore.Authorization; // Thêm
using Microsoft.AspNetCore.Mvc;

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")] // Giữ nguyên
    [Authorize(Roles = "Admin")] // CHỈ NHỮNG NGƯỜI CÓ ROLE "Admin" ĐƯỢC VÀO
    public class PhanPhoiController : Controller
    {
        // ... (Code của bạn)
        public IActionResult Index()
        {
            return View();
        }
    }
}