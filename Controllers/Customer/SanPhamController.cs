using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Customer
{
    [Area("Customer")] // Định danh khu vực Khách hàng
    public class SanPhamController : Controller
    {
        private readonly BHXContext _context;

        public SanPhamController(BHXContext context)
        {
            _context = context;
        }

        // =============================================================
        // 1. DANH SÁCH SẢN PHẨM (Hỗ trợ Tìm kiếm & Lọc Danh mục)
        // =============================================================
        public async Task<IActionResult> Index(string searchString, string category)
        {
            // Bắt đầu với toàn bộ danh sách
            var query = _context.SanPhams.AsQueryable();

            // 1. Logic Tìm kiếm theo tên (Nếu có nhập từ khóa)
            if (!string.IsNullOrEmpty(searchString))
            {
                // Dùng Contains để tìm gần đúng (VD: "mì" sẽ ra "mì tôm", "bánh mì")
                query = query.Where(s => s.TenSanPham.Contains(searchString));
            }

            // 2. Logic Lọc theo danh mục (Nếu bấm từ trang chủ)
            if (!string.IsNullOrEmpty(category))
            {
                // Kiểm tra khác null trước khi Contains để tránh lỗi
                query = query.Where(s => s.LoaiSanPham != null && s.LoaiSanPham.Contains(category));
            }

            // Lưu lại giá trị để hiển thị thông báo trên View (VD: "Đang xem danh mục: Bánh kẹo")
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentCategory"] = category;

            // Sắp xếp sản phẩm mới nhất lên đầu và lấy danh sách
            var result = await query.OrderByDescending(s => s.SanPhamID).ToListAsync();

            return View(result);
        }

        // =============================================================
        // 2. XEM CHI TIẾT SẢN PHẨM
        // =============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .FirstOrDefaultAsync(m => m.SanPhamID == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }
    }
}