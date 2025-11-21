using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BHX_Web.Data;
using BHX_Web.Models.Entities;

namespace BHX_Web.Controllers.Store
{
    [Area("Store")]
    [Authorize(Roles = "Store")]
    public class KhoCuaHangController : Controller
    {
        private readonly BHXContext _context;

        public KhoCuaHangController(BHXContext context)
        {
            _context = context;
        }

        // Helper: Lấy ID Cửa hàng từ Cookie
        private int GetCurrentStoreId()
        {
            var claim = User.FindFirst("CuaHangID");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // =========================================================
        // 1. DANH SÁCH TỒN KHO CỬA HÀNG
        // =========================================================
        public async Task<IActionResult> Index(string searchString, string filterType)
        {
            int storeId = GetCurrentStoreId();
            if (storeId == 0) return RedirectToAction("Login", "Account", new { area = "" });

            // Load dữ liệu tồn kho kèm thông tin sản phẩm
            var query = _context.TonKho_CuaHangs
                .Include(t => t.SanPham)
                .Where(t => t.CuaHangID == storeId)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.SanPham.TenSanPham.Contains(searchString));
            }

            // Lọc theo trạng thái (Để làm chức năng "Báo cáo tồn")
            if (filterType == "low")
            {
                // Lọc hàng sắp hết (ví dụ < 10)
                query = query.Where(t => t.SoLuong <= 10);
                ViewData["FilterStatus"] = "Cảnh báo: Hàng sắp hết";
            }
            else if (filterType == "out")
            {
                // Lọc hàng đã hết (= 0)
                query = query.Where(t => t.SoLuong == 0);
                ViewData["FilterStatus"] = "Cảnh báo: Đã hết hàng";
            }

            ViewData["CurrentFilter"] = searchString;

            // Sắp xếp: Ưu tiên hàng ít lên đầu để chú ý nhập hàng
            var data = await query.OrderBy(t => t.SoLuong).ToListAsync();

            return View(data);
        }
    }
}