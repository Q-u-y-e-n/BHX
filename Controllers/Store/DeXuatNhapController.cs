using Microsoft.AspNetCore.Authorization; // Thêm
using Microsoft.AspNetCore.Mvc;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")] // Giữ nguyên
    [Authorize(Roles = "Store")] // CHỈ NHỮNG NGƯỜI CÓ ROLE "Store" ĐƯỢC VÀO
    public class DeXuatNhapController : Controller
    {
        // ... (Code của bạn)
        public IActionResult Index()
        {
            return View();
        }
    }
}