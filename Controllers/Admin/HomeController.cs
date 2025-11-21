using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BHX_Web.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Chỉ Admin được vào
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Trả về View Index.cshtml trong Areas/Admin/Views/Home/
            return View();
        }
    }
}