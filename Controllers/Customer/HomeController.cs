using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Customer
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly BHXContext _context;

        public HomeController(BHXContext context)
        {
            _context = context;
        }

        // Trang chủ: Hiển thị 8 sản phẩm mới nhất
        public async Task<IActionResult> Index()
        {
            var products = await _context.SanPhams
                .OrderByDescending(p => p.SanPhamID)
                .Take(8) // Lấy 8 sản phẩm
                .ToListAsync();
            return View(products);
        }

        public IActionResult About() => View(); // Trang Về chúng tôi
        public IActionResult Contact() => View(); // Trang Liên hệ
        public IActionResult Privacy() => View(); // Trang Bảo mật
    }
}